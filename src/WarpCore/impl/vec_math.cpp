#include "vec_math.h"
#include "utils.h"
#include "cpu_info.h"
#include <math.h>

#include <algorithm>

namespace warpcore::impl
{
    void atdba_avx2(const float* a, int n, int m, const float* b, float alpha, float* y);
    void atdba_avx512(const float* a, int n, int m, const float* b, float alpha, float* y);

    __m256 _mm256_abs_ps(__m256 x)
    {
        return _mm256_andnot_ps(_mm256_set1_ps(-0.0f), x);
    }

    float WCORE_VECCALL expf_fast(float xx)
    {
        //return expf(xx);

        __m128 x = _mm_set_ss(xx);

        // adapted from http://software-lisc.fbk.eu/avx_mathfun/avx_mathfun.h
        x = _mm_max_ss(x, _mm_set_ss(-80));

        __m128 t, f, p, r;
        __m128i i, j;
        const __m128 l2e = _mm_set_ss(1.442695041f); /* log2(e) */
        const __m128 cvt = _mm_set_ss(12582912.0f);  /* 1.5 * (1 << 23) */
        const __m128 c0 = _mm_set_ss(0.238428936f);
        const __m128 c1 = _mm_set_ss(0.703448006f);
        const __m128 c2 = _mm_set_ss(1.000443142f);

        /* exp(x) = 2^i * 2^f; i = rint (log2(e) * x), -0.5 <= f <= 0.5 */
        t = _mm_mul_ss(x, l2e);             /* t = log2(e) * x */
        r = _mm_sub_ss(_mm_add_ss(t, cvt), cvt); /* r = rint (t) */
        f = _mm_sub_ss(t, r);               /* f = t - rint (t) */
        i = _mm_cvtps_epi32(t);             /* i = (int)t */
        p = _mm_fmadd_ss(c0, f, c1);
        p = _mm_fmadd_ss(p, f, c2);			/* p = (c0 * f + c1) * f + c2 ~= exp2(f) */

        j = _mm_slli_epi32(i, 23);          /* i << 23 */
        r = _mm_castsi128_ps(_mm_add_epi32(j, _mm_castps_si128(p))); /* r = p * 2^i*/
        return _mm_cvtss_f32(r);
    }

    __m256 WCORE_VECCALL expf_fast(__m256 x)
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

    __m512 WCORE_VECCALL expf_fast(__m512 x)
    {
        // adapted from http://software-lisc.fbk.eu/avx_mathfun/avx_mathfun.h
        x = _mm512_max_ps(x, _mm512_set1_ps(-80));

        __m512 t, f, p, r;
        __m512i i, j;
        const __m512 l2e = _mm512_set1_ps(1.442695041f); /* log2(e) */
        const __m512 cvt = _mm512_set1_ps(12582912.0f);  /* 1.5 * (1 << 23) */
        const __m512 c0 = _mm512_set1_ps(0.238428936f);
        const __m512 c1 = _mm512_set1_ps(0.703448006f);
        const __m512 c2 = _mm512_set1_ps(1.000443142f);

        /* exp(x) = 2^i * 2^f; i = rint (log2(e) * x), -0.5 <= f <= 0.5 */
        t = _mm512_mul_ps(x, l2e);             /* t = log2(e) * x */
        r = _mm512_sub_ps(_mm512_add_ps(t, cvt), cvt); /* r = rint (t) */
        f = _mm512_sub_ps(t, r);               /* f = t - rint (t) */
        i = _mm512_cvtps_epi32(t);             /* i = (int)t */
        p = _mm512_fmadd_ps(c0, f, c1);
        p = _mm512_fmadd_ps(p, f, c2);			/* p = (c0 * f + c1) * f + c2 ~= exp2(f) */

        j = _mm512_slli_epi32(i, 23);          /* i << 23 */
        r = _mm512_castsi512_ps(_mm512_add_epi32(j, _mm512_castps_si512(p))); /* r = p * 2^i*/
        return r;
    }

    __m256 WCORE_VECCALL expf_schraudolph(__m256 x)
    {
		__m256 a = _mm256_set1_ps(12102203.0f); /* (1 << 23) / log(2) */
		__m256i b = _mm256_set1_epi32(127 * (1 << 23) - 298765);
		__m256i t = _mm256_add_epi32(_mm256_cvtps_epi32(_mm256_mul_ps(a, x)), b);
		return _mm256_castsi256_ps(t);
    }

