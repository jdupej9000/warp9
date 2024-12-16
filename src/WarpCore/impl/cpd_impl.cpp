#include "cpd_impl.h"
#include "pcl_utils.h"
#include "vec_math.h"
#include "kmeans.h"
#include "utils.h"
#include <math.h>
#include <lapacke.h>
#include <cblas.h>
#include <algorithm>
#include <immintrin.h>
#include <stdlib.h>

namespace warpcore::impl
{
    void cpd_samplefirst_g(const float* y, int m, float beta, float* gi);
    void cpd_sample_g(const float* y, int m, int col, float beta, float* gi);
    void cpd_make_lambda(const float* y, int m, int k, float beta, const float* q, float* lambda);
    void cpd_sigmapart(int m, int n, const float* x, const float* t, float* si2partial);
    void cpd_psumpt1(int m, int n, float thresh, float expFactor, float denomAdd, const float* x, const float* t, float* psum, float* pt1, const int* trunc_wnd);
    void cpd_p1px(int m, int n, float thresh, float expFactor, float denomAdd, const float* x, const float* t, const float* psum, float* p1, float* px, const int* trunc_wnd);


    int cpd_lowrank_numcols(int m) 
    {
        return std::min(100, (int)ceil(cbrt(m)));
    }

    int cpd_tmp_size(int m, int n, int k)
    {
        return 4 * m * k;
    }

    void cpd_init_clustered(const float* y, int m, int k, float beta, float* q, float* lambda)
    {
        float* centers = new float[3*m];
        int* labels = new int[m+k];
        int* centerIdx = labels + m;
        float* tau = new float[m];
        float ci[3];

        // Cluster points y and find points which are closest to cluster centers -> centerIdx.
        kmeans<3>(y, m, k, centers, labels);
        for(int i = 0; i < k; i++) {
            get_row<float, 3>(centers, k, i, ci);
            centerIdx[i] = nearest<3>(y, m, ci);
        }

        // Sample matrix G at centerIdx.
        //cpd_samplefirst_g(y, m, beta, q);

        for(int i = 0; i < k; i++)
            cpd_sample_g(y, m, centerIdx[i], beta, q + i * m);

        // Orthogonalize q with a QR decomposition.
        LAPACKE_sgeqrf(LAPACK_COL_MAJOR, m, k, q, m, tau);
        LAPACKE_sorgqr(LAPACK_COL_MAJOR, m, k, k, q, m, tau);

        // Compute lambda (eigenvalues).
        std::memset(lambda, 0, k * sizeof(float));
        cpd_make_lambda(y, m, k, beta, q, lambda);

        delete[] centers;
        delete[] labels;
        delete[] tau;
    }

    float cpd_estimate_sigma(const float* x, const float* y, int m, int n, float* tmp)
    {
        cpd_sigmapart(m, n, x, y, tmp);
        return reduce_add(tmp, n) / (3 * m * n);
    }

    void cpd_estep(const float* sortedx, const float* t, int m, int n, float w, float sigma2, float denom, float* psum, float* pt1, float* p1, float* px, int* trunc_wnd, int xsort_by)
    {
        const float factor = -1.0f / (2.0f * sigma2);
        const float thresh = std::max(0.0001f, 2.0f * sqrtf(sigma2));

        cpd_narrow_trunc_window(sortedx + n * xsort_by, t + m * xsort_by, m, n, sqrtf(thresh), trunc_wnd);
        cpd_psumpt1(m, n, thresh, factor, denom, sortedx, t, psum, pt1, trunc_wnd);
        cpd_p1px(m, n, thresh, factor, denom, sortedx, t, psum, p1, px, trunc_wnd);
    }

