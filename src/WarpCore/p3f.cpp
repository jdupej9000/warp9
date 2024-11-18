#include "p3f.h"
#include <cmath>
#include <algorithm>

namespace warpcore
{
    p3f p3f_zero(void) noexcept
    {
        return _mm_setzero_ps();
    }

    p3f p3f_set(float x) noexcept
    {
        return _mm_setr_ps(x, x, x, 0);
    }

    p3f p3f_set(float x, float y, float z) noexcept
    {
        return _mm_setr_ps(x, y, z, 0);
    }

    p3f p3f_set(const float* x)
    {
        return _mm_loadu_ps(x);
    }

    p3i p3i_set(int x) noexcept
    {
        return _mm_setr_epi32(x, x, x, 0);
    }

    p3i p3i_set(int x, int y, int z) noexcept
    {
        return _mm_setr_epi32(x, y, z, 0);
    }

    p3i p3i_set(const int* x) noexcept
    {
        return _mm_loadu_si128((const __m128i*)x);
    }

    p3i p3f_to_p3i(const p3f a) noexcept
    {
        return _mm_cvtps_epi32(_mm_round_ps(a, (_MM_FROUND_TO_NEG_INF |_MM_FROUND_NO_EXC)));
        //return _mm_cvtps_epi32(_mm_round_ps(a, (_MM_FROUND_TO_NEAREST_INT | _MM_FROUND_NO_EXC)));
    }

    p3f p3i_to_p3f(const p3i a) noexcept
    {
        return _mm_cvtepi32_ps(a);
    }

    void p3f_to_int(const p3f a, int& x, int& y, int& z) noexcept
    {
        alignas(16) int xi[4];
        _mm_store_si128((__m128i*)xi, _mm_cvtps_epi32(_mm_round_ps(a, (_MM_FROUND_TO_NEG_INF |_MM_FROUND_NO_EXC))));
        x = xi[0];
        y = xi[1];
        z = xi[2];
    }

    int p3i_get(p3i x, int i) noexcept
    {
        alignas(16) int xi[4];
        _mm_store_si128((__m128i*)xi, x);

        return xi[i];
    }

    bool p3i_equal(p3i x, p3i y) noexcept
    {
        // TODO: optimize
        return _mm_test_all_ones(
            _mm_blend_epi16(
                _mm_cmpeq_epi32(x, y), 
                _mm_set1_epi8(0xff), 
                0b11000000));
    }

    // Returns an integer mask with bits set at positions, where the coordinates of x
    // are equal to the minimal coordinate. Only the lowest 3 coordinates are considered.
    int p3f_min_mask(p3f x) noexcept
    {
        p3f xmin = _mm_min_ss(x, _mm_min_ss(_mm_shuffle_ps(x, x, 0b01), _mm_shuffle_ps(x, x, 0b10)));
        xmin = _mm_shuffle_ps(xmin, xmin, 0);
        __m128i maskf = _mm_cmpeq_epi32(_mm_castps_si128(x), _mm_castps_si128(xmin));
        int mask = _mm_movemask_epi8(maskf);
        return (int)_pext_u32(mask, 0x111);
    }

    p3f p3f_min_mask_full(p3f x) noexcept
    {
        p3f xmin = _mm_min_ss(x, _mm_min_ss(_mm_shuffle_ps(x, x, 0b01), _mm_shuffle_ps(x, x, 0b10)));
        xmin = _mm_shuffle_ps(xmin, xmin, 0);
        return _mm_cmpeq_ps(x, xmin);
    }

    float p3f_max(p3f a) noexcept
    {
        return _mm_cvtss_f32(_mm_max_ss(a, _mm_max_ss(_mm_shuffle_ps(a, a, 0b01), _mm_shuffle_ps(a, a, 0b10))));
    }

    float p3f_min_removenan(p3f a) noexcept
    {
        // TODO:
        return 0;
    }

    p3f p3f_abs(p3f x) noexcept
    {
        // clear the sign bit
        return _mm_andnot_ps(_mm_set1_ps(-0.0f), x);
    }

