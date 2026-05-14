#pragma once

#include "../p3f.h"
#include "../config.h"
#include "utils.h"
#include <immintrin.h>

namespace warpcore::impl
{
    // Traits for raytri that store the distance to hit for each query.
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

    // Traits for raytri that store {distance, u, v, w} for each query.
    struct RayTri_TBary
    {
        static inline int WCORE_VECCALL store(__m256 bestt, __m256i besti, __m256 u, __m256 v, float* result) noexcept
        {
            int ret = -1;
            float t = FLT_MAX;
            int bestLane = reduce_idxmin(bestt, besti, t, ret);

            alignas(32) float uu[8], vv[8];
            _mm256_store_ps(uu, u);
            _mm256_store_ps(vv, v);
            result[0] = t;
            result[1] = uu[bestLane];
            result[2] = vv[bestLane];
            result[3] = 1.0f - uu[bestLane] - vv[bestLane];

            return ret;
        }
    };
   

    void _raytri(p3f orig, p3f dir, const float* vert, int n, __m256& u, __m256& v, __m256& bestt, __m256i& besti) noexcept;

    // Cast a ray from orig along dir and intersect triangles in the AoSoA-ordered array vert (containing n triangles).
    // For the closest hit (if any), return the index of hit triangle and write intersection data to result according
    // to TTraits. If there is no hit, -1 is reutrned and result is left unmodified.
    template<typename TTraits>
    int raytri(p3f orig, p3f dir, const float* vert, int n, float* result) noexcept
    {
        __m256 bestt = _mm256_set1_ps(1e30f);
        __m256i besti = _mm256_set1_epi32(-1);
        __m256 u, v;
        _raytri(orig, dir, vert, n, u, v, bestt, besti);
        return TTraits::store(bestt, besti, u, v, result);
    }

    // Traits for pttri that stores nothing for queries.
    struct PtTri_Blank
    {
        constexpr static size_t ResultSize = 1;
        static inline void store(p3f pt, p3f bary, float d, float* result) noexcept
        {
        }
    };

    // Traits for pttri that stores for each query: {distance, hit.x, hit.y, hit.z, hit.u, hit.v, undefined, undefined}
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
  

    int _pttri(p3f orig, const float* vert, int n, p3f& retBary, p3f& retPt, float& retDist);

    // Find the closest triangle to orig in the AoSoA-ordered array vert that contains n triangles. Store
    // the result according to TTraits into result and return the index of the hit. pdist is read to clamp
    // the maximum search distance and written to indicate the new hit distance. pdist is squared distance.
    // If there is no hit closer than pdist, returns -1.
    template<typename TTraits>
    int pttri(p3f orig, const float* vert, int n, float* result, float* pdist) noexcept
    {
        float dist = FLT_MAX;
        p3f bary = p3f_zero();
        p3f pt = p3f_zero();

        int ret = _pttri(orig, vert, n, bary, pt, dist);
        TTraits::store(pt, bary, dist, result);
        *pdist = dist;
        return ret;
    }

    // Assuming vert is an array that stores unshared triangles in AoSoA ordering {ax0..ax7,ay0..ay7,az0..az7..cz7}+,
    // extract the vertex coordinates for idx-th triangle and store into a,b,c.
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