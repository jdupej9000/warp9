#include "vec_math.h"
#include "utils.h"
#include "cpu_info.h"

#include <algorithm>

namespace warpcore::impl
{
    void atdba_avx2(const float* a, int n, int m, const float* b, float alpha, float* y);
    void atdba_avx512(const float* a, int n, int m, const float* b, float alpha, float* y);

    __m256 expf_fast(__m256 x)
    {
        // adapted from http://software-lisc.fbk.eu/avx_mathfun/avx_mathfun.h
        x = _mm256_max_ps(x, _mm256_set1_ps(-80));

        __m256 t, f, p, r;
        __m256i i, j;

        const __m256 l2e = _mm256_set1_ps(1.442695041f); /* log2(e) */
        const __m256 cvt = _mm256_set1_ps(12582912.0f);  /* 1.5 * (1 << 23) */
        const __m256 c0 = _mm256_set1_ps(0.238428936f);
        const __m256 c1 = _mm256_set1_ps(0.703448006f);
        const __m256 c2 = _mm256_set1_ps(1.000443142f);

        /* exp(x) = 2^i * 2^f; i = rint (log2(e) * x), -0.5 <= f <= 0.5 */
        t = _mm256_mul_ps(x, l2e);             /* t = log2(e) * x */
        r = _mm256_sub_ps(_mm256_add_ps(t, cvt), cvt); /* r = rint (t) */
        f = _mm256_sub_ps(t, r);               /* f = t - rint (t) */
        i = _mm256_cvtps_epi32(t);             /* i = (int)t */
        p = _mm256_fmadd_ps(c0, f, c1);
        p = _mm256_fmadd_ps(p, f, c2);			/* p = (c0 * f + c1) * f + c2 ~= exp2(f) */

        j = _mm256_slli_epi32(i, 23);          /* i << 23 */
        r = _mm256_castsi256_ps(_mm256_add_epi32(j, _mm256_castps_si256(p))); /* r = p * 2^i*/
        return r;
    }

    __m256 expf_schraudolph(__m256 x)
    {
		__m256 a = _mm256_set1_ps(12102203.0f); /* (1 << 23) / log(2) */
		__m256i b = _mm256_set1_epi32(127 * (1 << 23) - 298765);
		__m256i t = _mm256_add_epi32(_mm256_cvtps_epi32(_mm256_mul_ps(a, x)), b);
		return _mm256_castsi256_ps(t);
    }

    double reduce_add(__m256d v) 
    {
        __m128d vlow = _mm256_castpd256_pd128(v);
        __m128d vhigh = _mm256_extractf128_pd(v, 1); 
        vlow = _mm_add_pd(vlow, vhigh);    

        __m128d high64 = _mm_unpackhi_pd(vlow, vlow);
        return  _mm_cvtsd_f64(_mm_add_sd(vlow, high64)); 
    }

    float reduce_add(__m256 v) 
    {
        __m128 reduce = _mm_add_ps(_mm256_extractf128_ps(v, 0), _mm256_extractf128_ps(v, 1));
        reduce = _mm_hadd_ps(reduce, reduce);
        reduce = _mm_hadd_ps(reduce, reduce);
        return _mm_cvtss_f32(reduce);
    }

    float reduce_add(__m512 v)
    {
        __m128 reduce = _mm_add_ps(_mm512_extractf32x4_ps(v, 0), _mm512_extractf32x4_ps(v, 1));
        reduce = _mm_add_ps(reduce, _mm_add_ps(_mm512_extractf32x4_ps(v, 2), _mm512_extractf32x4_ps(v, 3)));
        reduce = _mm_hadd_ps(reduce, reduce);
        reduce = _mm_hadd_ps(reduce, reduce);
        return _mm_cvtss_f32(reduce);
    }

    int reduce_add_i32(__m256i v) 
    {
        __m128i reduce = _mm_add_epi32(_mm256_extracti128_si256(v, 0), _mm256_extracti128_si256(v, 1));
        reduce = _mm_hadd_epi32(reduce, reduce);
        reduce = _mm_hadd_epi32(reduce, reduce);
        return _mm_cvtsi128_si32 (reduce);
    }

    float reduce_add(const float* x, int n)
    {
        __m256 sum = _mm256_setzero_ps();

        int n8 = round_down(n, 8);
        for(int i = 0; i < n8; i+=8)
            sum = _mm256_add_ps(sum, _mm256_loadu_ps(x + i));

        float ret = 0;
        for(int i = n8; i < n; i++)
            ret += x[i];

        return reduce_add(sum) + ret;
    }

    float reduce_add_i32(const int* x, int n)
    {
        __m256i sum = _mm256_setzero_si256();

        int n8 = round_down(n, 8);
        for(int i = 0; i < n8; i+=8)
            sum = _mm256_add_epi32(sum, _mm256_loadu_si256((const __m256i*)(x + i)));

        int ret = 0;
        for(int i = n8; i < n; i++)
            ret += x[i];

        return reduce_add_i32(sum) + ret;
    }