    double WCORE_VECCALL reduce_add(__m256d v)
    {
        __m128d vlow = _mm256_castpd256_pd128(v);
        __m128d vhigh = _mm256_extractf128_pd(v, 1); 
        vlow = _mm_add_pd(vlow, vhigh);    
        __m128d high64 = _mm_unpackhi_pd(vlow, vlow);
        return  _mm_cvtsd_f64(_mm_add_sd(vlow, high64)); 
    }

    float WCORE_VECCALL reduce_add(__m256 v)
    {
        __m128 reduce = _mm_add_ps(_mm256_extractf128_ps(v, 0), _mm256_extractf128_ps(v, 1));
        reduce = _mm_hadd_ps(reduce, reduce);
        reduce = _mm_hadd_ps(reduce, reduce);
        return _mm_cvtss_f32(reduce);
    }

    float WCORE_VECCALL reduce_add(__m512 v)
    {
        __m128 reduce = _mm_add_ps(_mm512_extractf32x4_ps(v, 0), _mm512_extractf32x4_ps(v, 1));
        reduce = _mm_add_ps(reduce, _mm_add_ps(_mm512_extractf32x4_ps(v, 2), _mm512_extractf32x4_ps(v, 3)));
        reduce = _mm_hadd_ps(reduce, reduce);
        reduce = _mm_hadd_ps(reduce, reduce);
        return _mm_cvtss_f32(reduce);
    }

    int WCORE_VECCALL reduce_add_i32(__m256i v)
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

    int reduce_add_i32(const int* x, int n)
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

    int64_t reduce_add_i1(const void* x, int n)
    {
        const uint64_t* xx = (const uint64_t*)x;
        int nb = (n + 7) / 8;
        int n8 = round_down(nb, 8);
       
        int i = 0;
        int64_t sum = 0;
        for (; i < n8; i += 8)
            sum += _mm_popcnt_u64(xx[i]);

        for (; i < nb; i++)
            sum += _mm_popcnt_u32(*((const uint8_t*)x + i));

        return sum;
    }

    float WCORE_VECCALL reduce_min(__m256 v)
    {
        __m128 reduce = _mm_min_ps(_mm256_extractf128_ps(v, 0), _mm256_extractf128_ps(v, 1));
        reduce = _mm_min_ps(reduce, _mm_movehl_ps(reduce, reduce));
        reduce = _mm_min_ps(reduce, _mm_movehdup_ps(reduce));
        return _mm_cvtss_f32(reduce);
    }

    float WCORE_VECCALL reduce_max(__m256 v)
    {
        __m128 reduce = _mm_max_ps(_mm256_extractf128_ps(v, 0), _mm256_extractf128_ps(v, 1));
        reduce = _mm_max_ps(reduce, _mm_movehl_ps(reduce, reduce));
        reduce = _mm_max_ps(reduce, _mm_movehdup_ps(reduce));
        return _mm_cvtss_f32(reduce);
    }

    int is_corrupted(__m256 v) 
    {
        // https://stackoverflow.com/questions/30674291/how-to-check-inf-for-avx-intrinsic-m256
        __m256 self_sub_v8 = _mm256_sub_ps(v, v);
        return _mm256_movemask_epi8(_mm256_castps_si256(self_sub_v8));
    }

    void WCORE_VECCALL demux(__m256& a, __m256& b, __m256& c)
    {
        __m256 a0 = _mm256_blend_ps(a, b, 0b10010010);
        __m256 a1 = _mm256_blend_ps(a0, c, 0b00100100);
        __m256 ax = _mm256_permutevar8x32_ps(a1, _mm256_setr_epi32(0, 3, 6, 1, 4, 7, 2, 5));
        __m256 b0 = _mm256_blend_ps(a, b, 0b00100100);
        __m256 b1 = _mm256_blend_ps(b0, c, 0b01001001);
        __m256 bx = _mm256_permutevar8x32_ps(b1, _mm256_setr_epi32(1, 4, 7, 2, 5, 0, 3, 6));
        __m256 c0 = _mm256_blend_ps(a, b, 0b01001001);
        __m256 c1 = _mm256_blend_ps(c0, c, 0b10010010);
        __m256 cx = _mm256_permutevar8x32_ps(c1, _mm256_setr_epi32(2, 5, 0, 3, 6, 1, 4, 7));
        a = ax;
        b = bx;
        c = cx;
    }

