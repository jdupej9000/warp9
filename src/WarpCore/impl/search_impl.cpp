#include "search_impl.h"
#include "utils.h"
#include "vec_math.h"
#include <immintrin.h>

namespace warpcore::impl
{
    // Vert is assumed to be in an AoSoA order {x0,y0,z0,x1,y1,z1,x2,y2,z2}, where each element contains
    // one register worth of coordinates. A register is currently 8 floats. The last such block may be
    // partially filled, where each of the x0..z2 elements only has valid data in its lower k lanes. The
    // content of the other lanes does not matter. It is advisable though to fill that with valid data
    // (e.g. repeated lower lanes) so that the arithmetic does not generate expensive NaNs.
    void _raytri(p3f orig, p3f dir, const float* vert, int n, int stride, __m256& u, __m256& v, __m256& bestt, __m256i& besti) noexcept
    {
        constexpr int VectorSize = 8;

        // Moller-Trumbore intersection algorithm, vectorized
        constexpr float EPS = 1e-16f;
        const __m256i rng = _mm256_setr_epi32(0, 1, 2, 3, 4, 5, 6, 7);

        __m256 o = _mm256_set_m128(orig, orig);
        __m256 d = _mm256_set_m128(dir, dir);

        for(int i = 0; i < n; i += VectorSize) {
            __m256 mask = _mm256_castsi256_ps(_mm256_cmpgt_epi32(_mm256_set1_epi32(n - i), rng));

            const float* vert_base = vert + 9 * (i & (~(VectorSize - 1)));

            const __m256 ax = _mm256_loadu_ps(vert_base);
            const __m256 ay = _mm256_loadu_ps(vert_base + 1 * VectorSize);
            const __m256 az = _mm256_loadu_ps(vert_base + 2 * VectorSize);

            const __m256 e1x = _mm256_sub_ps(_mm256_loadu_ps(vert_base + 3 * VectorSize), ax);
            const __m256 e1y = _mm256_sub_ps(_mm256_loadu_ps(vert_base + 4 * VectorSize), ay);
            const __m256 e1z = _mm256_sub_ps(_mm256_loadu_ps(vert_base + 5 * VectorSize), az);

            const __m256 e2x = _mm256_sub_ps(_mm256_loadu_ps(vert_base + 6 * VectorSize), ax);
            const __m256 e2y = _mm256_sub_ps(_mm256_loadu_ps(vert_base + 7 * VectorSize), ay);
            const __m256 e2z = _mm256_sub_ps(_mm256_loadu_ps(vert_base + 8 * VectorSize), az);

            __m256 px, py, pz;
            cross(_mm256_permute_ps(d, 0b00000000), _mm256_permute_ps(d, 0b01010101), _mm256_permute_ps(d, 0b10101010),
                e2x, e2y, e2z, px, py, pz);

            __m256 det = dot(e1x, e1y, e1z, px, py, pz);

            mask = _mm256_and_ps(mask, _mm256_or_ps(
                _mm256_cmp_ps(det, _mm256_set1_ps(EPS), _CMP_GT_OQ),
                _mm256_cmp_ps(det, _mm256_set1_ps(-EPS), _CMP_LT_OQ)));

            if(_mm256_movemask_ps(mask) == 0)
                continue;

            //__m256 inv_det = _mm256_rcp_ps(det);
            __m256 inv_det = _mm256_rcp_ps(_mm256_blendv_ps(_mm256_set1_ps(1), det, mask)); // prevent NaNs
            __m256 sx = _mm256_sub_ps(_mm256_permute_ps(o, 0b00000000), ax);
            __m256 sy = _mm256_sub_ps(_mm256_permute_ps(o, 0b01010101), ay);
            __m256 sz = _mm256_sub_ps(_mm256_permute_ps(o, 0b10101010), az);

            u = _mm256_mul_ps(dot(sx, sy, sz, px, py, pz), inv_det);

            mask = _mm256_and_ps(mask, _mm256_cmp_ps(u, _mm256_setzero_ps(), _CMP_GE_OQ));
            mask = _mm256_and_ps(mask, _mm256_cmp_ps(u, _mm256_set1_ps(1), _CMP_LE_OQ));

            if(_mm256_movemask_ps(mask) == 0)
                continue;

            __m256 qx, qy, qz;
            cross(sx, sy, sz, e1x, e1y, e1z, qx, qy, qz);

            v = dot(_mm256_permute_ps(d, 0b00000000), _mm256_permute_ps(d, 0b01010101), _mm256_permute_ps(d, 0b10101010), qx, qy, qz);
            v = _mm256_mul_ps(v, inv_det);

            mask = _mm256_and_ps(mask, _mm256_cmp_ps(v, _mm256_setzero_ps(), _CMP_GE_OQ));
            mask = _mm256_and_ps(mask, _mm256_cmp_ps(_mm256_add_ps(u, v), _mm256_set1_ps(1), _CMP_LE_OQ));

            if(_mm256_movemask_ps(mask) == 0)
                continue;

            __m256 tt = dot(e2x, e2y, e2z, qx, qy, qz);
            tt = _mm256_mul_ps(tt, inv_det);

            mask = _mm256_and_ps(mask, _mm256_cmp_ps(tt, _mm256_setzero_ps(), _CMP_GT_OQ));
            mask = _mm256_and_ps(mask, _mm256_cmp_ps(tt, bestt, _CMP_LT_OQ));

            bestt = _mm256_blendv_ps(bestt, tt, mask);
            besti = _mm256_blendv_epi8(besti, 
                _mm256_add_epi32(rng, _mm256_set1_epi32(i)), 
                _mm256_castps_si256(mask));
        }
    }

