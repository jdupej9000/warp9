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

    struct PtTri_Blank
    {
        constexpr static size_t ResultSize = 1;
        static inline void store(p3f pt, p3f bary, float d, float* result) noexcept
        {
        }
    };

    struct PtTri_DPtBary
    {
        constexpr static size_t ResultSize = 8;
        static inline void store(p3f pt, p3f bary, float d, float* result) noexcept
        {
            result[0] = sqrtf(d);
            _mm_storeu_ps(result + 1, pt);
            _mm_storeu_ps(result + 4, bary);
        }
    };
  
    int _pttri(const float* orig, const float* vert, int n, int stride, p3f& retBary, p3f& retPt, float& retDist);

    template<typename TTraits>
    int pttri(const float* orig, const float* vert, int n, int stride, float* result, float* pdist) noexcept
    {
        float dist = FLT_MAX;
        p3f bary = p3f_zero();
        p3f pt = p3f_zero();

        int ret = _pttri(orig, vert, n, stride, bary, pt, dist);
        TTraits::store(pt, bary, dist, result);
        *pdist = dist;
    }
};