    p3f p3f_sign(p3f x) noexcept
    {
        // 1.0f with copied sign from x
        return _mm_or_ps(_mm_and_ps(_mm_set1_ps(-0.0f), x), _mm_set1_ps(1.0f));
    }

    p3i p3f_isign(p3f x) noexcept
    {
        // 0xffffffff if x negative, 0x0 if positive
        p3i s = _mm_srai_epi32(_mm_castps_si128(x), 31);
        return _mm_or_si128(s, _mm_set1_epi32(1));
    }

    p3f p3f_floor(p3f x) noexcept
    {
        return _mm_round_ps(x, _MM_FROUND_TO_NEG_INF | _MM_FROUND_NO_EXC);
    }

    float p3f_dot(p3f a, p3f b) noexcept
    {
        return _mm_cvtss_f32(_mm_dp_ps(a, b, 0x71));
    }

    float p3f_lensq(p3f a) noexcept
    {
        return p3f_dot(a, a);
    }

    float p3f_len(p3f a) noexcept
    {
        return sqrtf(p3f_dot(a, a));
    }

    float p3f_dist(p3f a, p3f b) noexcept
    {
        return p3f_len(p3f_sub(a, b));
    }

    float p3f_distsq(p3f a, p3f b) noexcept
    {
        return p3f_lensq(p3f_sub(a, b));
    }

    p3f p3f_cross(p3f a, p3f b) noexcept
    {
        return _mm_fmsub_ps(_mm_shuffle_ps(a, a, 0b001001), _mm_shuffle_ps(b, b, 0b010010),
            _mm_mul_ps(_mm_shuffle_ps(a, a, 0b010010), _mm_shuffle_ps(b, b, 0b001001)));
    }

    p3f p3f_normalize(p3f a) noexcept
    {
        return _mm_mul_ps(a, _mm_rsqrt_ps(_mm_dp_ps(a, a, 0x7F)));
    }

    p3f p3f_sub(p3f a, p3f b) noexcept
    {
        return _mm_sub_ps(a, b);
    }

    p3f p3f_add(float a, p3f b) noexcept
    {
        return _mm_add_ps(_mm_set1_ps(a), b);
    }

    p3f p3f_add(p3f a, p3f b) noexcept
    {
        return _mm_add_ps(a, b);
    }

    p3i p3i_add(p3i a, p3i b) noexcept
    {
        return _mm_add_epi32(a, b);
    }

    p3f p3f_mul(p3f a, p3f b) noexcept
    {
        return _mm_mul_ps(a, b);
    }

     p3f p3f_mul(float a, p3f b) noexcept
    {
        return _mm_mul_ps(b, _mm_set1_ps(a));
    }

    p3f p3f_div(p3f a, p3f b) noexcept
    {
        return _mm_div_ps(a, b);
    }

    p3f p3f_recip(p3f a) noexcept
    {
        return _mm_rcp_ps(a);
    }

    p3f p3f_fma(p3f a, p3f b, p3f c) noexcept
    {
        return _mm_fmadd_ps(a, b, c);
    }

    p3f p3f_fma(float a, p3f b, p3f c) noexcept
    {
        return _mm_fmadd_ps(_mm_set1_ps(a), b, c);
    }

    p3f p3f_mid(p3f a, p3f b) noexcept
    {
        return _mm_mul_ps(_mm_add_ps(a, b), _mm_set1_ps(0.5f));
    }

    p3f p3f_mid(p3f a, p3f b, p3f c) noexcept
    {
        return _mm_mul_ps(_mm_add_ps(_mm_add_ps(a, b), c), _mm_set1_ps(1.0f / 3.0f));
    }

    p3f p3f_lerp(p3f a, p3f b, float t) noexcept
    {
        return _mm_fmadd_ps(_mm_set1_ps(t), _mm_sub_ps(b, a), a);
    }

    p3f p3f_lerp(p3f a, p3f b, p3f t) noexcept
    {
        return _mm_fmadd_ps(t, _mm_sub_ps(b, a), a);
    }

