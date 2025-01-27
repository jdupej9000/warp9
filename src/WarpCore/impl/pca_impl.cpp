#include "pca_impl.h"
#include <lapacke.h>
#include <cblas.h>
#include "vec_math.h"
#include "cpu_info.h"
#include "utils.h"
#include "math.h"
#include <algorithm>

namespace warpcore::impl
{
    // PCA for high-dimensional data
    // see Bishop CM: Pattern Recognition and Machine Learning, 2007 -  section 12.1.4
    // Notation: n = datapoint count, m = datapoint dimension

    void pca_covmat_base(const float** data, const float* mean, const void* allow, int n, int m, float* cov);
    void pca_covmat_avx512(const float** data, const float* mean, const void* allow, int n, int m, float* cov);
    float dot_centered_pred(const float* x, const float* y, const float* x0, const void* allow, int n);

    void pca_mean(const float** data, int n, int m, float* mean)
    {
        constexpr int BlockSize = 8;
        int mb = round_down(m, BlockSize);

        memset(mean, 0, sizeof(float) * n);
        for (int i = 0; i < n; i++) {
            const float* datai = data[i];

            for (int j = 0; j < mb; j += BlockSize) {
                __m256 t = _mm256_add_ps(_mm256_loadu_ps(datai + j), _mm256_loadu_ps(mean + j));
                _mm256_storeu_ps(mean + j, t);
            }

            for (int j = mb; j < m; j++)
                mean[j] += datai[j];
        }

        float f = 1.0f / (float)n;
        __m256 fb = _mm256_set1_ps(f);
        for (int j = 0; j < mb; j += BlockSize) {
            __m256 t = _mm256_mul_ps(_mm256_loadu_ps(mean + j), fb);
            _mm256_storeu_ps(mean + j, t);
        }

        for (int j = mb; j < m; j++)
            mean[j] *= f;
    }

    void pca_covmat(const float** data, const float* mean, const void* allow, int n, int m, float* cov)
    {
        if (get_optpath() >= WCORE_OPTPATH::AVX512)
            pca_covmat_avx512(data, mean, allow, n, m, cov);
        else
            pca_covmat_base(data, mean, allow, n, m, cov);
    }

    void pca_covmat_base(const float** data, const float* mean, const void* allow, int n, int m, float* cov)
    {
        float norm = 1.0f / (float)(reduce_add_i1(allow, n) - 1);
        const int* allowq = (const int*)allow;

        for (int i = 0; i < n; i++) {
            const float* datai = data[i];

            for (int j = i; j < n; j++) {
                const float* dataj = data[j];
                float aa0 = 0;
                for (int k = 0; k < m; k++) {
                    if ((allowq[k >> 5] >> (k & 0x1f)) & 0x1) {
                        aa0 += (datai[k] - mean[k]) * (dataj[k] - mean[k]);
                    }
                }
                aa0 *= norm;
                cov[i * n + j] = aa0;
                cov[j * n + i] = aa0;
            }
        }
    }

