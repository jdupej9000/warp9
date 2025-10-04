#include "pcl_utils.h"
#include "vec_math.h"
#include "utils.h"
#include <immintrin.h>
#include <cmath>

namespace warpcore::impl
{
    void pcl_center(const float* x, int d, int m, float* c)
    {
        WCORE_ASSERT(d <= 4);

        __m128 sum = _mm_setzero_ps();
        for (int i = 0; i < m; i++) {
            sum = _mm_add_ps(sum, _mm_loadu_ps(x + d * i));
        }

        sum = _mm_mul_ps(sum, _mm_set1_ps(1.0f / m));
        alignas(16) float sump[4];
        _mm_store_ps(sump, sum);

        for (int i = 0; i < d; i++)
            c[i] = sump[i];
    }

    float pcl_cs(const float* x, int d, int m, const float* offs)
    {
        WCORE_ASSERT(d <= 4);

        __m128 center = _mm_loadu_ps(offs);
        __m128 ssq = _mm_setzero_ps();

        for (int i = 0; i < m; i++) {
            __m128 xt = _mm_sub_ps(_mm_loadu_ps(x + d * i), center);
            ssq = _mm_fmadd_ps(xt, xt, ssq);
        }

        alignas(16) float ssqp[4];
        _mm_store_ps(ssqp, ssq);

        float ret = 0;
        for (int i = 0; i < d; i++)
            ret += ssqp[i];

        return sqrtf(ret / m);
    }

    void pcl_transform(const float* x, int d, int m, bool add, float sc, const float* offs, float* y)
    {
        WCORE_ASSERT(d == 3);

        __m128 center = _mm_loadu_ps(offs);
        __m128 scale = _mm_set1_ps(sc);

        // TODO: unwrap by 4, loading 3 xmms in one pass
        for (int i = 0; i < m; i++) {
            float* yi = y + 3 * i;
            __m128 yy = add ? _mm_loadu_ps(yi) : _mm_setzero_ps();

            yy = _mm_fmadd_ps(_mm_sub_ps(_mm_loadu_ps(x + 3 * i), center), scale, yy);

            _mm_storel_pi((__m64*)yi, yy);
            ((int*)yi)[2] = _mm_extract_ps(yy, 2);
        }
    }

    void pcl_transform(const float* x, int d, int m, bool add, float sc, const float* offs, const float* rot, float* y)
    {
        WCORE_ASSERT(d == 3);

        __m128 center = _mm_loadu_ps(offs);
        __m128 scale = _mm_set1_ps(sc);

        __m128 rot0 = _mm_loadu_ps(rot);
        __m128 rot1 = _mm_loadu_ps(rot + 3);
        __m128 rot2 = _mm_loadu_ps(rot + 6);

        // TODO: unwrap by 4, loading 3 xmms in one pass
        for (int i = 0; i < m; i++) {
            float* yi = y + 3 * i;

            __m128 yy = add ? _mm_loadu_ps(yi) : _mm_setzero_ps();
            __m128 xt = _mm_sub_ps(_mm_loadu_ps(x + 3 * i), center);

            __m128 xtr0 = _mm_mul_ps(rot0, _mm_shuffle_ps(xt, xt, 0b00000000));
            __m128 xtr1 = _mm_mul_ps(rot1, _mm_shuffle_ps(xt, xt, 0b01010101));
            __m128 xtr2 = _mm_mul_ps(rot2, _mm_shuffle_ps(xt, xt, 0b10101010));
            __m128 xtr = _mm_add_ps(_mm_add_ps(xtr0, xtr1), xtr2);

            yy = _mm_fmadd_ps(scale, xtr, yy);

            _mm_storel_pi((__m64*)yi, yy);
            ((int*)yi)[2] = _mm_extract_ps(yy, 2);
        }
    }

    
    float pcl_rmse(const float* x, const float* y, int d, int m)
    {
        float rms = 0;
        int k = d * m;
        int k8 = round_down(k, 8);

        __m256 rms_accum = _mm256_setzero_ps();
        for(int i = 0; i < k8; i += 8) {
            const __m256 r = _mm256_sub_ps(_mm256_loadu_ps(x + i), _mm256_loadu_ps(y + i));
            rms_accum = _mm256_fmadd_ps(r, r, rms_accum);
        }

        for (size_t i = k8; i < k; i++) {
            float r = x[i] - y[i];
            rms += r * r;
        }
        
        rms += reduce_add(rms_accum);
        return sqrtf(rms / m);
    }

    void pcl_aabb(const float* x, int d, int m, float* x0, float* x1)
    {
        WCORE_ASSERT(d <= 4);

        if (m < 1)
            return;

        __m128 xmin = _mm_loadu_ps(x);
        __m128 xmax = xmin;

        for (int i = 0; i < m; i++) {
            __m128 xi = _mm_loadu_ps(x + 3 * i);
            xmin = _mm_min_ps(xi, xmin);
            xmax = _mm_max_ps(xi, xmax);
        }

        alignas(16) float xminp[4], xmaxp[4];
        _mm_store_ps(xminp, xmin);
        _mm_store_ps(xmaxp, xmax);

        for (int i = 0; i < d; i++) {
            x0[i] = xminp[i];
            x1[i] = xmaxp[i];
        }
    }
};