    float reduce_min(__m256 v)
    {
        __m128 reduce = _mm_min_ps(_mm256_extractf128_ps(v, 0), _mm256_extractf128_ps(v, 1));
        reduce = _mm_min_ps(reduce, _mm_movehl_ps(reduce, reduce));
        reduce = _mm_min_ps(reduce, _mm_movehdup_ps(reduce));
        return _mm_cvtss_f32(reduce);
    }

    float reduce_max(__m256 v)
    {
        __m128 reduce = _mm_max_ps(_mm256_extractf128_ps(v, 0), _mm256_extractf128_ps(v, 1));
        reduce = _mm_max_ps(reduce, _mm_movehl_ps(reduce, reduce));
        reduce = _mm_max_ps(reduce, _mm_movehdup_ps(reduce));
        return _mm_cvtss_f32(reduce);
    }

    float extract(__m256 v, int index)
    {
        alignas(32) float vv[8];
        _mm256_store_ps(vv, v);
        return vv[index];
    }

    int extract(__m256i v, int index)
    {
        alignas(32) int vv[8];
        _mm256_store_si256((__m256i*)vv, v);
        return vv[index];
    }

    int find_min_index(__m256 v)
    {
        __m128 reduce = _mm_min_ps(_mm256_extractf128_ps(v, 0), _mm256_extractf128_ps(v, 1));
        reduce = _mm_min_ps(reduce, _mm_movehl_ps(reduce, reduce));
        reduce = _mm_min_ps(reduce, _mm_movehdup_ps(reduce));

        __m256 minsel = _mm256_cmp_ps(v, _mm256_broadcastss_ps(reduce), _CMP_EQ_OQ);
        return _tzcnt_u32(_mm256_movemask_ps(minsel));
    }

    void reduce_minmax(const float* x, int n, float* xmin, float* xmax)
    {
        __m256 xmin8 = _mm256_set1_ps(*xmin);
        __m256 xmax8 = _mm256_set1_ps(*xmax);

        int n8 = round_down(n, 8);
        for(int i = 0; i < n8; i+= 8) {
            const __m256 xi = _mm256_loadu_ps(x + i);
            xmin8 = _mm256_min_ps(xmin8, xi);
            xmax8 = _mm256_max_ps(xmax8, xi);
        }

        float xmin1 = reduce_min(xmin8);
        float xmax1 = reduce_max(xmax8);
        for(int i = n8; i < n; i++) {
            xmin1 = std::min(x[i], xmin1);
            xmax1 = std::max(x[i], xmax1);
        }

        *xmin = xmin1;
        *xmax = xmax1;
    }

    __m256i clamp(__m256i x, __m256i x0, __m256i x1)
    {
        return _mm256_max_epi32(x0, _mm256_min_epi32(x1, x));
    }

    void dxa(const float* x, const float* v, int n, int m, float* y)
    {
        // diag(V) * X
        //   X is n x m (col. major)
        //   V is n
        const int n8 = round_down(n, 8);

        for(int i = 0; i < m; i++) {
            for(int j = 0; j < n8; j+=8) {
                const __m256 t = _mm256_mul_ps(_mm256_loadu_ps(x + i * n + j), _mm256_loadu_ps(v + j));
                _mm256_storeu_ps(y + i * n + j, t);
            }

            for(int j = n8; j < n; j++)
                y[i * n + j] = x[i * n + j] * v[j];
        }
    }

    void dxinva(const float* x, const float* v, int n, int m, float* y)
    {
        // diag(V)^-1 * X
        const int n8 = round_down(n, 8);

         for(int i = 0; i < m; i++) {
            for(int j = 0; j < n8; j+=8) {
                const __m256 t = _mm256_div_ps(_mm256_loadu_ps(x + i * n + j), _mm256_loadu_ps(v + j));
                _mm256_storeu_ps(y + i * n + j, t);
            }

            for(int j = n8; j < n; j++)
                y[i * n + j] = x[i * n + j] / v[j];
        }
    }

    void atdba(const float* a, int n, int m, const float* b, float alpha, float* y)
    {
        if (get_optpath() >= OPT_PATH::AVX512)
            atdba_avx512(a, n, m, b, alpha, y);
        else
            atdba_avx2(a, n, m, b, alpha, y);
    }