    void WCORE_VECCALL demux(__m256i& a, __m256i& b, __m256i& c)
    {
        __m256i a0 = _mm256_blend_epi32(a, b, 0b10010010);
        __m256i a1 = _mm256_blend_epi32(a0, c, 0b00100100);
        __m256i ax = _mm256_permutevar8x32_epi32(a1, _mm256_setr_epi32(0, 3, 6, 1, 4, 7, 2, 5));
        __m256i b0 = _mm256_blend_epi32(a, b, 0b00100100);
        __m256i b1 = _mm256_blend_epi32(b0, c, 0b01001001);
        __m256i bx = _mm256_permutevar8x32_epi32(b1, _mm256_setr_epi32(1, 4, 7, 2, 5, 0, 3, 6));
        __m256i c0 = _mm256_blend_epi32(a, b, 0b01001001);
        __m256i c1 = _mm256_blend_epi32(c0, c, 0b10010010);
        __m256i cx = _mm256_permutevar8x32_epi32(c1, _mm256_setr_epi32(2, 5, 0, 3, 6, 1, 4, 7));
        a = ax;
        b = bx;
        c = cx;
    }

    void WCORE_VECCALL cross(__m256 ax, __m256 ay, __m256 az, __m256 bx, __m256 by, __m256 bz, __m256& cx, __m256& cy, __m256& cz) noexcept
    {
        cx = _mm256_fmsub_ps(ay, bz, _mm256_mul_ps(az, by));
        cy = _mm256_fmsub_ps(az, bx, _mm256_mul_ps(ax, bz));
        cz = _mm256_fmsub_ps(ax, by, _mm256_mul_ps(ay, bx));
    }

    __m256 WCORE_VECCALL dot(__m256 ax, __m256 ay, __m256 az, __m256 bx, __m256 by, __m256 bz) noexcept
    {
        // There sure are dependent fmas nested, but usually we do at least 2 of these dots back to back.
        // If inlined, the CPU will able to reorder this.
        return _mm256_fmadd_ps(ax, bx, 
            _mm256_fmadd_ps(ay, by, _mm256_mul_ps(az, bz)));
    }

    __m256 WCORE_VECCALL blend_in(__m256 x, __m256 y, __m256 mask) noexcept
    {
        return _mm256_blendv_ps(x, y, mask);
    }

    float WCORE_VECCALL extract(__m256 v, int index)
    {
        alignas(32) float vv[8];
        _mm256_store_ps(vv, v);
        return vv[index];
    }

    int WCORE_VECCALL extract(__m256i v, int index)
    {
        alignas(32) int vv[8];
        _mm256_store_si256((__m256i*)vv, v);
        return vv[index];
    }

    int WCORE_VECCALL find_min_index(__m256 v)
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

    __m256i WCORE_VECCALL clamp(__m256i x, __m256i x0, __m256i x1)
    {
        return _mm256_max_epi32(x0, _mm256_min_epi32(x1, x));
    }

    void dxa(const float* x, const float* v, int n, int m, float* y)
    {
        // diag(V) * X
        //   X is n x m (col. major)
        //   V is n
        const int n16 = round_down(n, 16);

        for(int i = 0; i < m; i++) {
            for(int j = 0; j < n16; j+=16) {
                int xyoffs = i * n + j;
                __m256 t0 = _mm256_mul_ps(_mm256_loadu_ps(x + xyoffs), _mm256_loadu_ps(v + j));
                __m256 t1 = _mm256_mul_ps(_mm256_loadu_ps(x + xyoffs + 8), _mm256_loadu_ps(v + j + 8));
                _mm256_storeu_ps(y + xyoffs, t0);
                _mm256_storeu_ps(y + xyoffs + 8, t1);
            }

            for(int j = n16; j < n; j++)
                y[i * n + j] = x[i * n + j] * v[j];
        }
    }