    float cpd_mstep(const float* y, const float* pt1, const float* p1, const float* px, const float* q, const float* l, const float* linv, int m, int n, int k, float sigma2, float lambda, float* t, float* tmp)
    {
        float* _p0 = tmp;
        float* _p1 = _p0 + m * k;
        float* _p2 = _p1 + m * k;
        float* _p3 = _p2 + m * k;

        dxinva(px, p1, m, 3, _p0);
        cblas_saxpy(m * 3, -1.0f, y, 1, _p0, 1);
        // _p0 (m,3): diag(p1)^-1 * px - y [right]

        float tf = 1.0f / (sigma2 * lambda);
        atdba(q, m, k, p1, tf, _p1);
        cblas_saxpy(k, 1.0f, linv, 1, _p1, k + 1);
        int* piv = (int*)_p2;
        std::memset(piv, 0, k * sizeof(int));
        LAPACKE_sgetrf(LAPACK_COL_MAJOR, k, k, _p1, k, piv);
        LAPACKE_sgetri(LAPACK_COL_MAJOR, k, _p1, k, piv); 
        // _p1 (k,k): ( tf * q^T * diag(p1) * q + l^-1)^-1  [inner]
        // _p2 (k,1): destroyed

        dxa(_p0, p1, m, 3, _p2);
        std::memcpy(_p3, _p2, m * 3 * sizeof(float)); 
        cblas_sscal(m*3, tf, _p2, 1);
        // _p2 (m,3): tf * diag(p1) * right [w]
        // _p3 (m,3): diag(p1) * right

        cblas_sgemm(CblasColMajor, CblasTrans, CblasNoTrans, k, 3, m, tf*tf, q, m, _p3, m, 0.0f, _p0, k);
        // _p0 (k,3): tf^2 * q^T * diag(p1) * right

        cblas_sgemm(CblasColMajor, CblasNoTrans, CblasNoTrans, k, 3, k, 1.0f, _p1, k, _p0, k, 0.0f, _p3, k);
        // _p3 (k,3): [inner] * tf^2 * q^T * diag(p1) * right

        cblas_sgemm(CblasColMajor, CblasNoTrans, CblasNoTrans, m, 3, k, 1.0f, q, m, _p3, k, 0.0f, _p0, m);
        // _p0 (m,3): q * [inner] * tf^2 * q^T * diag(p1) * right

        dxa(_p0, p1, m, 3, _p0);
        // _p0 (m,3): diag(p1) * q * [inner] * tf^2 * q^T * diag(p1) * right  [solve1]
        // _p3 avail.

        cblas_saxpy(m*3, -1.0f, _p0, 1, _p2, 1);
        // _p2 (m,3): [w] - [solve1]

        cblas_sgemm(CblasColMajor, CblasTrans, CblasNoTrans, k, 3, m, 1.0f, q, m, _p2, m, 0.0f, _p3, k); 
        // _p3 (k,3): q^T * w

        float ret = lambda / 2 * tratdba(_p3, k, 3, l);

        dxa(_p3, l, k, 3, _p3);
        // _p3 (k,3): diag(l) * q^T * w

        std::memcpy(t, y, m * 3 * sizeof(float));
        cblas_sgemm(CblasColMajor, CblasNoTrans, CblasNoTrans, m, 3, k, 1.0f, q, m, _p3, k, 1.0f, t, m); 
        // T (m,3): q * diag(l) * q^t * w + y

        return ret;
    }

    float cpd_update_sigma2(const float* x, const float* t, const float* pt1, const float* p1, const float* px, int m, int n)
    {
        float ret = tratdba(x, n, 3, pt1) + tratdba(t, m, 3, p1);
        ret -= 2 * cblas_sdot(m * 3, px, 1, t, 1); // -= 2 * Matrix.TraceOfProduct(PX, T, true);
        return ret / (3 * reduce_add(p1, m));
    }

    int cpd_make_sortedx(const float* x, int n, float* sortedx)
    {
        float x01[6]{ FLT_MAX, FLT_MAX, FLT_MAX, FLT_MIN, FLT_MIN, FLT_MIN };
        pcl_aabb(x, 3, n, x01, x01 + 3);

        float dx[3]{ x01[3] - x01[0], x01[4] - x01[1],x01[5] - x01[2] };

        int sortby = 0;
        if (dx[1] > dx[sortby]) sortby = 1;
        if (dx[2] > dx[sortby]) sortby = 2;

        int* order = new int[n];
        for (int i = 0; i < n; i++)
            order[i] = i;

        const float* pcol = x + sortby * n;
        ::qsort_s(order, n, sizeof(int),
            [](void* pcol, const void* p0, const void* p1) -> int {
                int i0 = *(const int*)p0;
                int i1 = *(const int*)p1;
                const float* pfcol = (const float*)pcol;

                if (pfcol[i0] < pfcol[i1]) return -1;
                if (pfcol[i0] > pfcol[i1]) return 1;
                return 0;
            }, const_cast<float*>(pcol));

        for (int d = 0; d < 3; d++) {
            const float* col = x + d * n;
            float* sortedcol = sortedx + d * n;

            for (int i = 0; i < n; i++)
                sortedcol[i] = col[order[i]];
        }

        delete[] order;
        return sortby;
    }