    void atdba_avx512(const float* a, int n, int m, const float* b, float alpha, float* y)
    {
        // alpha * A' * diag(B) * A
        constexpr int VEC_WIDTH = 16;
        int m2 = round_down(m, 2);
        int n16 = round_down(n, VEC_WIDTH);
      
        for(int i = 0; i < m; i++) {
            int m2i = ((i & 0x1) == 0) ? m2 : (m2 - 1);
            for(int j = i; j < m2i; j+=2) {
                __m512 a0 = _mm512_setzero_ps();
                __m512 a1 = _mm512_setzero_ps();
                for(int k = 0; k < n16; k+= VEC_WIDTH) {
                    const __m512 aai = _mm512_mul_ps(_mm512_loadu_ps(a + k + i * n), _mm512_loadu_ps(b + k));
                    a0 = _mm512_fmadd_ps(aai, _mm512_loadu_ps(a + k + j * n), a0);
                    a1 = _mm512_fmadd_ps(aai, _mm512_loadu_ps(a + k + (j+1) * n), a1);
                }

                float aa0 = reduce_add(a0);
                float aa1 = reduce_add(a1);
                for(int k = n16; k < n; k++) {
                    const float aai = a[k + i * n] * b[k];
                    aa0 += aai * a[k + j * n];
                    aa1 += aai * a[k + (j + 1) * n];
                }

                aa0 *= alpha;
                aa1 *= alpha;
                y[i * m + j] = aa0;
                y[j * m + i] = aa0;
                y[i * m + j + 1] = aa1;
                y[(j + 1) * m + i] = aa1;
            }

            for(int j = m2i; j < m; j++) {
                __m512 a0 = _mm512_setzero_ps();
                for(int k = 0; k < n16; k+= VEC_WIDTH) {
                    const __m512 aai = _mm512_mul_ps(_mm512_loadu_ps(a + k + i * n), _mm512_loadu_ps(b + k));
                    a0 = _mm512_fmadd_ps(aai, _mm512_loadu_ps(a + k + j * n), a0);
                }

                float aa0 = reduce_add(a0);
                for(int k = n16; k < n; k++) {
                    const float aai = a[k + i * n] * b[k];
                    aa0 += aai * a[k + j * n];
                }

                aa0 *= alpha;
                y[i * m + j] = aa0;
                y[j * m + i] = aa0;
            }
        }
    }

    void atdba_avx2(const float* a, int n, int m, const float* b, float alpha, float* y)
    {
        // alpha * A' * diag(B) * A
        int m2 = round_down(m, 2);
        int n8 = round_down(n, 8);

        for (int i = 0; i < m; i++) {
            int m2i = ((i & 0x1) == 0) ? m2 : (m2 - 1);
            for (int j = i; j < m2i; j += 2) {
                __m256 a0 = _mm256_setzero_ps();
                __m256 a1 = _mm256_setzero_ps();
                for (int k = 0; k < n8; k += 8) {
                    const __m256 aai = _mm256_mul_ps(_mm256_loadu_ps(a + k + i * n), _mm256_loadu_ps(b + k));
                    a0 = _mm256_fmadd_ps(aai, _mm256_loadu_ps(a + k + j * n), a0);
                    a1 = _mm256_fmadd_ps(aai, _mm256_loadu_ps(a + k + (j + 1) * n), a1);
                }

                float aa0 = reduce_add(a0);
                float aa1 = reduce_add(a1);
                for (int k = n8; k < n; k++) {
                    const float aai = a[k + i * n] * b[k];
                    aa0 += aai * a[k + j * n];
                    aa1 += aai * a[k + (j + 1) * n];
                }

                aa0 *= alpha;
                aa1 *= alpha;

                y[i * m + j] = aa0;
                y[j * m + i] = aa0;
                y[i * m + j + 1] = aa1;
                y[(j + 1) * m + i] = aa1;
            }

            for (int j = m2i; j < m; j++) {
                __m256 a0 = _mm256_setzero_ps();
                for (int k = 0; k < n8; k += 8) {
                    const __m256 aai = _mm256_mul_ps(_mm256_loadu_ps(a + k + i * n), _mm256_loadu_ps(b + k));
                    a0 = _mm256_fmadd_ps(aai, _mm256_loadu_ps(a + k + j * n), a0);
                }

                float aa0 = reduce_add(a0);
                for (int k = n8; k < n; k++) {
                    const float aai = a[k + i * n] * b[k];
                    aa0 += aai * a[k + j * n];
                }

                aa0 *= alpha;
                y[i * m + j] = aa0;
                y[j * m + i] = aa0;
            }
        }
    }

    float tratdba(const float* a, int n, int m, const float* b)
    {
        // trace(A' * diag(B) * A)

        float ret = 0;
        /*for (int i = 0; i < m; i++) {
            for (int k = 0; k < n; k++) {
                float ak = a[i * n + k];
                ret += ak * ak * b[k];
            }                
        }*/

        int n16 = round_down(n, 16);
        for(int i = 0; i < m; i++) {
            __m256 accum = _mm256_setzero_ps();
            
            for(int j = 0; j < n16; j+=16) {
                const __m256 aj0 = _mm256_loadu_ps(a + j + i * n);
                const __m256 bj0 = _mm256_loadu_ps(b + j);

                const __m256 aj1 = _mm256_loadu_ps(a + j + i * n + 8);
                const __m256 bj1 = _mm256_loadu_ps(b + j + 8);

                const __m256 p0 = _mm256_mul_ps(_mm256_mul_ps(aj0, aj0), bj0);
                const __m256 p1 = _mm256_mul_ps(_mm256_mul_ps(aj1, aj1), bj1);

                accum = _mm256_add_ps(accum, _mm256_add_ps(p0, p1));
            }

            float part = reduce_add(accum);
            for(int j = n16; j < n; j++) {
                const float aj = a[j + i * n];
                part += aj * aj * b[j]; 
            }

            ret += part;
        }

        return ret;
    }
};