    void dxinva(const float* x, const float* v, int n, int m, float* y)
    {
        // diag(V)^-1 * X
        const int n16 = round_down(n, 16);

         for(int i = 0; i < m; i++) {
            for(int j = 0; j < n16; j+=16) {
                int xyoffs = i * n + j;
                __m256 t0 = _mm256_div_ps(_mm256_loadu_ps(x + xyoffs), _mm256_loadu_ps(v + j));
                __m256 t1 = _mm256_div_ps(_mm256_loadu_ps(x + xyoffs + 8), _mm256_loadu_ps(v + j + 8));
                _mm256_storeu_ps(y + xyoffs, t0);
                _mm256_storeu_ps(y + xyoffs + 8, t1);
            }

            for (int j = n16; j < n; j++) {
                if (fabs(v[j]) > 1e-4f)
                    y[i * n + j] = x[i * n + j] / v[j];
                else
                    y[i * n + j] = 0;
            }
        }
    }

    void atdba(const float* a, int n, int m, const float* b, float alpha, float* y)
    {
        if (has_feature(WCORE_OPTPATH::AVX512))
            atdba_avx512(a, n, m, b, alpha, y);
        else
            atdba_avx2(a, n, m, b, alpha, y);
    }

    void atdba_avx512(const float* a, int n, int m, const float* b, float alpha, float* y)
    {
        // alpha * A' * diag(B) * A
        constexpr int BlockSize = 16;
        int m2 = round_down(m, 2);
        int n16 = round_down(n, BlockSize);
      
        for(int i = 0; i < m; i++) {
            int m2i = ((i & 0x1) == 0) ? m2 : (m2 - 1);
            for(int j = i; j < m2i; j+=2) {
                __m512 a0 = _mm512_setzero_ps();
                __m512 a1 = _mm512_setzero_ps();
                for(int k = 0; k < n16; k+= BlockSize) {
                    const __m512 aai = _mm512_mul_ps(_mm512_loadu_ps(a + k + i * n), _mm512_loadu_ps(b + k));
                    a0 = _mm512_fmadd_ps(aai, _mm512_loadu_ps(a + k + j * n), a0);
                    a1 = _mm512_fmadd_ps(aai, _mm512_loadu_ps(a + k + (j+1) * n), a1);
                }

                float aa0 = 0;
                float aa1 = 0;
                for(int k = n16; k < n; k++) {
                    const float aai = a[k + i * n] * b[k];
                    aa0 += aai * a[k + j * n];
                    aa1 += aai * a[k + (j + 1) * n];
                }

                aa0 = (reduce_add(a0) + aa0) * alpha;
                aa1 = (reduce_add(a1) + aa1) * alpha;
                y[i * m + j] = aa0;
                y[j * m + i] = aa0;
                y[i * m + j + 1] = aa1;
                y[(j + 1) * m + i] = aa1;
            }

            for(int j = m2i; j < m; j++) {
                __m512 a0 = _mm512_setzero_ps();
                for(int k = 0; k < n16; k+= BlockSize) {
                    const __m512 aai = _mm512_mul_ps(_mm512_loadu_ps(a + k + i * n), _mm512_loadu_ps(b + k));
                    a0 = _mm512_fmadd_ps(aai, _mm512_loadu_ps(a + k + j * n), a0);
                }

                float aa0 = 0;
                for(int k = n16; k < n; k++) {
                    const float aai = a[k + i * n] * b[k];
                    aa0 += aai * a[k + j * n];
                }

                aa0 = (reduce_add(a0) + aa0) * alpha;
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

                float aa0 = 0;
                float aa1 = 0;
                for (int k = n8; k < n; k++) {
                    const float aai = a[k + i * n] * b[k];
                    aa0 += aai * a[k + j * n];
                    aa1 += aai * a[k + (j + 1) * n];
                }

                aa0 = (reduce_add(a0) + aa0) * alpha;
                aa1 = (reduce_add(a1) + aa1) * alpha;
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

                float aa0 = 0;
                for (int k = n8; k < n; k++) {
                    const float aai = a[k + i * n] * b[k];
                    aa0 += aai * a[k + j * n];
                }

                aa0 = (reduce_add(a0) + aa0) * alpha;
                y[i * m + j] = aa0;
                y[j * m + i] = aa0;
            }
        }
    }

