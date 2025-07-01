#include "cpd_impl.h"
#include "vec_math.h"
#include "kmeans.h"
#include "utils.h"
#include <lapacke.h>
#include <cblas.h>
#include <algorithm>
#include <immintrin.h>
#include "cpu_info.h"


namespace warpcore::impl
{
    void cpd_samplefirst_g(const float* y, int m, float beta, float* gi);
    void cpd_sample_g(const float* y, int m, int col, float beta, float* gi);
    void cpd_make_lambda(const float* y, int m, int k, float beta, const float* q, float* lambda);
    void cpd_sigmapart(int m, int n, const float* x, const float* t, float* si2partial);

    void cpd_psumpt1(int i0, int m, int n, float thresh, float expFactor, float denomAdd, const float* x, const float* t, float* psum, float* pt1);
    void cpd_psumpt1_avx2(int m, int n, float thresh, float expFactor, float denomAdd, const float* x, const float* t, float* psum, float* pt1);
    void cpd_psumpt1_avx512(int m, int n, float thresh, float expFactor, float denomAdd, const float* x, const float* t, float* psum, float* pt1);

    void cpd_p1px(int j0, int m, int n, float thresh, float expFactor, float denomAdd, const float* x, const float* t, const float* psum, float* p1, float* px);
    void cpd_p1px_avx2(int m, int n, float thresh, float expFactor, float denomAdd, const float* x, const float* t, const float* psum, float* p1, float* px);
    void cpd_p1px_avx512(int m, int n, float thresh, float expFactor, float denomAdd, const float* x, const float* t, const float* psum, float* p1, float* px);
    
    constexpr bool cpd_is_tier_complete(int x) 
    {
        return (x & 0xff) == 0;
    }

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

        _MM_SET_DENORMALS_ZERO_MODE(_MM_DENORMALS_ZERO_ON);
        _MM_SET_FLUSH_ZERO_MODE(_MM_FLUSH_ZERO_ON);

        // Cluster points y and find points which are closest to cluster centers -> centerIdx.
        kmeans<3>(y, m, k, centers, labels);
        for(int i = 0; i < k; i++) {
            get_row<float, 3>(centers, k, i, ci);
            centerIdx[i] = nearest<3>(y, m, ci);
        }

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
        _MM_SET_DENORMALS_ZERO_MODE(_MM_DENORMALS_ZERO_ON);
        _MM_SET_FLUSH_ZERO_MODE(_MM_FLUSH_ZERO_ON);

