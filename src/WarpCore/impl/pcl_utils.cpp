#include "pcl_utils.h"
#include "vec_math.h"
#include "utils.h"
#include <immintrin.h>
#include <cmath>

namespace warpcore::impl
{
    void pcl_center(const float* x, int d, int m, float* c)
    {
        for(int i = 0; i < d; i++)
            c[i] = reduce_add(x + i * m, m) / m;
    }

    float pcl_cs(const float* x, int d, int m, const float* offs)
    {
        float ssq = 0;

        int m8 = round_down(m, 8);
        for(int i = 0; i < d; i++) {
            const float* xi = x + i * m;
            const float oi = offs[i];
            const __m256 oi8 = _mm256_set1_ps(oi);

            __m256 accum = _mm256_setzero_ps();
            for(int j = 0; j < m8; j += 8) {
                const __m256 r = _mm256_sub_ps(_mm256_loadu_ps(xi + j), oi8);
                accum = _mm256_fmadd_ps(r, r, accum);
            }

            ssq += reduce_add(accum);
            
            float accum2 = 0;
            for(int j = m8; j < m; j++) {
                const float r = xi[j] - oi;
                accum2 += r * r;
            }

            ssq += accum2;
        }

        return sqrtf(ssq / m);
    }

    void pcl_transform(const float* x, int d, int m, bool add, float sc, const float* offs, float* y)
    {
        WCORE_ASSERT(d == 3);

        if(add) {
             for(int i = 0; i < m; i++) {
                const float xi = x[i + 0 * m] - offs[0];
                const float yi = x[i + 1 * m] - offs[1];
                const float zi = x[i + 2 * m] - offs[2];

                y[i + 0 * m] += sc * xi;
                y[i + 1 * m] += sc * yi;
                y[i + 2 * m] += sc * zi;
            }
        } else {
            for(int i = 0; i < m; i++) {
                const float xi = x[i + 0 * m] - offs[0];
                const float yi = x[i + 1 * m] - offs[1];
                const float zi = x[i + 2 * m] - offs[2];

                y[i + 0 * m] = sc * xi;
                y[i + 1 * m] = sc * yi;
                y[i + 2 * m] = sc * zi;
            }
        }
    }

    void pcl_transform(const float* x, int d, int m, bool add, float sc, const float* offs, const float* rot, float* y)
    {
        WCORE_ASSERT(d == 3);

        if(add) {
             for(int i = 0; i < m; i++) {
                const float xi = x[i + 0 * m] - offs[0];
                const float yi = x[i + 1 * m] - offs[1];
                const float zi = x[i + 2 * m] - offs[2];

                y[i + 0 * m] += sc * (xi * rot[0] + yi * rot[3] + zi * rot[6]);
                y[i + 1 * m] += sc * (xi * rot[1] + yi * rot[4] + zi * rot[7]);
                y[i + 2 * m] += sc * (xi * rot[2] + yi * rot[5] + zi * rot[8]);
            }
        } else {
            for(int i = 0; i < m; i++) {
                const float xi = x[i + 0 * m] - offs[0];
                const float yi = x[i + 1 * m] - offs[1];
                const float zi = x[i + 2 * m] - offs[2];

                y[i + 0 * m] = sc * (xi * rot[0] + yi * rot[3] + zi * rot[6]);
                y[i + 1 * m] = sc * (xi * rot[1] + yi * rot[4] + zi * rot[7]);
                y[i + 2 * m] = sc * (xi * rot[2] + yi * rot[5] + zi * rot[8]);
            }
        }
    }

    
    float pcl_rmse(const float* x, const float* y, int d, int m)
    {
        float rms = 0;
        int k = d * m;
        int k8 = round_down(k, 8);

        __m256 rms_accum = _mm256_setzero_ps();
        for(int i = 0; i < k8 ; i += 8) {
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
        for(int i = 0; i < d; i++) {
            reduce_minmax(x + i * m, m, x0 + i, x1 + i);
        }
    }
};