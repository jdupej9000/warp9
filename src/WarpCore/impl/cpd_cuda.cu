#include <cuda.h>
#include <cuda_runtime.h>

#include <math.h>

#define CONST_ARG const __grid_constant__

__global__ void cpd_psumpt1_cuda(CONST_ARG int m, CONST_ARG int n, CONST_ARG float thresh, CONST_ARG float expFactor, CONST_ARG float denomAdd, float* ctx);
__global__ void cpd_p1px_cuda(CONST_ARG int m, CONST_ARG int n, CONST_ARG float thresh, CONST_ARG float expFactor, float* ctx);
__global__ void cpd_sigmaest_cuda(CONST_ARG int m, CONST_ARG int n, float* ctx);

#define BLOCK_SIZE (512)

bool cpd_init_cuda(int m, int n, const void* x, void** ppDevCtx)
{
    // Layout:
    size_t devMemorySize = sizeof(float) * (3 * n + 3 * m + n + m + 3 * m);

    if (cudaMalloc(ppDevCtx, devMemorySize) != cudaSuccess)
        return false;

    float* dx = (float*)*ppDevCtx;
    cudaMemcpy(dx, x, sizeof(float) * 3 * n, cudaMemcpyHostToDevice);

    return true;
}

void cpd_deinit_cuda(void* pDevCtx)
{
    cudaFree(pDevCtx);
}

float cpd_estimate_sigma_cuda(void* pDevCtx, const float* x, const float* t, int m, int n)
{
    float* dx = (float*)pDevCtx;
    float* dt = dx + 3 * n;
    float* dtemp = dt + 3 * m;

    cudaMemcpy(dt, t, sizeof(float) * 3 * m, cudaMemcpyHostToDevice);
    cudaMemset(dtemp, 0, sizeof(float) * n);

    const int threadsPerBlock = BLOCK_SIZE;

    int blocksPerGrid = (n + threadsPerBlock - 1) / threadsPerBlock;
    cpd_sigmaest_cuda<<<blocksPerGrid, threadsPerBlock>>> (m, n, dx);

    float* sumpart = new float[n];
    cudaMemcpy(sumpart, dtemp, sizeof(float) * n, cudaMemcpyDeviceToHost);

    float sum = 0;
    for (int i = 0; i < n; i++)
        sum += sumpart[i];

    delete[] sumpart;

    return sum / (3 * m * n);
}

void cpd_estep_cuda(void* pDevCtx, const float* x, const float* t, int m, int n, float w, float sigma2, float denom, float* pt1p1px)
{
    const float factor = -1.0f / (2.0f * sigma2);
    const float thresh = std::max(0.0001f, 2.0f * sqrtf(sigma2));

    float* dx = (float*)pDevCtx;
    float* dt = dx + 3 * n;
    float* dpt1 = dt + 3 * m; // dpt1, dp1, dpx must be in the correct sequence, we'll be copying them in one operation
    float* dp1 = dpt1 + n;
    float* dpx = dp1 + m;  
    //float* dpsum = dpx + 3 * m;
    cudaMemcpy(dt, t, sizeof(float) * 3 * m, cudaMemcpyHostToDevice);
    cudaMemset(dpx, 0, sizeof(float) * (3 * m + m + n + n));

    const int threadsPerBlock = BLOCK_SIZE;

    int blocksPerGrid = (n + threadsPerBlock - 1) / threadsPerBlock;
    cpd_psumpt1_cuda<<<blocksPerGrid, threadsPerBlock>>>(m, n, thresh, factor, denom, dx);

    blocksPerGrid = (m + threadsPerBlock - 1) / threadsPerBlock;
    cpd_p1px_cuda<<<blocksPerGrid, threadsPerBlock>>>(m, n, thresh, factor, dx);
   
    // TODO: check errors
    cudaMemcpy(pt1p1px, dpt1, sizeof(float) * (n + m + 3 * m), cudaMemcpyDeviceToHost);
}

__global__ void cpd_psumpt1_cuda(CONST_ARG int m, CONST_ARG int n, CONST_ARG float thresh, CONST_ARG float expFactor, CONST_ARG float denomAdd, float* ctx)
{
    __shared__ float t012[3 * BLOCK_SIZE];

    int thread = threadIdx.x;
    int i = thread + blockIdx.x * BLOCK_SIZE;

    float* x = ctx;
    float* t = x + 3 * n;
    float* pt1 = t + 3 * m;
    float* p1 = pt1 + n;
    float* px = p1 + m;
    float* psum = px + 3 * m;

    float sum = 0;
    float x0 = 0, x1 = 0, x2 = 0;
    if (i < n) {
        x0 = x[0 * n + i];
        x1 = x[1 * n + i];
        x2 = x[2 * n + i];
    }

    for (int jb = 0; jb < m; jb += BLOCK_SIZE) {
        int mb = __min(m, jb + BLOCK_SIZE) - jb;

        int jthread = jb + thread;
        t012[3 * thread + 0] = t[0 * m + jthread];
        t012[3 * thread + 1] = t[1 * m + jthread];
        t012[3 * thread + 2] = t[2 * m + jthread];

        __syncthreads();

        if (i < n) {
            float sumb = 0;
            for (int j = 0; j < mb; j++) {
                float d0 = x0 - t012[3 * j + 0];
                float d1 = x1 - t012[3 * j + 1];
                float d2 = x2 - t012[3 * j + 2];
                float dist = fmaf(d0, d0, fmaf(d1, d1, d2 * d2));

                if (dist < thresh)
                    sumb += __expf(expFactor * dist);
            }
            sum += sumb;
        }

        __syncthreads();
    }

    if (i < n) {
        if (fabs(sum + denomAdd) > 1e-5f) {
            float psumi = 1.0f / (sum + denomAdd);
            psum[i] = psumi;
            pt1[i] = sum * psumi;
        }
        else {
            psum[i] = 10000;
            pt1[i] = 0;
        }
    }
}