    bool p3f_in_aabb(p3f x, p3f box0, p3f box1) noexcept
    {
        __m128 mask = _mm_and_ps(_mm_cmp_ps(x, box0, _CMP_GE_OQ),
            _mm_cmp_ps(x, box1, _CMP_LE_OQ));
        return (_mm_movemask_ps(mask) & 0x7) == 0x7;
    }

    p3f p3f_mask_is_almost_zero(p3f x) noexcept
    {
        return _mm_cmp_ps(p3f_abs(x), _mm_set1_ps(1e-10f), _CMP_LT_OQ);
    }

    p3f p3f_switch(p3f zero, p3f one, p3f mask) noexcept
    {
        return _mm_blendv_ps(zero, one, mask);
    }

    p3i p3i_in_range(p3i x, p3i box0, p3i box1) noexcept
    {
        p3i ret = _mm_or_si128(_mm_cmpeq_epi32(x, box0), _mm_cmpgt_epi32(x, box0));
        ret = _mm_and_si128(ret, _mm_cmplt_epi32(x, box1));
        return ret;
    }

    bool p3i_in_aabb(p3i x, p3i box0, p3i box1) noexcept
    {
        p3i ret = _mm_or_si128(_mm_cmpeq_epi32(x, box0), _mm_cmpgt_epi32(x, box0));
        ret = _mm_and_si128(ret, _mm_cmplt_epi32(x, box1));
        return (_mm_movemask_epi8(ret) & 0xfff) == 0xfff;
    }

    bool p3i_is_zero(p3i x) noexcept
    {
        return _mm_testz_si128(p3i_set(0xffffffff, 0xffffffff, 0xffffffff), x);
    }

    p3i p3i_clamp(p3i x, p3i box0, p3i box1) noexcept
    {
        return _mm_min_epi32(_mm_max_epi32(x, box0), box1);
    }

    p3f p3f_clamp_zero(p3f x) noexcept
    {
        return _mm_max_ps(x, _mm_setzero_ps());
    }

    p3f p3f_clamp(p3f x, float x0, float x1) noexcept
    {
        return _mm_max_ps(_mm_min_ps(x, _mm_set1_ps(x1)), _mm_set1_ps(x0));
    }

    p3f p3f_xy(p3f x) noexcept
    {
        return _mm_movelh_ps(x, _mm_setzero_ps());
    }

    float p3f_addxy(p3f x) noexcept
    {
        return _mm_cvtss_f32(_mm_add_ps(x, _mm_movehdup_ps(x)));
    }


    p3f p3f_to_bary(p3f a, p3f b, p3f c, p3f p) noexcept
    {
        const p3f v0 = p3f_sub(b, a);
        const p3f v1 = p3f_sub(c, a);
        const p3f v2 = p3f_sub(p, a);

        const float d00 = p3f_dot(v0, v0);
        const float d01 = p3f_dot(v0, v1);
        const float d11 = p3f_dot(v1, v1);
        const float d20 = p3f_dot(v2, v0);
        const float d21 = p3f_dot(v2, v1);

        const float rdenom = 1.0f / (d00 * d11 - d01 * d01);
        const float v = (d11 * d20 - d01 * d21) * rdenom;
        const float w = (d00 * d21 - d01 * d20) * rdenom;
        const float u = 1.0f - v - w;

        return p3f_set(u, v, w);
    }

    p3f p3f_from_bary(p3f a, p3f b, p3f c, p3f p) noexcept
    {
        return p3f_fma(a, p3f_broadcast<0>(p),
                p3f_fma(b, p3f_broadcast<1>(p), 
                p3f_mul(c, p3f_broadcast<2>(p))));
    }

    p3f p3f_proj_to_plane(planef plane, p3f pt) noexcept
    {
        return _mm_sub_ps(pt, _mm_mul_ps(plane, _mm_dp_ps(plane, pt, 0xf7)));
    }

    planef planef_from_center_normal(p3f c, p3f n) noexcept
    {
        planef d = _mm_xor_ps(_mm_dp_ps(n, c, 0x87), _mm_set1_ps(-0.0f)); //-dot(c,n)
        return _mm_blend_ps(d, n, 7);
    }

    planef planef_from_center_normaldir(p3f c, p3f nd) noexcept
    {
        return planef_from_center_normal(c, p3f_normalize(nd));
    }

