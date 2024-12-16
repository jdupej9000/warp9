#include <cuda.h>
#include <cuda_runtime.h>

#include <math.h>

__global__ void cpd_psumpt1_cuda(int m, int n, float thresh, float expFactor, float denomAdd, float* ctx);
__global__ void cpd_p1px_cuda(int m, int n, float thresh, float expFactor, float* ctx);
__global__ void cpd_sigmaest_cuda(int m, int n, float* ctx);

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

    const int threadsPerBlock = 512;

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

    const int threadsPerBlock = 512;

    int blocksPerGrid = (n + threadsPerBlock - 1) / threadsPerBlock;
    cpd_psumpt1_cuda<<<blocksPerGrid, threadsPerBlock>>>(m, n, thresh, factor, denom, dx);

    blocksPerGrid = (m + threadsPerBlock - 1) / threadsPerBlock;
    cpd_p1px_cuda<<<blocksPerGrid, threadsPerBlock>>>(m, n, thresh, factor, dx);
   
    // TODO: check errors
    cudaMemcpy(pt1p1px, dpt1, sizeof(float) * (n + m + 3 * m), cudaMemcpyDeviceToHost);
}

__global__ void cpd_psumpt1_cuda(int m, int n, float thresh, float expFactor, float denomAdd, float* ctx)
{
    int i = threadIdx.x + blockIdx.x * blockDim.x;

    if (i < n) {
        float* x = ctx;
        float* t = x + 3 * n;
        float* pt1 = t + 3 * m;
        float* p1 = pt1 + n;
        float* px = p1 + m;
        float* psum = px + 3 * m;

        float sum = 0;
        const float x0 = x[0 * n + i];
        const float x1 = x[1 * n + i];
        const float x2 = x[2 * n + i];

        for (int j = 0; j < m; j++) {
            float d0 = x0 - t[0 * m + j];
            float d1 = x1 - t[1 * m + j];
            float d2 = x2 - t[2 * m + j];
            float dist = __fmaf_rz(d0, d0, __fmaf_rz(d1, d1, __fmul_rz(d2, d2)));

            if (dist < thresh)
                sum += __expf(expFactor * dist);
        }

        psum[i] = 1.0f / (sum + denomAdd);
        pt1[i] = sum / (sum + denomAdd);
    }
}

__global__ void cpd_p1px_cuda(int m, int n, float thresh, float expFactor, float* ctx)
{
    int j = threadIdx.x + blockIdx.x * blockDim.x;

    if (j < m) {
        float* x = ctx;
        float* t = x + 3 * n;
        float* pt1 = t + 3 * m;
        float* p1 = pt1 + n;
        float* px = p1 + m;
        float* psum = px + 3 * m;

        float sumpx0 = 0, sumpx1 = 0, sumpx2 = 0;
        float sump1 = 0.0f;
        const float t0 = t[0 * m + j];
        const float t1 = t[1 * m + j];
        const float t2 = t[2 * m + j];

        for (int i = 0; i < n; i++) {
            const float x0 = x[0 * n + i];
            const float x1 = x[1 * n + i];
            const float x2 = x[2 * n + i];

            float d0 = x0 - t0;
            float d1 = x1 - t1;
            float d2 = x2 - t2;
            float dist = __fmaf_rz(d0, d0, __fmaf_rz(d1, d1, __fmul_rz(d2, d2)));

            if (dist < thresh) {
                float pmn = __expf(expFactor * dist) * psum[i];
                sumpx0 += pmn * x0;
                sumpx1 += pmn * x1;
                sumpx2 += pmn * x2;
                sump1 += pmn;
            }
        }

        p1[j] = sump1;
        px[0 * m + j] = sumpx0;
        px[1 * m + j] = sumpx1;
        px[2 * m + j] = sumpx2;
    }
}

__global__ void cpd_sigmaest_cuda(int m, int n, float* ctx)
{
    int i = threadIdx.x + blockIdx.x * blockDim.x;

    if (i < n) {
        float* x = ctx;
        float* t = x + 3 * n;
        float* temp = t + 3 * m;

        float accum = 0.0f;
        for (int j = 0; j < m; j++) {
            float dd0 = x[0 * n + i] - t[0 * m + j];
            float dd1 = x[1 * n + i] - t[1 * m + j];
            float dd2 = x[2 * n + i] - t[2 * m + j];
            float dist = dd0 * dd0 + dd1 * dd1 + dd2 * dd2;
            accum += dist;
        }

        temp[i] = accum;
    }
}