    // For the layout of vert, see _raytri.
    int _pttri(p3f orig, const float* vert, int n, int stride, p3f& retBary, p3f& retPt, float& retDist)
    {
        constexpr int VectorSize = 8;

        if (n == 0) return -1;

        const __m256i rng = _mm256_setr_epi32(0, 1, 2, 3, 4, 5, 6, 7);
        __m256 dist_best = _mm256_set1_ps(1e20f);
        __m256 u_best = _mm256_setzero_ps(), v_best = _mm256_setzero_ps();
        __m256i i_best = _mm256_setzero_si256();
        __m256 o = _mm256_set_m128(orig, orig);

        // This loop is a vectorized form of what's in Embree: 
        // https://github.com/RenderKit/embree/blob/master/tutorials/common/math/closest_point.h
        for (int i = 0; i < n; i += VectorSize) {
            const float* vert_base = vert + (i / VectorSize) * 9 * VectorSize;

            __m256 mask = _mm256_castsi256_ps(_mm256_cmpgt_epi32(_mm256_set1_epi32(n - i), rng));

            __m256 u = _mm256_setzero_ps(), v = _mm256_setzero_ps();

            // load a
            const __m256 ax = _mm256_loadu_ps(vert_base);
            const __m256 ay = _mm256_loadu_ps(vert_base + 1 * VectorSize);
            const __m256 az = _mm256_loadu_ps(vert_base + 2 * VectorSize);

            // ab = b - a
            const __m256 abx = _mm256_sub_ps(_mm256_loadu_ps(vert_base + 3 * VectorSize), ax);
            const __m256 aby = _mm256_sub_ps(_mm256_loadu_ps(vert_base + 4 * VectorSize), ay);
            const __m256 abz = _mm256_sub_ps(_mm256_loadu_ps(vert_base + 5 * VectorSize), az);

            // ac = c - a
            const __m256 acx = _mm256_sub_ps(_mm256_loadu_ps(vert_base + 6 * VectorSize), ax);
            const __m256 acy = _mm256_sub_ps(_mm256_loadu_ps(vert_base + 7 * VectorSize), ay);
            const __m256 acz = _mm256_sub_ps(_mm256_loadu_ps(vert_base + 8 * VectorSize), az);

            // ap = p - a
            const __m256 apx = _mm256_sub_ps(_mm256_permute_ps(o, 0b00000000), ax);
            const __m256 apy = _mm256_sub_ps(_mm256_permute_ps(o, 0b01010101), ay);
            const __m256 apz = _mm256_sub_ps(_mm256_permute_ps(o, 0b10101010), az);

            // d1 = dot(ab, ap); d2 = dot(ac, ap);
            __m256 d1 = dot(abx, aby, abz, apx, apy, apz);
            __m256 d2 = dot(acx, acy, acz, apx, apy, apz);

            // if (d1 <= 0.f && d2 <= 0.f) return a;
            __m256 m1 = _mm256_and_ps(_mm256_cmp_ps(d1, _mm256_setzero_ps(), _CMP_LE_OQ),
                _mm256_cmp_ps(d2, _mm256_setzero_ps(), _CMP_LE_OQ));
            //u = _mm256_blendv_ps(u, _mm256_setzero_ps(), m1); // these lanes are already zero
            //v = _mm256_blendv_ps(v, _mm256_setzero_ps(), m1);

            // bp = p - b
            const __m256 bpx = _mm256_sub_ps(_mm256_permute_ps(o, 0b00000000), _mm256_loadu_ps(vert_base + 3 * VectorSize));
            const __m256 bpy = _mm256_sub_ps(_mm256_permute_ps(o, 0b01010101), _mm256_loadu_ps(vert_base + 4 * VectorSize));
            const __m256 bpz = _mm256_sub_ps(_mm256_permute_ps(o, 0b10101010), _mm256_loadu_ps(vert_base + 5 * VectorSize));
            
            // d3 = dot(ab, bp); d4 = dot(ac, bp);
            __m256 d3 = dot(abx, aby, abz, bpx, bpy, bpz);
            __m256 d4 = dot(acx, acy, acz, bpx, bpy, bpz);

            // if (d3 >= 0.f && d4 <= d3) return b;
            __m256 m2 = _mm256_and_ps(_mm256_cmp_ps(d3, _mm256_setzero_ps(), _CMP_GE_OQ),
                _mm256_cmp_ps(d4, d3, _CMP_LE_OQ));
            u = blend_in(u, _mm256_set1_ps(1.0f), m2);
            //v = _mm256_blendv_ps(v, _mm256_setzero_ps(), m2); // these lanes are already zero

            // cp = p - c
            const __m256 cpx = _mm256_sub_ps(_mm256_permute_ps(o, 0b00000000), _mm256_loadu_ps(vert_base + 6 * VectorSize));
            const __m256 cpy = _mm256_sub_ps(_mm256_permute_ps(o, 0b01010101), _mm256_loadu_ps(vert_base + 7 * VectorSize));
            const __m256 cpz = _mm256_sub_ps(_mm256_permute_ps(o, 0b10101010), _mm256_loadu_ps(vert_base + 8 * VectorSize));

            // d5 = dot(ab, cp); d6 = dot(ac, cp);
            __m256 d5 = dot(abx, aby, abz, cpx, cpy, cpz);
            __m256 d6 = dot(acx, acy, acz, cpx, cpy, cpz);

            // if (d6 >= 0.f && d5 <= d6) return c;
            __m256 m3 = _mm256_and_ps(_mm256_cmp_ps(d6, _mm256_setzero_ps(), _CMP_GE_OQ),
                _mm256_cmp_ps(d5, d6, _CMP_LE_OQ));
            v = blend_in(v, _mm256_set1_ps(1.0f), m3);
            //u = _mm256_blendv_ps(u, _mm256_setzero_ps(), m3); // these lanes are already zero

            // const float vc = d1 * d4 - d3 * d2;
            __m256 vc = _mm256_sub_ps(_mm256_mul_ps(d1, d4), _mm256_mul_ps(d3, d2));

            // if (vc <= 0.f && d1 >= 0.f && d3 <= 0.f) {...
            __m256 m4 = _mm256_and_ps(  _mm256_cmp_ps(vc, _mm256_setzero_ps(), _CMP_LE_OQ),
                        _mm256_and_ps(  _mm256_cmp_ps(d1, _mm256_setzero_ps(), _CMP_GE_OQ),
                                        _mm256_cmp_ps(d3, _mm256_setzero_ps(), _CMP_LE_OQ)));
            if (!_mm256_testz_ps(m4, mask)) {
                __m256 t = _mm256_div_ps(d1, _mm256_sub_ps(d1, d3));
                u = blend_in(u, t, m4);
                //v = _mm256_blendv_ps(v, _mm256_setzero_ps(), m4); // these lanes are already zero
            }

            // const float vb = d5 * d2 - d1 * d6;
            __m256 vb = _mm256_sub_ps(_mm256_mul_ps(d5, d2), _mm256_mul_ps(d1, d6));

            // if (vb <= 0.f && d2 >= 0.f && d6 <= 0.f) { ...
            __m256 m5 = _mm256_and_ps(  _mm256_cmp_ps(vb, _mm256_setzero_ps(), _CMP_LE_OQ),
                        _mm256_and_ps(  _mm256_cmp_ps(d2, _mm256_setzero_ps(), _CMP_GE_OQ),
                                        _mm256_cmp_ps(d6, _mm256_setzero_ps(), _CMP_LE_OQ)));
            if (!_mm256_testz_ps(m5, mask)) {
                __m256 t = _mm256_div_ps(d2, _mm256_sub_ps(d2, d6));
                v = blend_in(v, t, m5);
                //u = _mm256_blendv_ps(u, _mm256_setzero_ps(), m5); // these lanes are already zero
            }

            // const float va = d3 * d6 - d5 * d4;
            __m256 va = _mm256_sub_ps(_mm256_mul_ps(d3, d6), _mm256_mul_ps(d5, d4));
            
            //if (va <= 0.f && (d4 - d3) >= 0.f && (d5 - d6) >= 0.f) { ...
            __m256 m6 = _mm256_and_ps(  _mm256_cmp_ps(va, _mm256_setzero_ps(), _CMP_LE_OQ),
                        _mm256_and_ps(  _mm256_cmp_ps(d4, d3, _CMP_GE_OQ),
                                        _mm256_cmp_ps(d5, d6, _CMP_GE_OQ)));
            if (!_mm256_testz_ps(m6, mask)) {
                __m256 d4d3 = _mm256_sub_ps(d4, d3);
                __m256 t = _mm256_div_ps(d4d3, _mm256_add_ps(d4d3, _mm256_sub_ps(d5, d6)));
                u = blend_in(u, _mm256_sub_ps(_mm256_set1_ps(1.0f), t), m6);
                v = blend_in(v, t, m6);
            }

            // m7 = mask & ~(m1 | m2 | ... | m6)
            __m256 m123456 = _mm256_or_ps(_mm256_or_ps(m1, m2), _mm256_or_ps(m3, m4));
            m123456 = _mm256_or_ps(m123456, _mm256_or_ps(m5, m6));
            __m256 m7 = _mm256_andnot_ps(mask, m123456);

            // const float denom = 1.f / (va + vb + vc);
            // const float v = vb * denom;
            // const float w = vc * denom;
            __m256 denom = _mm256_rcp_ps(_mm256_add_ps(_mm256_add_ps(va, vb), vc));
            __m256 s = _mm256_mul_ps(vb, denom);
            __m256 t = _mm256_mul_ps(vc, denom);
            u = blend_in(u, s, m7);
            v = blend_in(v, t, m7);

            // convert from barycentric to cartesian
            __m256 rx = _mm256_fmadd_ps(u, abx, _mm256_fmadd_ps(v, acx, ax));
            __m256 ry = _mm256_fmadd_ps(u, aby, _mm256_fmadd_ps(v, acy, ay));
            __m256 rz = _mm256_fmadd_ps(u, abz, _mm256_fmadd_ps(v, acz, az));

            // calculate distance squared from p
            __m256 prx = _mm256_sub_ps(rx, _mm256_permute_ps(o, 0b00000000));
            __m256 pry = _mm256_sub_ps(ry, _mm256_permute_ps(o, 0b01010101));
            __m256 prz = _mm256_sub_ps(rz, _mm256_permute_ps(o, 0b10101010));
            __m256 dist2 = _mm256_fmadd_ps(prx, prx, 
                _mm256_add_ps(_mm256_mul_ps(pry, pry), _mm256_mul_ps(prz, prz)));

            // update uv, index on triangles that are closer in their lane
            __m256 mm = _mm256_and_ps(mask, _mm256_cmp_ps(dist2, dist_best, _CMP_LT_OQ));
            dist_best = _mm256_blendv_ps(dist_best, dist2, mm);
            u_best = _mm256_blendv_ps(u_best, u, mm);
            v_best = _mm256_blendv_ps(v_best, v, mm);
            i_best = _mm256_blendv_epi8(i_best, _mm256_add_epi32(rng, _mm256_set1_epi32(i)), _mm256_castps_si256(mm));
        }

        int i = find_min_index(dist_best);
        //p3f& retBary, p3f& retPt, float& retDist
        int j = extract(i_best, i);
        retDist = extract(dist_best, i);
        retBary = p3f_set(extract(u_best, i), extract(v_best, i), 0);
        
        p3f a, b, c;
        extract_aosoa_triangle<VectorSize>(vert, j, a, b, c);

        retPt = p3f_add(a, p3f_add(
                p3f_mul(p3f_broadcast<0>(retBary), p3f_sub(b, a)),
                p3f_mul(p3f_broadcast<1>(retBary), p3f_sub(c, a))));
        // p3f_from_bary(a, b, c, retBary);

        return j;
    }
};