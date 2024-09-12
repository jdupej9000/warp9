#include "search_impl.h"
#include "utils.h"

namespace warpcore::impl
{
    void cross(__m256 ax, __m256 ay, __m256 az, __m256 bx, __m256 by, __m256 bz, __m256& cx, __m256& cy, __m256& cz) noexcept;
    void dot(__m256 ax, __m256 ay, __m256 az, __m256 bx, __m256 by, __m256 bz, __m256& c) noexcept;

    void cross(__m256 ax, __m256 ay, __m256 az, __m256 bx, __m256 by, __m256 bz, __m256& cx, __m256& cy, __m256& cz) noexcept
    {
        cx = _mm256_fmsub_ps(ay, bz, _mm256_mul_ps(az, by));
        cy = _mm256_fmsub_ps(az, bx, _mm256_mul_ps(ax, bz));
        cz = _mm256_fmsub_ps(ax, by, _mm256_mul_ps(ay, bx));
    }

    void dot(__m256 ax, __m256 ay, __m256 az, __m256 bx, __m256 by, __m256 bz, __m256& c) noexcept
    {
        c = _mm256_fmadd_ps(ax, bx, _mm256_fmadd_ps(ay, by, _mm256_mul_ps(az, bz)));
    }

    void _raytri(const float* orig, const float* dir, const float* vert, int n, int stride, __m256& u, __m256& v, __m256& bestt, __m256i& besti) noexcept
    {
        // Moller-Trumbore intersection algorithm, vectorized
        constexpr float EPS = 1e-8f;
        const __m256i rng = _mm256_setr_epi32(0, 1, 2, 3, 4, 5, 6, 7);

        for(int i = 0; i < n; i += 8) {
            __m256 mask = _mm256_castsi256_ps(_mm256_cmpgt_epi32(_mm256_set1_epi32(n - i), rng));

            const __m256 ax = _mm256_loadu_ps(vert + i);
            const __m256 ay = _mm256_loadu_ps(vert + i + stride);
            const __m256 az = _mm256_loadu_ps(vert + i + 2 * stride);

            const __m256 e1x = _mm256_sub_ps(_mm256_loadu_ps(vert + i + 3 * stride), ax);
            const __m256 e1y = _mm256_sub_ps(_mm256_loadu_ps(vert + i + 4 * stride), ay);
            const __m256 e1z = _mm256_sub_ps(_mm256_loadu_ps(vert + i + 5 * stride), az);

            const __m256 e2x = _mm256_sub_ps(_mm256_loadu_ps(vert + i + 6 * stride), ax);
            const __m256 e2y = _mm256_sub_ps(_mm256_loadu_ps(vert + i + 7 * stride), ay);
            const __m256 e2z = _mm256_sub_ps(_mm256_loadu_ps(vert + i + 8 * stride), az);

            __m256 px, py, pz;
            cross(_mm256_broadcast_ss(dir), _mm256_broadcast_ss(dir+1), _mm256_broadcast_ss(dir+2), 
                e2x, e2y, e2z, px, py, pz);

            __m256 det;
            dot(e1x, e1y, e1z, px, py, pz, det);

            mask = _mm256_and_ps(mask, _mm256_or_ps(
                _mm256_cmp_ps(det, _mm256_set1_ps(EPS), _CMP_GT_OQ),
                _mm256_cmp_ps(det, _mm256_set1_ps(-EPS), _CMP_LT_OQ)));

            if(_mm256_movemask_epi8(_mm256_castps_si256(mask)) == 0)
                continue;

            //__m256 inv_det = _mm256_rcp_ps(det);
            __m256 inv_det = _mm256_rcp_ps(_mm256_blendv_ps(_mm256_set1_ps(1), det, mask)); // prevent NaNs
            __m256 sx = _mm256_sub_ps(_mm256_broadcast_ss(orig), ax);
            __m256 sy = _mm256_sub_ps(_mm256_broadcast_ss(orig + 1), ay);
            __m256 sz = _mm256_sub_ps(_mm256_broadcast_ss(orig + 2), az);

            dot(sx, sy, sz, px, py, pz, u);
            u = _mm256_mul_ps(u, inv_det);

            mask = _mm256_and_ps(mask, _mm256_cmp_ps(u, _mm256_set1_ps(0), _CMP_GE_OQ));
            mask = _mm256_and_ps(mask, _mm256_cmp_ps(u, _mm256_set1_ps(1), _CMP_LE_OQ));

            if(_mm256_movemask_epi8(_mm256_castps_si256(mask)) == 0)
                continue;

            __m256 qx, qy, qz;
            cross(sx, sy, sz, e1x, e1y, e1z, qx, qy, qz);

            dot(_mm256_broadcast_ss(dir), _mm256_broadcast_ss(dir + 1), _mm256_broadcast_ss(dir + 2), qx, qy, qz, v);
            v = _mm256_mul_ps(v, inv_det);

            mask = _mm256_and_ps(mask, _mm256_cmp_ps(v, _mm256_set1_ps(0), _CMP_GE_OQ));
            mask = _mm256_and_ps(mask, _mm256_cmp_ps(_mm256_add_ps(u, v), _mm256_set1_ps(1), _CMP_LE_OQ));

            if(_mm256_movemask_epi8(_mm256_castps_si256(mask)) == 0)
                continue;

            __m256 tt;
            dot(e2x, e2y, e2z, qx, qy, qz, tt);
            tt = _mm256_mul_ps(tt, inv_det);

            mask = _mm256_and_ps(mask, _mm256_cmp_ps(tt, _mm256_set1_ps(0), _CMP_GT_OQ));
            mask = _mm256_and_ps(mask, _mm256_cmp_ps(tt, bestt, _CMP_LT_OQ));

            bestt = _mm256_blendv_ps(bestt, tt, mask);
            besti = _mm256_blendv_epi8(besti, 
                _mm256_add_epi32(rng, _mm256_set1_epi32(i)), 
                _mm256_castps_si256(mask));
        }
    }

    int pttri(const float* orig, const float* vert, int n, int stride, float* closest)
    {
        const __m128i idx = _mm_set_epi32(0, 2 * stride, stride, 0);
        const p3f pt = p3f_set(orig); 

        float mindist = 1e30f;
        int ret = 0;
        p3f ptclosest = p3f_zero();

        const float* v = vert;
        for(int i = 0; i < n; i++) {
            p3f a = _mm_i32gather_ps(v, idx, 4);
            p3f b = _mm_i32gather_ps(v + 3 * stride, idx, 4);
            p3f c = _mm_i32gather_ps(v + 6 * stride, idx, 4);
            v++;

            p3f q = p3f_proj_to_tri(a, b, c, pt);
            float dist = p3f_distsq(q, pt);
            if(dist < mindist) {
                mindist = dist;
                ret = i;
                ptclosest = q;
            }
        }

        if(closest)
            _mm_storeu_ps(closest, ptclosest);

        return ret;
    }
};