    void cpd_narrow_trunc_window(const float* sortedxcol, const float* tcol, int m, int n, float thresh, int* bounds)
    {
        for (int i = 0; i < m; i++) {
            //int bmin = bounds[2 * i], bmax = bounds[2 * i + 1];
            int bmin = 0, bmax = n - 1;

            while (bmin < bmax && sortedxcol[bmin] < tcol[i] - thresh)
                bmin++;

            while (bmin < bmax && sortedxcol[bmax] > tcol[i] + thresh)
                bmax--;

            bounds[2 * i] = bmin;
            bounds[2 * i + 1] = bmax;
        }
    }

    void cpd_init_trunc_window(int n, int m, int* bounds)
    {
        for (int i = 0; i < m; i++) {
            bounds[2 * i] = 0;
            bounds[2 * i + 1] = n - 1;
        }
    }

    void cpd_samplefirst_g(const float* y, int m, float beta, float* gi)
    {
        const float ef = -0.5f / (beta * beta);

        for(int i = 0; i < m; i++) {
            float d = 0;
            for(int j = 0; j < 3; j++) {
                const float dj = y[j*m+i];
                d += dj * dj;
            }

            gi[i] = expf(ef * d);
        }
    }


    void cpd_sample_g(const float* y, int m, int col, float beta, float* gi)
    {
        const float ef = -0.5f / (beta * beta);
        const __m256 ef8 = _mm256_set1_ps(ef);
      
        const int mch = round_down(m, 8);
        for(int i = 0; i < mch; i += 8) {
            __m256 d = _mm256_setzero_ps();
            
            for(int j = 0; j < 3; j++) {
                const __m256 dj = _mm256_sub_ps(_mm256_loadu_ps(y + j*m + i), _mm256_broadcast_ss(y + j*m + col));
                d = _mm256_fmadd_ps(dj, dj, d);
            }

            const __m256 t = expf_fast(_mm256_mul_ps(d, ef8));
            _mm256_storeu_ps(gi + i, t);
        }

        for(int i = mch; i < m; i++) {
            float d = 0;
            for(int j = 0; j < 3; j++) {
                const float dj = y[j*m+i] - y[j*m+col];
                d += dj * dj;
            }

            gi[i] = expf(ef * d);
        }
    }

    void cpd_make_lambda(const float* y, int m, int k, float beta, const float* q, float* lambda)
    {
        const float ef = -0.5f / (beta * beta);
        const __m256 ef8 = _mm256_set1_ps(ef);
        const int m8 = round_down(m, 8);
        float yi[3];

        for(int i = 0; i < m; i++) {
            get_row<float, 3>(y, m, i, yi);

            for(int j = 0; j < m8; j+= 8) {
                __m256 d2 = _mm256_setzero_ps();

                for(int l = 0; l < 3; l++) {
                    const __m256 dj = _mm256_sub_ps(_mm256_broadcast_ss(yi + l), _mm256_loadu_ps(y + l*m + j));
                    d2 = _mm256_fmadd_ps(dj, dj, d2);
                }

                const __m256 g = expf_fast(_mm256_mul_ps(d2, ef8));

                for(int l = 0; l < k; l++) {
                    __m256 lj = _mm256_mul_ps(
                        _mm256_mul_ps(g, _mm256_loadu_ps(q + j + l*m)),
                        _mm256_broadcast_ss(q + i + l*m));

                    lambda[l] += reduce_add(lj);
                }
            }

            for(int j = m8; j < m; j++) {
                float d2 = 0;
                for(int l = 0; l < 3; l++) {
                    const float dj = yi[l] - y[j + l*m];
                    d2 += dj * dj;
                }
                float g = expf(ef * d2);

                for(int l = 0; l < k; l++) {
                    lambda[l] += g * q[j + l*m] * q[i + l*m];
                }
            }
        }
    }

