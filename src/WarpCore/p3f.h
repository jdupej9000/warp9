#pragma once
#include <immintrin.h>
#include <cmath>

namespace warpcore
{
    typedef __m128 p3f;
    typedef __m128 planef;
    typedef __m128i p3i;

    p3f p3f_zero(void) noexcept;
    p3f p3f_set(float x) noexcept;
    p3f p3f_set(float x, float y, float z) noexcept;
    p3f p3f_set(const float* x);
    p3i p3i_set(int x) noexcept;
    p3i p3i_set(int x, int y, int z) noexcept;
    p3i p3i_set(const int* x) noexcept;
    p3i p3f_to_p3i(const p3f a) noexcept;
    p3f p3i_to_p3f(const p3i a) noexcept;
    void p3f_to_int(const p3f a, int& x, int& y, int& z) noexcept;
    int p3f_min_mask(p3f x) noexcept;
    float p3f_max(p3f a) noexcept;
    float p3f_min_removenan(p3f a) noexcept;
    p3f p3f_abs(p3f x) noexcept;
    p3f p3f_sign(p3f x) noexcept;
    float p3f_dot(p3f a, p3f b) noexcept;
    float p3f_lensq(p3f a) noexcept;
    float p3f_len(p3f a) noexcept;
    float p3f_dist(p3f a, p3f b) noexcept;
    float p3f_distsq(p3f a, p3f b) noexcept;
    p3f p3f_cross(p3f a, p3f b) noexcept;
    p3f p3f_normalize(p3f a) noexcept;
    p3f p3f_sub(p3f a, p3f b) noexcept;
    p3f p3f_add(p3f a, p3f b) noexcept;
    p3i p3i_add(p3i a, p3i b) noexcept;
    p3f p3f_mul(p3f a, p3f b) noexcept;
    p3f p3f_mul(float a, p3f b) noexcept;
    p3f p3f_div(p3f a, p3f b) noexcept;
    p3f p3f_recip(p3f a) noexcept;
    p3f p3f_fma(p3f a, p3f b, p3f c) noexcept;
    p3f p3f_fma(float a, p3f b, p3f c) noexcept;
    p3f p3f_mid(p3f a, p3f b) noexcept;
    p3f p3f_mid(p3f a, p3f b, p3f c) noexcept;
    p3f p3f_lerp(p3f a, p3f b, float t) noexcept;
    p3f p3f_lerp(p3f a, p3f b, p3f t) noexcept;
    bool p3f_in_aabb(p3f x, p3f box0, p3f box1) noexcept;
    p3f p3f_mask_is_almost_zero(p3f x) noexcept;
    p3f p3f_switch(p3f zero, p3f one, p3f mask) noexcept;
    p3i p3i_in_range(p3i x, p3i box0, p3i box1) noexcept;
    bool p3i_in_aabb(p3i x, p3i box0, p3i box1) noexcept;
    bool p3i_is_zero(p3i x) noexcept;
    p3i p3i_clamp(p3i x, p3i box0, p3i box1) noexcept;
    p3f p3f_clamp_zero(p3f x) noexcept;
    p3f p3f_xy(p3f x) noexcept;
    float p3f_addxy(p3f x) noexcept;
    int p3i_get(p3i x, int i) noexcept;

    p3f p3f_to_bary(p3f a, p3f b, p3f c, p3f p) noexcept;
    p3f p3f_from_bary(p3f a, p3f b, p3f c, p3f p) noexcept;

    p3f p3f_proj_to_plane(planef plane, p3f pt) noexcept;
    planef planef_from_center_normal(p3f c, p3f n) noexcept;
    planef planef_from_center_normaldir(p3f c, p3f nd) noexcept;

    float intersect_ray_aabb(p3f o, p3f d, p3f box0, p3f box1) noexcept;
    bool intersect_tri_aabb(p3f box0, p3f box1, p3f t0, p3f t1, p3f t2) noexcept;
    bool intersect_aabb_aabb(p3f a0, p3f a1, p3f b0, p3f b1) noexcept;
    void aabb_from_tri(p3f t0, p3f t1, p3f t2, p3f& box0, p3f& box1) noexcept; 
    p3f normal_from_tri(p3f a, p3f b, p3f c) noexcept;
    void tri_transpose_inplace(p3f& a, p3f& b, p3f& c) noexcept;

    p3f p3f_proj_to_tri(p3f aa, p3f bb, p3f cc, p3f pt) noexcept;
    
    template<int I>
    p3f p3f_broadcast(p3f a) noexcept
    {
        static_assert(I >= 0 && I < 4, "Lane index out of range.");
        return _mm_shuffle_ps(a, a, I | (I << 2) | (I << 4) | (I << 6)); // TODO: permute
    }

    template<int ISrc, int IDest>
    p3f p3f_insert(p3f a, p3f r) noexcept
    {
        static_assert(ISrc >= 0 && ISrc < 4, "Lane index out of range.");
        static_assert(IDest >= 0 && IDest < 4, "Lane index out of range.");
        return _mm_insert_ps(a, r, (IDest << 4) | (ISrc << 6)); 
    }

    template<int ISrc>
    float p3f_get(p3f x) noexcept
    {
        return _mm_cvtss_f32(_mm_permute_ps(x, ISrc));
    }
  
    constexpr int _cabs(int x) {
#if _MSC_VER
        return x >= 0 ? x : -x;
#else
        return abs(x);
#endif
    }

    // Applies a swizzle with indices known at compile time. If any of the indices
    // is negative, that lane will be multiplied by -1 at a minor efficiency cost.
    // If any of the indices is zero, that lane will be set to zero, at furhter minor
    // efficiency cost. Permute is elided if there is no change in lane order.
    template<int IX, int IY, int IZ>
    p3f p3f_swizzle(p3f a)
    {
        // Set all to zero - easy peasy.
        if constexpr (IX == 0 && IY == 0 && IZ == 0)
            return _mm_setzero_ps();

        // Compute permute indices.
        constexpr int PX = (IX == 0) ? 0 : (_cabs(IX) - 1);
        constexpr int PY = (IY == 0) ? 0 : (_cabs(IY) - 1);
        constexpr int PZ = (IZ == 0) ? 0 : (_cabs(IZ) - 1);

        p3f ret = a; 
        
        // Permute only if there is a change in lane ordering.
        if constexpr (PX != 0 || PY != 1 || PZ != 2)
            ret = _mm_permute_ps(a, PX | (PY << 2) | (PZ << 4));

        // Negate selected lanes if needed.
        if constexpr (IX < 0 || IY < 0 || IZ < 0) {
            p3f mret = _mm_xor_ps(ret, _mm_set1_ps(-0.0f));           
            constexpr int mblend = ((IX < 0) ? 1 : 0) | ((IY < 0) ? 2 : 0) | ((IZ < 0) ? 4 : 0);
            ret = _mm_blend_ps(ret, mret, mblend);
        }

        // Set selected lanes to zero if needed.
        if constexpr (IX == 0 || IY == 0 || IZ == 0) {
            constexpr int zblend = ((IX == 0) ? 1 : 0) | ((IY == 0) ? 2 : 0) | ((IZ == 0) ? 4 : 0);
            ret = _mm_blend_ps(ret, _mm_setzero_ps(), zblend);
        }

        return ret;
    }


};