    float intersect_ray_aabb(p3f o, p3f d, p3f box0, p3f box1) noexcept
    {
        float t0, t1;
        if (!intersect_ray_aabb(o, d, box0, box1, t0, t1))
            return -1;

        return t0;
    }

    bool intersect_ray_aabb(p3f o, p3f d, p3f box0, p3f box1, float& t0, float& t1) noexcept
    {
        const __m128 cutoff = p3f_set(1e-8f);
        __m128 valid_mask = _mm_cmp_ps(p3f_abs(d), cutoff, _CMP_GT_OQ);
        int valid = _mm_movemask_ps(valid_mask);
        p3f dd = _mm_blendv_ps(cutoff, d, valid_mask); // avoid divisions by zero

        // TODO: profile this on d=0x`
        alignas(16) float k0[4];
        _mm_store_ps(k0, _mm_div_ps(_mm_sub_ps(box0, o), dd));

        alignas(16) float k1[4];
        _mm_store_ps(k1, _mm_div_ps(_mm_sub_ps(box1, o), dd));

        float tmin = 0.0f, tmax = 1e30f;

        for (int d = 0; d < 3; d++) {
            if (valid & (1 << d)) {
                tmin = std::max(tmin, std::min(k0[d], k1[d]));
                tmax = std::min(tmax, std::max(k0[d], k1[d]));
            }
        }

        t0 = tmin;
        t1 = tmax;
        return (tmax > 0 && tmin < tmax);
    }