    void cpd_sigmapart(int m, int n, const float* x, const float* t, float* si2partial)
    {
        const int n8 = n >> 3;

        #pragma omp parallel for schedule(dynamic, 8)
        for(int i8 = 0; i8 < n8; i8++) {
            int i = 8 * i8;
            __m256 accum = _mm256_setzero_ps();
            const __m256 x0 = _mm256_loadu_ps(x + i);
            const __m256 x1 = _mm256_loadu_ps(x + i + n);
            const __m256 x2 = _mm256_loadu_ps(x + i + 2*n);

            for(int j = 0; j < m; j++) {
                const __m256 d0 = _mm256_sub_ps(x0, _mm256_broadcast_ss(t + j));
                const __m256 d1 = _mm256_sub_ps(x1, _mm256_broadcast_ss(t + j + m));
                const __m256 d2 = _mm256_sub_ps(x2, _mm256_broadcast_ss(t + j + 2*m));

                __m256 a = _mm256_fmadd_ps(d1, d1, _mm256_mul_ps(d0, d0));
                a = _mm256_fmadd_ps(d2, d2, a);
                accum = _mm256_add_ps(accum, a);
            }

            _mm256_storeu_ps(si2partial + i, accum);
        }

        for(int i = 8 * n8; i < n; i++) {
            float accum = 0;
            for(int j = 0; j < m; j++) {
                const float d0 = x[i] - t[j];
                const float d1 = x[i+n] - t[j+m];
                const float d2 = x[i+2*n] - t[j+2*m];
                accum += d0*d0 + d1*d1 + d2*d2;
            }
            si2partial[i] = accum;
        }
    }

    void cpd_psumpt1(int m, int n, float thresh, float expFactor, float denomAdd, const float* x, const float* t, float* psum, float* pt1, const int* trunc_wnd)
    {
        const __m256 factor8 = _mm256_broadcast_ss(&expFactor);
        const __m256 thresh8 = _mm256_broadcast_ss(&thresh);	
        const __m256 denomAdd8 = _mm256_broadcast_ss(&denomAdd);
        const int n8 = n >> 3;

        #pragma omp parallel for schedule(dynamic, 8)
        for(int i8 = 0; i8 < n8; i8++) {	
            const int i = i8 * 8;
            __m256 accum = _mm256_setzero_ps();
            const __m256 x0 = _mm256_loadu_ps(x + i);
            const __m256 x1 = _mm256_loadu_ps(x + i + n);
            const __m256 x2 = _mm256_loadu_ps(x + i + 2*n);

            for(int j = 0; j < m; j++) {
                const __m256 d0 = _mm256_sub_ps(_mm256_broadcast_ss(t + j), x0);
                const __m256 d1 = _mm256_sub_ps(_mm256_broadcast_ss(t + j + m), x1);
                const __m256 d2 = _mm256_sub_ps(_mm256_broadcast_ss(t + j + 2*m), x2);
                __m256 dist = _mm256_mul_ps(d0, d0);
                dist = _mm256_fmadd_ps(d1, d1, dist);
                dist = _mm256_fmadd_ps(d2, d2, dist);

                const __m256 compareMask = _mm256_cmp_ps(dist, thresh8, _CMP_LT_OQ);
                int mask = _mm256_movemask_epi8(_mm256_castps_si256(compareMask));
                if (mask != 0) {
                    __m256 affinity = _mm256_and_ps(expf_fast(_mm256_mul_ps(dist, factor8)), compareMask);
                    accum = _mm256_add_ps(accum, affinity);
                }
            }
            
            const __m256 denom = _mm256_rcp_ps(_mm256_add_ps(accum, denomAdd8));
            _mm256_storeu_ps(psum + i, denom);
            _mm256_storeu_ps(pt1 + i, _mm256_mul_ps(denom, accum));
        }

        for(int i = 8*n8; i < n; i++) {
            float sumAccum = 0.0f;

            for(int j = 0; j<m; j++) {			
                const float dd1 = x[0*n+i] - t[0*m+j];
                const float dd2 = x[1*n+i] - t[1*m+j];
                const float dd3 = x[2*n+i] - t[2*m+j];
                float dist = dd1*dd1 + dd2*dd2 + dd3*dd3;

                if(dist < thresh)			
                    sumAccum += expf(expFactor * dist);			
            }

            const float rcp = 1.0f / (sumAccum + denomAdd);
            psum[i] = rcp;
            pt1[i] = sumAccum * rcp;
        }
    }

