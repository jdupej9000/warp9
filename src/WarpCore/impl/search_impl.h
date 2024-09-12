#pragma once

#include "../p3f.h"
#include "utils.h"
#include <immintrin.h>

namespace warpcore::impl
{
    struct RayTri_T
    {
        static inline int store(__m256 bestt, __m256i besti, __m256, __m256, float* result) noexcept
        {
             int ret = -1;
            float t = FLT_MAX;
            reduce_idxmin(bestt, besti, t, ret);
            *result = t;

            return ret;
        }
    };

    struct RayTri_TBary
    {
        static inline int store(__m256 bestt, __m256i besti, __m256 u, __m256 v, float* result) noexcept
        {
            int ret = -1;
            float t = FLT_MAX;
            reduce_idxmin(bestt, besti, t, ret);

            alignas(32) float uu[8], vv[8];
            _mm256_store_ps(uu, u);
            _mm256_store_ps(vv, v);
            result[0] = t;
            result[1] = uu[ret];
            result[2] = vv[ret];
            result[3] = 1.0f - uu[ret] - vv[ret];

            return ret;
        }
    };
   

    void _raytri(const float* orig, const float* dir, const float* vert, int n, int stride, __m256& u, __m256& v, __m256& bestt, __m256i& besti) noexcept;

    template<typename TTraits>
    int raytri(const float* orig, const float* dir, const float* vert, int n, int stride, float* result) noexcept
    {
        __m256 bestt = _mm256_set1_ps(1e30f);
        __m256i besti = _mm256_set1_epi32(-1);
        __m256 u, v;
        _raytri(orig, dir, vert, n, stride, u, v, bestt, besti);
        return TTraits::store(bestt, besti, u, v, result);
    }

    // Casts a ray from orig in the direction dir into the triangle soup in vert (assuming
    // there are n triangles). The triangles are represented as struct-of-arrays, with all x0,
    // then y0, z0, x1, y1, z1, x2, y2, z2. Each channel starts at an integer multiple of stride.
    // If there is no spacing between channels, n==stride. If a collision is found, the index of
    // closest colliding triangle is returned and t is set to the distance of collision (in units
    // of dir). Otherwise, -1 is returned and t is left unmodified.
    //int raytri(const float* orig, const float* dir, const float* vert, int n, int stride, float* t);

    p3f pttri(p3f a, p3f b, p3f c, p3f pt);
    int pttri(const float* orig, const float* vert, int n, int stride, float* closest);
};