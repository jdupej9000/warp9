#include "pca_impl.h"
#include <lapacke.h>
#include "vec_math.h"
#include "cpu_info.h"
#include "utils.h"
#include "math.h"
#include <algorithm>

namespace warpcore::impl
{
    // Notation: n = datapoint count, m = datapoint dimension
    // This is a high-dimensional PCA, it is assumed that n < m.

    void pca_covmat_base(const float** data, const float* mean, const void* allow, int n, int m, float* cov);
    void pca_covmat_avx512(const float** data, const float* mean, const void* allow, int n, int m, float* cov);

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
        constexpr int VEC_WIDTH = 16;
        int n2 = round_down(n, 2);
        int m16 = round_down(m, VEC_WIDTH);
        float norm = 1.0f / (float)(reduce_add_i1(allow, n) - 1);
        const __mmask16* allowb = (const __mmask16*)allow;

        for (int i = 0; i < n; i++) {
            const float* datai = data[i];
            int n2i = ((i & 0x1) == 0) ? n2 : (n2 - 1);

            for (int j = i; j < n2i; j += 2) {
                __m512 a0 = _mm512_setzero_ps();
                __m512 a1 = _mm512_setzero_ps();

                for (int k = 0, ki = 0; k < m16; k += VEC_WIDTH, ki++) {
                    __mmask16 allowMask = allowb[ki];
                    __m512 meank = _mm512_loadu_ps(mean + k);
                    __m512 coli = _mm512_sub_ps(_mm512_loadu_ps(datai + k), meank);
                    __m512 colj0 = _mm512_sub_ps(_mm512_loadu_ps(data[j] + k), meank);
                    __m512 colj1 = _mm512_sub_ps(_mm512_loadu_ps(data[j + 1] + k), meank);
                    a0 = _mm512_mask_fmadd_ps(coli, allowMask, colj0, a0);
                    a1 = _mm512_mask_fmadd_ps(coli, allowMask, colj1, a1);
                }

                float aa0 = 0;
                float aa1 = 0;
                for (int k = m16; k < m; k++) {
                    if ((allowb[k >> 4] >> (k & 0xf)) & 0x1) {
                        const float meank = mean[k];
                        const float coli = datai[k] - meank;
                        const float colj0 = data[j][k] - meank;
                        const float colj1 = data[j + 1][k] - meank;
                        aa0 += coli * colj0;
                        aa1 += coli * colj1;
                    }
                }

                aa0 = (aa0 + reduce_add(a0)) * norm;
                aa1 = (aa1 + reduce_add(a1)) * norm;
                cov[i * n + j] = aa0;
                cov[j * n + i] = aa0;
                cov[i * n + j + 1] = aa1;
                cov[(j + 1) * n + i] = aa1;
            }

            for (int j = n2i; j < n; j++) {
                __m512 a0 = _mm512_setzero_ps();

                for (int k = 0, ki = 0; k < m16; k += VEC_WIDTH, ki++) {
                    __mmask16 allowMask = allowb[ki];
                    __m512 meank = _mm512_loadu_ps(mean + k);
                    __m512 coli = _mm512_sub_ps(_mm512_loadu_ps(datai + k), meank);
                    __m512 colj0 = _mm512_sub_ps(_mm512_loadu_ps(data[j] + k), meank);
                    a0 = _mm512_mask_fmadd_ps(coli, allowMask, colj0, a0);
                }

                float aa0 = 0;
                for (int k = m16; k < m; k++) {
                    if ((allowb[k >> 4] >> (k & 0xf)) & 0x1) {
                        const float meank = mean[k];
                        const float coli = datai[k] - meank;
                        const float colj0 = data[j][k] - meank;
                        aa0 += data[i][k] * data[j][k] - meank;
                    }
                }

                aa0 = (aa0 + reduce_add(a0)) * norm;
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

        std::sort(order, order + n,
            [evals](int a, int b) { return fabs(evals[a]) > fabs(evals[b]); });

        // Use the eigenvectors corresponding to 'npcs' eigenvalues with the largest magnitude
        // and transform centered data with these weights to get the principal vectors.
        #pragma omp parallel for schedule(static, 4)
        for (int i = 0; i < std::min(npcs, n); i++) {
            wsumc(data, mean, cov + order[i] * n, n, m, pcs + i * m);
        }

        // Calculate proportion of explained variance.
        float sumev = reduce_add(evals, n);
        for (int i = 0; i < std::min(npcs, n); i++)
            var[i] = evals[i] / sumev;

        delete[] order;
        delete[] evals;
    }
};