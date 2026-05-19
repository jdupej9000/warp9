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

    p3f p3f_set(p3f p) noexcept
    {
        return _mm_blend_ps(p, _mm_setzero_ps(), 0b1000);
    }

    void p3f_store(float* x, p3f pt)
    {
        int* xi = (int*)x;
        ((int64_t*)xi)[0] = _mm_extract_epi64(_mm_castps_si128(pt), 0);
        //xi[0] = _mm_extract_ps(pt, 0);
        //xi[1] = _mm_extract_ps(pt, 1);
        xi[2] = _mm_extract_ps(pt, 2);
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
        __m128i rounded = _mm_cvtps_epi32(_mm_round_ps(a, (_MM_FROUND_TO_NEG_INF | _MM_FROUND_NO_EXC)));
        x = _mm_extract_epi32(rounded, 0);
        y = _mm_extract_epi32(rounded, 1);
        z = _mm_extract_epi32(rounded, 2);
        /*alignas(16) int xi[4];
        _mm_store_si128((__m128i*)xi, rounded);
        x = xi[0];
        y = xi[1];
        z = xi[2];*/
    }

    int p3i_get(p3i x, int i) noexcept
    {
        //alignas(16) int xi[4];
        //_mm_store_si128((__m128i*)xi, x);
        //return xi[i];

        return _mm_extract_epi32(_mm_castps_si128(_mm_permutevar_ps(_mm_castsi128_ps(x),
            _mm_cvtsi32_si128(i))), 0);
    }

    int p3i_sum(p3i x) noexcept
    {
        return _mm_extract_epi32(x, 0) + _mm_extract_epi32(x, 1) + _mm_extract_epi32(x, 2);
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
        p3f xmin = _mm_min_ps(x, _mm_min_ss(_mm_shuffle_ps(x, x, 0b01), _mm_shuffle_ps(x, x, 0b10)));
        xmin = _mm_shuffle_ps(xmin, xmin, 0);
        __m128i maskf = _mm_cmpeq_epi32(_mm_castps_si128(x), _mm_castps_si128(xmin));
        int mask = _mm_movemask_epi8(maskf);
        return (int)_pext_u32(mask, 0x111);
    }

    p3f p3f_min_mask_full(p3f x) noexcept
    {
        p3f xmin = _mm_min_ps(x, _mm_min_ps(_mm_shuffle_ps(x, x, 0b01), _mm_shuffle_ps(x, x, 0b10)));
        xmin = _mm_shuffle_ps(xmin, xmin, 0);
        return _mm_cmpeq_ps(x, xmin);
    }

    float p3f_max(p3f a) noexcept
    {
        return _mm_cvtss_f32(_mm_max_ss(a, _mm_max_ss(_mm_shuffle_ps(a, a, 0b01), _mm_shuffle_ps(a, a, 0b10))));
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
        __m128 ab = _mm_mul_ps(a, b);
        return _mm_cvtss_f32(_mm_add_ps(ab, _mm_add_ps(_mm_permute_ps(ab, 1), _mm_permute_ps(ab, 2))));
        //return _mm_cvtss_f32(_mm_dp_ps(a, b, 0x71));
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
        __m128 aa = _mm_mul_ps(a, a);

        // the first permute is only to set the upper lane and prevent NaNs when doing rsqrt.
        aa = _mm_add_ps(_mm_permute_ps(aa, 0b00100100), _mm_add_ps(
            _mm_permute_ps(aa, 0b00001001),
            _mm_permute_ps(aa, 0b00010010)));

        return _mm_mul_ps(a, _mm_rsqrt_ps(aa));
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

    p3i p3i_mul(p3i a, p3i b) noexcept
    {
        return _mm_mullo_epi32(a, b);
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

    p3f p3f_proj_to_aabb(p3f x, p3f box0, p3f box1) noexcept
    {
        return _mm_max_ps(_mm_min_ps(x, box1), box0);
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
};  