	void pca_covmat_avx512(const float** data, const float* mean, const void* allow, int n, int m, float* cov)
	{
        constexpr int BlockSize = 16;
        int n2 = round_down(n, 2);
        int m16 = round_down(m, BlockSize);
        float norm = 1.0f / (float)(reduce_add_i1(allow, n) - 1);
        const __mmask16* allowb = (const __mmask16*)allow;

        for (int i = 0; i < n; i++) {
            const float* datai = data[i];
            int n2i = ((i & 0x1) == 0) ? n2 : (n2 - 1);

            for (int j = i; j < n2i; j += 2) {
                __m512 a0 = _mm512_setzero_ps();
                __m512 a1 = _mm512_setzero_ps();

                for (int k = 0, ki = 0; k < m16; k += BlockSize, ki++) {
                    int numOutOfRangeItems = std::max(0, k + 16 - m);
                    __mmask16 allowMask = allowb[ki] & (0xff >> numOutOfRangeItems);
                    __m512 meank = _mm512_loadu_ps(mean + k);
                    __m512 coli = _mm512_sub_ps(_mm512_loadu_ps(datai + k), meank);
                    __m512 colj0 = _mm512_sub_ps(_mm512_loadu_ps(data[j] + k), meank);
                    __m512 colj1 = _mm512_sub_ps(_mm512_loadu_ps(data[j + 1] + k), meank);
                    a0 = _mm512_mask_fmadd_ps(coli, allowMask, colj0, a0);
                    a1 = _mm512_mask_fmadd_ps(coli, allowMask, colj1, a1);
                }

                float aa0 = reduce_add(a0) * norm;
                float aa1 = reduce_add(a1) * norm;
                cov[i * n + j] = aa0;
                cov[j * n + i] = aa0;
                cov[i * n + j + 1] = aa1;
                cov[(j + 1) * n + i] = aa1;
            }

            for (int j = n2i; j < n; j++) {
                __m512 a0 = _mm512_setzero_ps();

                for (int k = 0, ki = 0; k < m16; k += BlockSize, ki++) {
                    int numOutOfRangeItems = std::max(0, k + 16 - m);
                    __mmask16 allowMask = allowb[ki] & (0xff >> numOutOfRangeItems);
                    __m512 meank = _mm512_loadu_ps(mean + k);
                    __m512 coli = _mm512_sub_ps(_mm512_loadu_ps(datai + k), meank);
                    __m512 colj0 = _mm512_sub_ps(_mm512_loadu_ps(data[j] + k), meank);
                    a0 = _mm512_mask_fmadd_ps(coli, allowMask, colj0, a0);
                }

                float aa0 = reduce_add(a0) * norm;
                cov[i * m + j] = aa0;
                cov[j * m + i] = aa0;
            }
        }
	}

    void pca_make_pcs(const float** data, const float* mean, float* cov, int n, int m, int npcs, float* var, float* pcs)
    {
        float* evals = new float[n];
        LAPACKE_ssyev(LAPACK_COL_MAJOR, 'V', 'U', n, cov, n, evals) <= 0;

        // Find the order that sorts the eigenvalues in a descending order of magnitude.
        int* order = new int[n];
        for (int i = 0; i < n; i++)
            order[i] = i;

        // Absolute values for eigenvalues are ommitted as the matrix should be positive-semidefinite.
        std::sort(order, order + n,
            [evals](int a, int b) { return evals[a] > evals[b]; });

        // Use the eigenvectors corresponding to 'npcs' eigenvalues with the largest magnitude
        // and transform centered data with these weights to get the principal vectors.
        #pragma omp parallel for schedule(static, 4)
        for (int i = 0; i < std::min(npcs, n); i++) {
            wsumc(data, mean, cov + order[i] * n, n, m, pcs + i * m);
        }

        // Calculate proportion of explained variance if desired.
        if (var != nullptr) {
            for (int i = 0; i < n; i++)
                evals[i] = sqrtf(evals[i]);

            float sumev = reduce_add(evals, n);
            for (int i = 0; i < std::min(npcs, n); i++)
                var[i] = evals[i] / sumev;
        }

        delete[] order;
        delete[] evals;
    }

    float dot_centered_pred(const float* x, const float* y, const float* x0, const void* allow, int n)
    {
        // TODO: avx512 after proving the following is correct
        const int* allowq = (const int*)allow;
        float ret = 0;
        for (int i = 0; i < n; i++) {
            if ((allowq[i >> 5] >> (i & 0x1f)) & 0x1)
                ret += (x[i] - x0[i]) * y[i];
        }

        return ret;
    }

    void pca_make_scores(const float* x, const float* mean, const float* pcs, const void* allow, int n, int m, float* sc)
    {
        // sc := pcs * (x - mean) with rows predicated on allow
        for (int i = 0; i < n; i++)
            sc[i] = dot_centered_pred(x, pcs + i * m, mean, allow, m);
    }

    void pca_predict(const float* scores, const float* mean, const float* pcs, int n, int m, float* x)
    {
        // x := scores * pcs + mean
        memcpy(x, mean, sizeof(float) * m);
        cblas_sgemm(CblasColMajor, CblasNoTrans, CblasNoTrans, m, 1, n, 1.0f, pcs, n, scores, n, 1.0f, x, n);
    }
};