    float tratdba(const float* a, int n, int m, const float* b)
    {
        // trace(A' * diag(B) * A)

        float ret = 0;
      
        int n16 = round_down(n, 16);
        for(int i = 0; i < m; i++) {
            const float* ai = a + i * n;
            __m256 accum = _mm256_setzero_ps();
            
            for(int j = 0; j < n16; j+=16) {
                const __m256 aj0 = _mm256_loadu_ps(ai + j);
                const __m256 aj1 = _mm256_loadu_ps(ai + j + 8);

                const __m256 bj0 = _mm256_loadu_ps(b + j);                
                const __m256 bj1 = _mm256_loadu_ps(b + j + 8);

                const __m256 p0 = _mm256_mul_ps(_mm256_mul_ps(aj0, aj0), bj0);
                const __m256 p1 = _mm256_mul_ps(_mm256_mul_ps(aj1, aj1), bj1);

                accum = _mm256_add_ps(accum, _mm256_add_ps(p0, p1));
            }

            float part = 0;
            for(int j = n16; j < n; j++) {
                const float aj = ai[j];
                part += aj * aj * b[j]; 
            }

            ret += part + reduce_add(accum);
        }

        return ret;
    }

    void wsumc(const float** cols, const float* center, const float* weights, int n, int m, float* res)
    {
        constexpr int BlockSize = 8;
        int mb = round_down(m, BlockSize);
        memset(res, 0, sizeof(float) * m);

        for (int i = 0; i < n; i++) {
            const float* coli = cols[i];
            __m256 w = _mm256_broadcast_ss(weights + i);

            for (int j = 0; j < mb; j += BlockSize) {
                __m256 t = _mm256_sub_ps(_mm256_loadu_ps(coli + j), _mm256_loadu_ps(center + j));
                __m256 wt = _mm256_fmadd_ps(t, w, _mm256_loadu_ps(res + j));
                _mm256_storeu_ps(res + j, wt);
            }

            float ws = weights[i];
            for (int j = mb; j < m; j++)
                res[j] += (coli[j] - center[j]) * ws;
        }
    }

    float dot(const float* x, const float* y, int n)
    {
        const int BlockSize = 8;
        const int nb = round_down(n, BlockSize);

        __m256 sumb = _mm256_setzero_ps();
        for (int i = 0; i < nb; i += BlockSize) {
            sumb = _mm256_fmadd_ps(_mm256_loadu_ps(x + i), _mm256_loadu_ps(y + i), sumb);
        }

        float sum = 0;
        for (int i = nb; i < n; i++) {
            sum += x[i] * y[i];
        }

        return reduce_add(sumb) + sum;
    }

    void scale(float* x, float f, int n)
    {
        const int BlockSize = 8;
        const int nb = round_down(n, BlockSize);
        const __m256 fb = _mm256_set1_ps(f);

        for (int i = 0; i < nb; i += BlockSize) {
            __m256 xf = _mm256_mul_ps(_mm256_loadu_ps(x + i), fb);
            _mm256_storeu_ps(x + i, xf);
        }

        for (int i = nb; i < n; i++)
            x[i] *= f;
    }

    void add(float* x, float f, int n)
    {
        const int BlockSize = 8;
        const int nb = round_down(n, BlockSize);
        const __m256 fb = _mm256_set1_ps(f);

        for (int i = 0; i < nb; i += BlockSize) {
            __m256 xf = _mm256_add_ps(_mm256_loadu_ps(x + i), fb);
            _mm256_storeu_ps(x + i, xf);
        }

        for (int i = nb; i < n; i++)
            x[i] += f;
    }

    void normalize_columns(float* mat, int rows, int cols)
    {
        for (int c = 0; c < cols; c++) {
            float f = 1.0f / sqrtf(dot(mat + c * rows, mat + c * rows, rows));
            scale(mat + c * rows, f, rows);
        }
    }

    void check_finite(const float* x, size_t len)
    {
        size_t num_bad = 0;
        for (size_t i = 0; i < len; i++) {
            if (!std::_Is_finite(x[i]))
                num_bad++;
        }

        if (num_bad) {
            throw std::exception{};
        }
    }
};
