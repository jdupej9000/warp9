#pragma once

#include "../p3f.h"
#include "../config.h"
#include "utils.h"
#include <immintrin.h>

namespace warpcore::impl
{
    struct RayTri_T
    {
        static inline int WCORE_VECCALL store(__m256 bestt, __m256i besti, __m256, __m256, float* result) noexcept
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
        static inline int WCORE_VECCALL store(__m256 bestt, __m256i besti, __m256 u, __m256 v, float* result) noexcept
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
   

    void _raytri(p3f orig, p3f dir, const float* vert, int n, int stride, __m256& u, __m256& v, __m256& bestt, __m256i& besti) noexcept;

    template<typename TTraits>
    int raytri(p3f orig, p3f dir, const float* vert, int n, int stride, float* result) noexcept
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
        static inline void WCORE_VECCALL store(p3f pt, p3f bary, float d, float* result) noexcept
        {
            __m128 low = _mm_sqrt_ss(_mm_set_ss(d));
            low = _mm_blend_ps(low, _mm_permute_ps(pt, 0b10010000), 0b1110);
            _mm_storeu_ps(result, low);
            _mm_storeu_ps(result + 4, bary);
        }
    };
  

    int _pttri(p3f orig, const float* vert, int n, int stride, p3f& retBary, p3f& retPt, float& retDist);

    template<typename TTraits>
    int pttri(p3f orig, const float* vert, int n, int stride, float* result, float* pdist) noexcept
    {
        float dist = FLT_MAX;
        p3f bary = p3f_zero();
        p3f pt = p3f_zero();

        int ret = _pttri(orig, vert, n, stride, bary, pt, dist);
        TTraits::store(pt, bary, dist, result);
        *pdist = dist;
        return ret;
    }

    template<int NRegSize>
    void extract_aosoa_triangle(const float* vert, int idx, p3f& a, p3f& b, p3f& c)
    {
        static_assert((NRegSize & (NRegSize - 1)) == 0);

        int jbase = (idx & ~(NRegSize - 1)) * 9;
        int joffs = idx & (NRegSize - 1);

        const __m128i tidx = _mm_setr_epi32(jbase + joffs, jbase + joffs + 8,
            jbase + joffs + 16, jbase + joffs);

        a = _mm_i32gather_ps(vert, tidx, 4);
        b = _mm_i32gather_ps(vert + 3 * NRegSize, tidx, 4);
        c = _mm_i32gather_ps(vert + 6 * NRegSize, tidx, 4);
    }
};