    bool intersect_tri_aabb(p3f box0, p3f box1, p3f t0, p3f t1, p3f t2) noexcept
    {
        // https://omnigoat.github.io/2015/03/09/box-triangle-intersection/

        // Test tri AABB vs box
        p3f tbox0, tbox1;
        aabb_from_tri(t0, t1, t2, tbox0, tbox1);
        if(intersect_aabb_aabb(box0, box1, tbox0, tbox1))
            return false;

        // Test tri plane vs box
        p3f n = normal_from_tri(t0, t1, t2);
        p3f boxw = p3f_sub(box1, box0);
        p3f c = _mm_max_ps(boxw, _mm_setzero_ps());

        float d1 = p3f_dot(n, p3f_sub(c, t0));
        float d2 = p3f_dot(n, p3f_sub(boxw, p3f_add(c, t0)));
        float dotnp = p3f_dot(n, box0);

        if((dotnp + d1) * (dotnp + d2) > 0)
            return false;

        // gather all x, y, z in their repsective registers
        //p3f tx = t0, ty = t1, tz = t2;
        //tri_transpose_inplace(tx, ty, tz);
        p3f e0 = p3f_sub(t1, t0);
        p3f e1 = p3f_sub(t2, t1);
        p3f e2 = p3f_sub(t0, t2);

        // xy-plane overlap
        // TODO: this can be turned into purely vertical operations
        p3f xym = (p3f_get<2>(n) < 0) ? p3f_set(-1.0f) : p3f_set(1.0f);
        p3f ne0xy = p3f_mul(xym, p3f_swizzle<-1, 0, 3>(e0));
        p3f ne1xy = p3f_mul(xym, p3f_swizzle<-1, 0, 3>(e1));
        p3f ne2xy = p3f_mul(xym, p3f_swizzle<-1, 0, 3>(e2));
        p3f v0xy = p3f_xy(t0);
        p3f v1xy = p3f_xy(t1);
        p3f v2xy = p3f_xy(t2);
        float de0xy = -p3f_dot(ne0xy, v0xy) + p3f_addxy(p3f_clamp_zero(p3f_mul(boxw, ne0xy)));
        float de1xy = -p3f_dot(ne1xy, v1xy) + p3f_addxy(p3f_clamp_zero(p3f_mul(boxw, ne1xy)));
        float de2xy = -p3f_dot(ne2xy, v2xy) + p3f_addxy(p3f_clamp_zero(p3f_mul(boxw, ne2xy)));
        p3f pxy = p3f_xy(box0);
    
        if(p3f_dot(ne0xy, pxy) + de0xy < 0 || p3f_dot(ne1xy, pxy) + de1xy < 0 || p3f_dot(ne2xy, pxy) + de2xy < 0)
            return false;

        // TODO: yz, zx

        return true;

        /*
       
	// yz-plane projection overlap
	auto yzm = (n.x < 0.f ? -1.f : 1.f);
	auto ne0yz = vector4f{-tri.edge0().z, tri.edge0().y, 0.f, 0.f} * yzm;
	auto ne1yz = vector4f{-tri.edge1().z, tri.edge1().y, 0.f, 0.f} * yzm;
	auto ne2yz = vector4f{-tri.edge2().z, tri.edge2().y, 0.f, 0.f} * yzm;

	auto v0yz = math::vector4f{tri.v0.y, tri.v0.z, 0.f, 0.f};
	auto v1yz = math::vector4f{tri.v1.y, tri.v1.z, 0.f, 0.f};
	auto v2yz = math::vector4f{tri.v2.y, tri.v2.z, 0.f, 0.f};

	float de0yz = -dot_product(ne0yz, v0yz) + std::max(0.f, dp.y * ne0yz.x) + std::max(0.f, dp.z * ne0yz.y);
	float de1yz = -dot_product(ne1yz, v1yz) + std::max(0.f, dp.y * ne1yz.x) + std::max(0.f, dp.z * ne1yz.y);
	float de2yz = -dot_product(ne2yz, v2yz) + std::max(0.f, dp.y * ne2yz.x) + std::max(0.f, dp.z * ne2yz.y);

	auto pyz = vector4f(p.y, p.z, 0.f, 0.f);

	if ((dot_product(ne0yz, pyz) + de0yz) < 0.f || (dot_product(ne1yz, pyz) + de1yz) < 0.f || (dot_product(ne2yz, pyz) + de2yz) < 0.f)
		return false;


	// zx-plane projection overlap
	auto zxm = (n.y < 0.f ? -1.f : 1.f);
	auto ne0zx = vector4f{-tri.edge0().x, tri.edge0().z, 0.f, 0.f} * zxm;
	auto ne1zx = vector4f{-tri.edge1().x, tri.edge1().z, 0.f, 0.f} * zxm;
	auto ne2zx = vector4f{-tri.edge2().x, tri.edge2().z, 0.f, 0.f} * zxm;

	auto v0zx = math::vector4f{tri.v0.z, tri.v0.x, 0.f, 0.f};
	auto v1zx = math::vector4f{tri.v1.z, tri.v1.x, 0.f, 0.f};
	auto v2zx = math::vector4f{tri.v2.z, tri.v2.x, 0.f, 0.f};

	float de0zx = -dot_product(ne0zx, v0zx) + std::max(0.f, dp.y * ne0zx.x) + std::max(0.f, dp.z * ne0zx.y);
	float de1zx = -dot_product(ne1zx, v1zx) + std::max(0.f, dp.y * ne1zx.x) + std::max(0.f, dp.z * ne1zx.y);
	float de2zx = -dot_product(ne2zx, v2zx) + std::max(0.f, dp.y * ne2zx.x) + std::max(0.f, dp.z * ne2zx.y);

	auto pzx = vector4f(p.z, p.x, 0.f, 0.f);

	if ((dot_product(ne0zx, pzx) + de0zx) < 0.f || (dot_product(ne1zx, pzx) + de1zx) < 0.f || (dot_product(ne2zx, pzx) + de2zx) < 0.f)
		return false;


	return true;*/
    }

    bool intersect_aabb_aabb(p3f a0, p3f a1, p3f b0, p3f b1) noexcept
    {
        p3f acent = p3f_mid(a0, a1);
        p3f bcent = p3f_mid(b0, b1);
        p3f ahw = p3f_mul(0.5f, p3f_sub(a1, a0));
        p3f bhw = p3f_mul(0.5f, p3f_sub(b1, b0));

        int m = _mm_movemask_ps(_mm_cmp_ps(
            p3f_abs(p3f_sub(acent, bcent)), 
            p3f_add(ahw, bhw),
            _CMP_GT_OQ));

        return (m & 0x7) != 0;
    }