__global__ void cpd_p1px_cuda(CONST_ARG int m, CONST_ARG int n, CONST_ARG float thresh, CONST_ARG float expFactor, float* ctx)
{
    __shared__ float x012sum[4 * BLOCK_SIZE];

    int thread = threadIdx.x;
    int j = thread + blockIdx.x * BLOCK_SIZE;

    float* x = ctx;
    float* t = x + 3 * n;
    float* pt1 = t + 3 * m;
    float* p1 = pt1 + n;
    float* px = p1 + m;
    float* psum = px + 3 * m;

    float sumpx0 = 0, sumpx1 = 0, sumpx2 = 0;
    float sump1 = 0.0f;

    float t0 = 0, t1 = 0, t2 = 0;
    if (j < m) {
        t0 = t[0 * m + j];
        t1 = t[1 * m + j];
        t2 = t[2 * m + j];
    }

    for (int ib = 0; ib < n; ib += BLOCK_SIZE) {
        int nb = __min(n, ib + BLOCK_SIZE) - ib;

        int ithread = ib + thread;
        x012sum[4 * thread + 0] = x[0 * n + ithread];
        x012sum[4 * thread + 1] = x[1 * n + ithread];
        x012sum[4 * thread + 2] = x[2 * n + ithread];
        x012sum[4 * thread + 3] = psum[ithread];

        __syncthreads();

        if (j < m) {
            float sumpx0b = 0, sumpx1b = 0, sumpx2b = 0, sump1b = 0;
            for (int i = 0; i < nb; i++) {
                float d0 = x012sum[4 * i + 0] - t0;
                float d1 = x012sum[4 * i + 1] - t1;
                float d2 = x012sum[4 * i + 2] - t2;
                float dist = fmaf(d0, d0, fmaf(d1, d1, d2 * d2));

                if (dist < thresh) {
                    float pmn = __expf(expFactor * dist) * x012sum[4 * i + 3];
                    sumpx0b += pmn * x012sum[4 * i + 0];
                    sumpx1b += pmn * x012sum[4 * i + 1];
                    sumpx2b += pmn * x012sum[4 * i + 2];
                    sump1b += pmn;
                }
            }
            sumpx0 += sumpx0b;
            sumpx1 += sumpx1b;
            sumpx2 += sumpx2b;
            sump1 += sump1b;
        }

        __syncthreads();
    }

    if (j < m) {
        p1[j] = sump1;
        px[0 * m + j] = sumpx0;
        px[1 * m + j] = sumpx1;
        px[2 * m + j] = sumpx2;
    }
}

__global__ void cpd_sigmaest_cuda(CONST_ARG int m, CONST_ARG int n, float* ctx)
{
    __shared__ float t012[3 * BLOCK_SIZE];

    int thread = threadIdx.x;
    int i = thread + blockIdx.x * BLOCK_SIZE;

    float* x = ctx;
    float* t = x + 3 * n;
    float* temp = t + 3 * m;

    float x0 = 0, x1 = 0, x2 = 0, accum = 0;
    if (i < n) {
        x0 = x[i];
        x1 = x[n + i];
        x2 = x[2 * n + i];
    }

    for (int jb = 0; jb < m; jb += BLOCK_SIZE) {
        int mb = __min(m, jb + BLOCK_SIZE) - jb;

        int jthread = jb + thread;
        t012[3 * thread + 0] = t[0 * m + jthread];
        t012[3 * thread + 1] = t[1 * m + jthread];
        t012[3 * thread + 2] = t[2 * m + jthread];
        
        __syncthreads();

        if (i < n) {
            float accumBlock = 0;
            for (int j = 0; j < mb; j++) {
                float dd0 = x0 - t012[3 * j + 0];
                float dd1 = x1 - t012[3 * j + 1];
                float dd2 = x2 - t012[3 * j + 2];
                float dist = fmaf(dd0, dd0, fmaf(dd1, dd1, dd2 * dd2));
                accumBlock += dist;
            }

            accum += accumBlock;
        }

        __syncthreads();
    }

    if (i < n)
        temp[i] = accum;
}