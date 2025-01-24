#include "pca_impl.h"
#include <lapacke.h>
#include "vec_math.h"
#include "utils.h"

namespace warpcore::impl
{
    void pca_covmat(const float** data, const void* allow, int n, int m, float* cov)
    {
        float norm = 1.0f / (float)reduce_add_i1(allow, n);
        const int* allowq = (const int*)allow;

        for (int i = 0; i < m; i++) {
            for (int j = i; j < m; j++) {
                float aa0 = 0;
                for (int k = 0; k < n; k++) {
                    if ((allowq[k >> 5] >> (k & 0x1f)) & 0x1) {
                        const float coli = data[i][k];
                        const float colj0 = data[j][k];
                        aa0 += data[i][k] * data[j][k];
                    }
                }
                aa0 *= norm;
                cov[i * m + j] = aa0;
                cov[j * m + i] = aa0;
            }
        }
    }

	void pca_covmat_avx512(const float** data, const void* allow, int n, int m, float* cov)
	{
        constexpr int VEC_WIDTH = 16;
        int m2 = round_down(m, 2);
        int n16 = round_down(n, VEC_WIDTH);
        float norm = 1.0f / (float)reduce_add_i1(allow, n);
        const __mmask16* allowb = (const __mmask16*)allow;

        for (int i = 0; i < m; i++) {
            int m2i = ((i & 0x1) == 0) ? m2 : (m2 - 1);
            for (int j = i; j < m2i; j += 2) {
                __m512 a0 = _mm512_setzero_ps();
                __m512 a1 = _mm512_setzero_ps();

                for (int k = 0, ki = 0; k < n16; k += VEC_WIDTH, ki++) {
                    __mmask16 allowMask = allowb[ki];
                    __m512 coli = _mm512_loadu_ps(data[i] + k);
                    __m512 colj0 = _mm512_loadu_ps(data[j] + k);
                    __m512 colj1 = _mm512_loadu_ps(data[j + 1] + k);
                    a0 = _mm512_mask_fmadd_ps(coli, allowMask, colj0, a0);
                    a1 = _mm512_mask_fmadd_ps(coli, allowMask, colj1, a1);
                }

                float aa0 = 0;
                float aa1 = 0;
                for (int k = n16; k < n; k++) {
                    if ((allowb[k >> 4] >> (k & 0xf)) & 0x1) {
                        const float coli = data[i][k];
                        const float colj0 = data[j][k];
                        const float colj1 = data[j + 1][k];
                        aa0 += coli * colj0;
                        aa1 += coli * colj1;
                    }
                }

                aa0 = (aa0 + reduce_add(a0)) * norm;
                aa1 = (aa1 + reduce_add(a1)) * norm;
                cov[i * m + j] = aa0;
                cov[j * m + i] = aa0;
                cov[i * m + j + 1] = aa1;
                cov[(j + 1) * m + i] = aa1;
            }

            for (int j = m2i; j < m; j++) {
                __m512 a0 = _mm512_setzero_ps();

                for (int k = 0, ki = 0; k < n16; k += VEC_WIDTH, ki++) {
                    __mmask16 allowMask = allowb[ki];
                    __m512 coli = _mm512_loadu_ps(data[i] + k);
                    __m512 colj0 = _mm512_loadu_ps(data[j] + k);
                    a0 = _mm512_mask_fmadd_ps(coli, allowMask, colj0, a0);
                }

                float aa0 = 0;
                for (int k = n16; k < n; k++) {
                    if ((allowb[k >> 4] >> (k & 0xf)) & 0x1) {
                        const float coli = data[i][k];
                        const float colj0 = data[j][k];
                        aa0 += data[i][k] * data[j][k];
                    }
                }

                aa0 = (aa0 + reduce_add(a0)) * norm;
                cov[i * m + j] = aa0;
                cov[j * m + i] = aa0;
            }
        }
	}
};