        cpd_sigmapart(m, n, x, y, tmp);
        return reduce_add(tmp, n) / (3 * m * n);
    }

    void cpd_estep(const float* x, const float* t, int m, int n, float w, float sigma2, float denom, float* psum, float* pt1, float* p1, float* px)
    {
        _MM_SET_DENORMALS_ZERO_MODE(_MM_DENORMALS_ZERO_ON);
        _MM_SET_FLUSH_ZERO_MODE(_MM_FLUSH_ZERO_ON);

        const float factor = -1.0f / (2.0f * sigma2);
        const float thresh = std::max(0.0001f, 2.0f * sqrtf(sigma2));

        if (get_optpath() >= WCORE_OPTPATH::AVX512)
        {
            cpd_psumpt1_avx512(m, n, thresh, factor, denom, x, t, psum, pt1);
            cpd_p1px_avx512(m, n, thresh, factor, denom, x, t, psum, p1, px);
        }
        else
        {
            cpd_psumpt1_avx2(m, n, thresh, factor, denom, x, t, psum, pt1);
            cpd_p1px_avx2(m, n, thresh, factor, denom, x, t, psum, p1, px);
        }
    }

    float cpd_mstep(const float* y, const float* pt1, const float* p1, const float* px, const float* q, const float* l, const float* linv, int m, int n, int k, float sigma2, float lambda, float* t, float* tmp)
    {
        float* _p0 = tmp;
        float* _p1 = _p0 + m * k;
        float* _p2 = _p1 + m * k;
        float* _p3 = _p2 + m * k;

        _MM_SET_DENORMALS_ZERO_MODE(_MM_DENORMALS_ZERO_ON);
        _MM_SET_FLUSH_ZERO_MODE(_MM_FLUSH_ZERO_ON);

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

        dxa(_p0, p1, m, 3, _p3);
        // _p3 (m,3): diag(p1) * q * [inner] * tf^2 * q^T * diag(p1) * right  [solve1]

        memcpy(_p0, _p2, m * 3 * sizeof(float));
        // _p0 (m,3): [w]

        cblas_saxpy(m*3, -1.0f, _p3, 1, _p2, 1);
        // _p2 (m,3): [w] - [solve1]

        cblas_sgemm(CblasColMajor, CblasTrans, CblasNoTrans, k, 3, m, 1.0f, q, m, _p2, m, 0.0f, _p3, k); 
        // _p3 (k,3): q^T * w

        float ret = lambda / 2 * tratdba(_p3, k, 3, l);

        dxa(_p3, l, k, 3, _p0);
        // _p0 (k,3): diag(l) * q^T * w

        std::memcpy(t, y, m * 3 * sizeof(float));
        cblas_sgemm(CblasColMajor, CblasNoTrans, CblasNoTrans, m, 3, k, 1.0f, q, m, _p0, k, 1.0f, t, m); 
        // T (m,3): q * diag(l) * q^t * w + y

        return ret;
    }

    float cpd_update_sigma2(const float* x, const float* t, const float* pt1, const float* p1, const float* px, int m, int n)
    {
        _MM_SET_DENORMALS_ZERO_MODE(_MM_DENORMALS_ZERO_ON);
        _MM_SET_FLUSH_ZERO_MODE(_MM_FLUSH_ZERO_ON);

        float ret = tratdba(x, n, 3, pt1) + tratdba(t, m, 3, p1);
        ret -= 2 * cblas_sdot(m * 3, px, 1, t, 1); // -= 2 * Matrix.TraceOfProduct(PX, T, true);
        return ret / (3 * reduce_add(p1, m));
    }

    void cpd_samplefirst_g(const float* y, int m, float beta, float* gi)
    {
        _MM_SET_DENORMALS_ZERO_MODE(_MM_DENORMALS_ZERO_ON);
        _MM_SET_FLUSH_ZERO_MODE(_MM_FLUSH_ZERO_ON);

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
        _MM_SET_DENORMALS_ZERO_MODE(_MM_DENORMALS_ZERO_ON);
        _MM_SET_FLUSH_ZERO_MODE(_MM_FLUSH_ZERO_ON);

        const float ef = -0.5f / (beta * beta);
        const __m256 ef8 = _mm256_set1_ps(ef);
      
        const int mch = round_down(m, 8);
       /* for(int i = 0; i < mch; i += 8) {
            __m256 d = _mm256_setzero_ps();
            
            for(int j = 0; j < 3; j++) {
                const __m256 dj = _mm256_sub_ps(_mm256_loadu_ps(y + j*m + i), _mm256_broadcast_ss(y + j*m + col));
                d = _mm256_fmadd_ps(dj, dj, d);
            }

            const __m256 t = expf_fast(_mm256_mul_ps(d, ef8));
            _mm256_storeu_ps(gi + i, t);
        }*/

        for(int i = 0/*mch*/; i < m; i++) {
            float d = 0;
            for(int j = 0; j < 3; j++) {
                const float dj = y[j*m+i] - y[j*m+col];
                d += dj * dj;
            }

            gi[i] = expf_fast(ef * d);
        }
    }

    void cpd_make_lambda(const float* y, int m, int k, float beta, const float* q, float* lambda)
    {
        _MM_SET_DENORMALS_ZERO_MODE(_MM_DENORMALS_ZERO_ON);
        _MM_SET_FLUSH_ZERO_MODE(_MM_FLUSH_ZERO_ON);

        const __m256i order = _mm256_setr_epi32(0, 1, 2, 3, 4, 5, 6, 7);
        const float ef = -0.5f / (beta * beta);
        const __m256 ef8 = _mm256_set1_ps(ef);

        __m256* lambda8 = new __m256[k];
        memset(lambda8, 0, sizeof(__m256) * k);
      
        for (int i = 0; i < m; i++) {
            const __m256 yi0 = _mm256_broadcast_ss(y + i);
            const __m256 yi1 = _mm256_broadcast_ss(y + i + m);
            const __m256 yi2 = _mm256_broadcast_ss(y + i + 2 * m);

            for (int j = 0; j < m; j+=8) {
                const __m256 jmask = _mm256_castsi256_ps(_mm256_cmpgt_epi32(_mm256_set1_epi32(m - j), order));

                const __m256 d0 = _mm256_sub_ps(yi0, _mm256_loadu_ps(y + j));
                const __m256 d1 = _mm256_sub_ps(yi1, _mm256_loadu_ps(y + j + m));
                const __m256 d2 = _mm256_sub_ps(yi2, _mm256_loadu_ps(y + j + 2 * m));

                __m256 dist = _mm256_mul_ps(d0, d0);
                dist = _mm256_fmadd_ps(d1, d1, dist);
                dist = _mm256_fmadd_ps(d2, d2, dist);

                __m256 g = expf_fast(_mm256_mul_ps(dist, ef8));
                g = _mm256_and_ps(g, jmask); // g[m..] := 0

                for (int l = 0; l < k; l++) {
                    lambda8[l] = _mm256_fmadd_ps(_mm256_mul_ps(g, _mm256_loadu_ps(q + j + l * m)), 
                        _mm256_broadcast_ss(q + i + l * m), 
                        lambda8[l]);
                }
            }
        }

        for (int l = 0; l < k; l++)
            lambda[l] = reduce_add(lambda8[l]);

        delete[] lambda8;
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

            __m256 accumb = _mm256_setzero_ps();
            for(int j = 0; j < m; j++) {
                const __m256 d0 = _mm256_sub_ps(x0, _mm256_broadcast_ss(t + j));
                const __m256 d1 = _mm256_sub_ps(x1, _mm256_broadcast_ss(t + j + m));
                const __m256 d2 = _mm256_sub_ps(x2, _mm256_broadcast_ss(t + j + 2*m));

                __m256 a = _mm256_fmadd_ps(d1, d1, _mm256_mul_ps(d0, d0));
                a = _mm256_fmadd_ps(d2, d2, a);
                accumb = _mm256_add_ps(accumb, a);

                if (cpd_is_tier_complete(j)) {
                    accum = _mm256_add_ps(accum, accumb);
                    accumb = _mm256_setzero_ps();
                }
            }
            accum = _mm256_add_ps(accum, accumb);

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

    void cpd_psumpt1(int i0, int m, int n, float thresh, float expFactor, float denomAdd, const float* x, const float* t, float* psum, float* pt1)
    {
        for (int i = i0; i < n; i++) {
            float sumAccum = 0.0f;
            float sumAccumB = 0;
            for (int j = 0; j < m; j++) {
                const float dd1 = x[0 * n + i] - t[0 * m + j];
                const float dd2 = x[1 * n + i] - t[1 * m + j];
                const float dd3 = x[2 * n + i] - t[2 * m + j];
                float dist = dd1 * dd1 + dd2 * dd2 + dd3 * dd3;

                if (dist < thresh)
                    sumAccumB += expf_fast(expFactor * dist);

                if (cpd_is_tier_complete(j)) {
                    sumAccum += sumAccumB;
                    sumAccumB = 0;
                }
            }
            sumAccum += sumAccumB;

            const float rcp = 1.0f / (sumAccum + denomAdd);
            psum[i] = rcp;
            pt1[i] = sumAccum * rcp;
        }
    }

    void cpd_psumpt1_avx2(int m, int n, float thresh, float expFactor, float denomAdd, const float* x, const float* t, float* psum, float* pt1)
    {
        constexpr int BlockSize = 8;
        const __m256 factor8 = _mm256_broadcast_ss(&expFactor);
        const __m256 thresh8 = _mm256_broadcast_ss(&thresh);	
        const __m256 denomAdd8 = _mm256_broadcast_ss(&denomAdd);
        const int nb = round_down(n, BlockSize);

        // Intel hybrid architectures would usually be the ones where we fall back to avx2 code. Dynamic
        // scheduling results in beter utilization there.
        #pragma omp parallel for schedule(dynamic, 8)
        for(int i = 0; i < nb; i += BlockSize) {	
            __m256 accum = _mm256_setzero_ps(), accumb = _mm256_setzero_ps();
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
                    accumb = _mm256_add_ps(accumb, affinity);
                }

                if (cpd_is_tier_complete(j)) {
                    accum = _mm256_add_ps(accum, accumb);
                    accumb = _mm256_setzero_ps();
                }
            }
            accum = _mm256_add_ps(accum, accumb);
            
            const __m256 denom = _mm256_rcp_ps(_mm256_add_ps(accum, denomAdd8));
            _mm256_storeu_ps(psum + i, denom);
            _mm256_storeu_ps(pt1 + i, _mm256_mul_ps(denom, accum));
        }

        cpd_psumpt1(nb, m, n, thresh, expFactor, denomAdd, x, t, psum, pt1);
    }

    void cpd_psumpt1_avx512(int m, int n, float thresh, float expFactor, float denomAdd, const float* x, const float* t, float* psum, float* pt1)
    {
        constexpr int BlockSize = 16;
        const __m512 factorb = _mm512_set1_ps(expFactor);
        const __m512 threshb = _mm512_set1_ps(thresh);
        const __m512 denomAddb = _mm512_set1_ps(denomAdd);
        const int nb = round_down(n, BlockSize);

        #pragma omp parallel for schedule(dynamic, 8)
        for (int i = 0; i < nb; i += BlockSize) {
            __m512 accum = _mm512_setzero_ps(), accumb = _mm512_setzero_ps();
            const __m512 x0 = _mm512_loadu_ps(x + i);
            const __m512 x1 = _mm512_loadu_ps(x + i + n);
            const __m512 x2 = _mm512_loadu_ps(x + i + 2 * n);

            for (int j = 0; j < m; j++) {
                const __m512 d0 = _mm512_sub_ps(_mm512_set1_ps(t[j]), x0);
                const __m512 d1 = _mm512_sub_ps(_mm512_set1_ps(t[j + m]), x1);
                const __m512 d2 = _mm512_sub_ps(_mm512_set1_ps(t[j + 2 * m]), x2);
                __m512 dist = _mm512_mul_ps(d0, d0);
                dist = _mm512_fmadd_ps(d1, d1, dist);
                dist = _mm512_fmadd_ps(d2, d2, dist);

                const __mmask16 compareMask = _mm512_cmplt_ps_mask(dist, threshb);
                if (_cvtmask16_u32(compareMask) != 0) {
                    __m512 affinity = expf_fast(_mm512_mul_ps(dist, factorb));
                    accumb = _mm512_mask_add_ps(accumb, compareMask, accumb, affinity);
                }

                if (cpd_is_tier_complete(j)) {
                    accum = _mm512_add_ps(accum, accumb);
                    accumb = _mm512_setzero_ps();
                }
            }
            accum = _mm512_add_ps(accum, accumb);

            const __m512 denom = _mm512_rcp14_ps(_mm512_add_ps(accum, denomAddb));
            _mm512_storeu_ps(psum + i, denom);
            _mm512_storeu_ps(pt1 + i, _mm512_mul_ps(denom, accum));
        }

        cpd_psumpt1(nb, m, n, thresh, expFactor, denomAdd, x, t, psum, pt1);
    }

    void cpd_p1px(int j0, int m, int n, float thresh, float expFactor, float denomAdd, const float* x, const float* t, const float* psum, float* p1, float* px)
    {
        for (int j = j0; j < m; j++) {
            float px0 = 0, px1 = 0, px2 = 0;
            float p1a = 0.0f;
            const float t0 = t[j],
                t1 = t[m + j],
                t2 = t[2 * m + j];

            for (int i = 0; i < n; i++) {
                const float dd1 = x[0 * n + i] - t0;
                const float dd2 = x[1 * n + i] - t1;
                const float dd3 = x[2 * n + i] - t2;
                float dist = dd1 * dd1 + dd2 * dd2 + dd3 * dd3;

                if (dist < thresh) {
                    const float pmn = expf_fast(expFactor * dist) * psum[i];
                    px0 += pmn * x[0 * n + i];
                    px1 += pmn * x[1 * n + i];
                    px2 += pmn * x[2 * n + i];
                    p1a += pmn;
                }
            }

            p1[j] = p1a;
            px[0 * m + j] = px0;
            px[1 * m + j] = px1;
            px[2 * m + j] = px2;
        }
    }

    void cpd_p1px_avx2(int m, int n, float thresh, float expFactor, float denomAdd, const float* x, const float* t, const float* psum, float* p1, float* px)
    {
        constexpr int BlockSize = 8;
        const __m256 factor8 = _mm256_broadcast_ss(&expFactor);
        const __m256 thresh8 = _mm256_broadcast_ss(&thresh);	
        const int mb = round_down(m, BlockSize);

        #pragma omp parallel for schedule(dynamic, 8)
        for(int j = 0; j < mb; j += BlockSize) {	
            const __m256 t0 = _mm256_loadu_ps(t + j);
            const __m256 t1 = _mm256_loadu_ps(t + j + m);
            const __m256 t2 = _mm256_loadu_ps(t + j + 2*m);

            __m256 px0 = _mm256_setzero_ps(), px0b = _mm256_setzero_ps();
            __m256 px1 = _mm256_setzero_ps(), px1b = _mm256_setzero_ps();
            __m256 px2 = _mm256_setzero_ps(), px2b = _mm256_setzero_ps();
            __m256 p1a = _mm256_setzero_ps(), p1ab = _mm256_setzero_ps();

            for(int i = 0; i < n; i++) {
                const __m256 x0 = _mm256_broadcast_ss(x + 0*n+i);
                const __m256 x1 = _mm256_broadcast_ss(x + 1*n+i);
                const __m256 x2 = _mm256_broadcast_ss(x + 2*n+i);
                const __m256 dd1 = _mm256_sub_ps(x0, t0);
                const __m256 dd2 = _mm256_sub_ps(x1, t1);
                const __m256 dd3 = _mm256_sub_ps(x2, t2);

                __m256 dist = _mm256_mul_ps(dd1, dd1);
                dist = _mm256_fmadd_ps(dd2, dd2, dist); // a*b + c, FMA3
                dist = _mm256_fmadd_ps(dd3, dd3, dist);

                const __m256 compareMask = _mm256_cmp_ps(dist, thresh8, _CMP_LT_OQ);
                const int mask = _mm256_movemask_ps(compareMask);
                if (mask != 0) {
                    __m256 pmn = expf_fast(_mm256_mul_ps(dist, factor8));
                    pmn = _mm256_mul_ps(pmn, _mm256_broadcast_ss(psum + i));
                    pmn = _mm256_and_ps(pmn, compareMask);

                    px0b = _mm256_fmadd_ps(x0, pmn, px0b);
                    px1b = _mm256_fmadd_ps(x1, pmn, px1b);
                    px2b = _mm256_fmadd_ps(x2, pmn, px2b);
                    p1ab = _mm256_add_ps(p1ab, pmn);
                }

                if (cpd_is_tier_complete(i)) {
                    px0 = _mm256_add_ps(px0, px0b); px0b = _mm256_setzero_ps();
                    px1 = _mm256_add_ps(px1, px1b); px1b = _mm256_setzero_ps();
                    px2 = _mm256_add_ps(px2, px2b); px2b = _mm256_setzero_ps();
                    p1a = _mm256_add_ps(p1a, p1ab); p1ab = _mm256_setzero_ps();
                }
            }
            px0 = _mm256_add_ps(px0, px0b);
            px1 = _mm256_add_ps(px1, px1b);
            px2 = _mm256_add_ps(px2, px2b);
            p1a = _mm256_add_ps(p1a, p1ab); 
            
            _mm256_storeu_ps(p1 + j, p1a);
            _mm256_storeu_ps(px + j, px0);
            _mm256_storeu_ps(px + j + m, px1);
            _mm256_storeu_ps(px + j + 2*m, px2);
        }

        cpd_p1px(mb, m, n, thresh, expFactor, denomAdd, x, t, psum, p1, px);
    }

    void cpd_p1px_avx512(int m, int n, float thresh, float expFactor, float denomAdd, const float* x, const float* t, const float* psum, float* p1, float* px)
    {
        constexpr int BlockSize = 16;
        const __m512 factorb = _mm512_set1_ps(expFactor);
        const __m512 threshb = _mm512_set1_ps(thresh);
        const int mb = round_down(m, BlockSize);

        #pragma omp parallel for schedule(dynamic, 8)
        for (int j = 0; j < mb; j += BlockSize) {
            const __m512 t0 = _mm512_loadu_ps(t + j);
            const __m512 t1 = _mm512_loadu_ps(t + j + m);
            const __m512 t2 = _mm512_loadu_ps(t + j + 2 * m);

            __m512 px0 = _mm512_setzero_ps(), px0b = _mm512_setzero_ps();
            __m512 px1 = _mm512_setzero_ps(), px1b = _mm512_setzero_ps();
            __m512 px2 = _mm512_setzero_ps(), px2b = _mm512_setzero_ps();
            __m512 p1a = _mm512_setzero_ps(), p1ab = _mm512_setzero_ps();

            for (int i = 0; i < n; i++) {
                const __m512 x0 = _mm512_set1_ps(x[i]);
                const __m512 x1 = _mm512_set1_ps(x[n + i]);
                const __m512 x2 = _mm512_set1_ps(x[2 * n + i]);
                const __m512 dd1 = _mm512_sub_ps(x0, t0);
                const __m512 dd2 = _mm512_sub_ps(x1, t1);
                const __m512 dd3 = _mm512_sub_ps(x2, t2);

                __m512 dist = _mm512_mul_ps(dd1, dd1);
                dist = _mm512_fmadd_ps(dd2, dd2, dist); // a*b + c, FMA3
                dist = _mm512_fmadd_ps(dd3, dd3, dist);

                const __mmask16 compareMask = _mm512_cmplt_ps_mask(dist, threshb);
                if (_cvtmask16_u32(compareMask) != 0) {
                    __m512 pmn = expf_fast(_mm512_mul_ps(dist, factorb));
                    pmn = _mm512_mul_ps(pmn, _mm512_set1_ps(psum[i]));
                    pmn = _mm512_maskz_mov_ps(compareMask, pmn);

                    px0b = _mm512_fmadd_ps(x0, pmn, px0b);
                    px1b = _mm512_fmadd_ps(x1, pmn, px1b);
                    px2b = _mm512_fmadd_ps(x2, pmn, px2b);
                    p1ab = _mm512_add_ps(p1ab, pmn);
                }

                if (cpd_is_tier_complete(i)) {
                    px0 = _mm512_add_ps(px0, px0b); px0b = _mm512_setzero_ps();
                    px1 = _mm512_add_ps(px1, px1b); px1b = _mm512_setzero_ps();
                    px2 = _mm512_add_ps(px2, px2b); px2b = _mm512_setzero_ps();
                    p1a = _mm512_add_ps(p1a, p1ab); p1ab = _mm512_setzero_ps();
                }
            }
            px0 = _mm512_add_ps(px0, px0b);
            px1 = _mm512_add_ps(px1, px1b);
            px2 = _mm512_add_ps(px2, px2b);
            p1a = _mm512_add_ps(p1a, p1ab);

            _mm512_storeu_ps(p1 + j, p1a);
            _mm512_storeu_ps(px + j, px0);
            _mm512_storeu_ps(px + j + m, px1);
            _mm512_storeu_ps(px + j + 2 * m, px2);
        }

        cpd_p1px(mb, m, n, thresh, expFactor, denomAdd, x, t, psum, p1, px);
    }
};