    void aabb_from_tri(p3f t0, p3f t1, p3f t2, p3f& box0, p3f& box1) noexcept
    {
        box0 = _mm_min_ps(_mm_min_ps(t0, t1), t2);
        box1 = _mm_max_ps(_mm_max_ps(t0, t1), t2);
    }

    p3f normal_from_tri(p3f a, p3f b, p3f c) noexcept
    {
        return p3f_cross(p3f_sub(b, a), p3f_sub(c, a));
    }

    void tri_transpose_inplace(p3f& a, p3f& b, p3f& c) noexcept
    {
        p3f tmp3, tmp2, tmp1, tmp0;
        tmp0 = _mm_unpacklo_ps(a, b);
        tmp2 = _mm_unpacklo_ps(c, c);
        tmp1 = _mm_unpackhi_ps(a, b);
        tmp3 = _mm_unpackhi_ps(c, c);
        a = _mm_movelh_ps(tmp0, tmp2);
        b = _mm_movehl_ps(tmp2, tmp0);
        c = _mm_movelh_ps(tmp1, tmp3);
        //d := _mm_movehl_ps(tmp3, tmp1);
    }

    p3f p3f_proj_to_tri_bary(p3f aa, p3f bb, p3f cc, p3f pt) noexcept
    {
        //https://www.geometrictools.com/Documentation/DistancePoint3Triangle3.pdf
        const p3f e0 = p3f_sub(aa, bb);
        const p3f e1 = p3f_sub(cc, bb);
        const p3f dd = p3f_sub(bb, pt);
        const float a = p3f_dot(e0, e0);
        const float b = p3f_dot(e0, e1);
        const float c = p3f_dot(e1, e1);
        const float d = p3f_dot(e0, dd);
        const float e = p3f_dot(e1, dd);

        float s = b * e - c * d;
        float t = b * d - a * e;
        float det = a * c - b * b;

        if (s + t <= det) {
            if (s < 0) {
                if (t < 0) {
                    //reg4
                    if (d < 0) {
                        t = 0;
                        if (-d >= a) s = 1;
                        else s = -d / a;
                    } else {
                        s = 0;
                        if (e >= 0) t = 0;
                        else if (-e >= c) t = 1;
                        else t = -e / c;
                    }
                } else {
                    //reg3
                    s = 0;
                    if (e >= 0) t = 0;
                    else if (-e >= c) t = 1;
                    else t = -e / c;
                }
            } else if (t < 0) {
                //reg5
                t = 0;
                if (d >= 0) s = 0;
                else if (-d >= a) s = 1;
                else s = -d / a;
            } else {
                //reg0
                s /= det;
                t /= det;
            }
        } else {
            if (s < 0) {
                //reg2
                float t0 = b + d, t1 = c + e;
                if (t1 > t0) {
                    float numer = t1 - t0;
                    float denom = a - 2 * b + c;
                    if (numer >= denom) s = 1;
                    else s = numer / denom;
                    t = 1 - s;
                } else {
                    s = 0;
                    if (t1 <= 0) t = 1;
                    else if (e >= 0) t = 0;
                    else t = -e / c;
                }
            } else if (t < 0) {
                //reg6
                float t0 = b + e, t1 = a + d;
                if (t1 > t0) {
                    float numer = t1 - t0;
                    float denom = a - 2 * b + c;
                    if (numer >= denom) t = 1;
                    else t = numer / denom;
                    s = 1 - t;
                } else {
                    t = 0;
                    if (t1 <= 0) s = 1;
                    else if (d >= 0) s = 0;
                    else s = -d / a;
                }
            } else {
                //reg1
                float numer = c + e - b - d;
                if (numer <= 0) {
                    s = 0;
                } else {
                    float denom = a - 2 * b + c;
                    if (numer >= denom) s = 1;
                    else s = numer / denom;
                }
                t = 1 - s;
            }
        }

        return p3f_set(s, 1.0f - s - t, t);
        //return p3f_fma(s, e0, p3f_fma(t, e1, bb)); // s*e0 + t*e1 + bb
    }
};  