    void cpd_p1px(int m, int n, float thresh, float expFactor, float denomAdd, const float* x, const float* t, const float* psum, float* p1, float* px, const int* trunc_wnd)
    {
        const __m256 factor8 = _mm256_broadcast_ss(&expFactor);
        const __m256 thresh8 = _mm256_broadcast_ss(&thresh);
        const __m256i seq8 = _mm256_setr_epi32(-1, 0, 1, 2, 3, 4, 5, 6); // starting from -1; we simulate a '>=' comparison with '>', for loop_mask

        #pragma omp parallel for schedule(dynamic, 8)
        for (int j = 0; j < m; j++) {
            const __m256 t0 = _mm256_broadcast_ss(t + j);
            const __m256 t1 = _mm256_broadcast_ss(t + j + m);
            const __m256 t2 = _mm256_broadcast_ss(t + j + 2 * m);

            __m256 px0 = _mm256_setzero_ps();
            __m256 px1 = _mm256_setzero_ps();
            __m256 px2 = _mm256_setzero_ps();
            __m256 p1a = _mm256_setzero_ps();
            const __m256i loop_max = _mm256_set1_epi32(trunc_wnd[2 * j + 1]);

            for (int i8 = trunc_wnd[2 * j]; i8 <= trunc_wnd[2 * j + 1]; i8+=8) {
                const __m256i loop_mask = _mm256_cmpgt_epi32(loop_max, _mm256_add_epi32(_mm256_set1_epi32(i8), seq8));

                const __m256 x0 = _mm256_loadu_ps(x + 0 * n + i8);
                const __m256 x1 = _mm256_loadu_ps(x + 1 * n + i8);
                const __m256 x2 = _mm256_loadu_ps(x + 2 * n + i8);
                const __m256 dd1 = _mm256_sub_ps(x0, t0);
                const __m256 dd2 = _mm256_sub_ps(x1, t1);
                const __m256 dd3 = _mm256_sub_ps(x2, t2);

                __m256 dist = _mm256_mul_ps(dd1, dd1);
                dist = _mm256_fmadd_ps(dd2, dd2, dist); // a*b + c, FMA3
                dist = _mm256_fmadd_ps(dd3, dd3, dist);

                //const __m256 compareMask = _mm256_cmp_ps(dist, thresh8, _CMP_LT_OQ);
                const __m256 compareMask = _mm256_and_ps(_mm256_castsi256_ps(loop_mask), _mm256_cmp_ps(dist, thresh8, _CMP_LT_OQ));
                const int mask = _mm256_movemask_ps(compareMask);
                if (mask != 0) {
                    __m256 pmn = expf_fast(_mm256_mul_ps(dist, factor8));
                    pmn = _mm256_mul_ps(pmn, _mm256_loadu_ps(psum + i8));
                    pmn = _mm256_and_ps(pmn, compareMask);

                    px0 = _mm256_fmadd_ps(x0, pmn, px0);
                    px1 = _mm256_fmadd_ps(x1, pmn, px1);
                    px2 = _mm256_fmadd_ps(x2, pmn, px2);
                    p1a = _mm256_add_ps(p1a, pmn);
                }
            }

            p1[j] = reduce_add(p1a);
            px[j] = reduce_add(px0);
            px[j + m] = reduce_add(px1);
            px[j + 2 * m] = reduce_add(px2);
        }
    }
};