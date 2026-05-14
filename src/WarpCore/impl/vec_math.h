#pragma once

#include "../config.h"
#include <immintrin.h>
#include <intrin.h>

namespace warpcore::impl
{
    __m256 _mm256_abs_ps(__m256 x);

    float WCORE_VECCALL expf_fast(float x);
    __m256 WCORE_VECCALL expf_fast(__m256 x);
    __m512 WCORE_VECCALL expf_fast(__m512 x);
    __m256 WCORE_VECCALL expf_schraudolph(__m256 x);
    
    double WCORE_VECCALL reduce_add(__m256d v);
    float WCORE_VECCALL reduce_add(__m256 v);
    float WCORE_VECCALL reduce_add(__m512 v);
    int WCORE_VECCALL reduce_add_i32(__m256i v);

    float WCORE_VECCALL reduce_min(__m256 v);
    float WCORE_VECCALL reduce_max(__m256 v);

    int is_corrupted(__m256 v);

    // Reorder a section of AoS data (a,b,c)=(x0,y0,z0,x1,y1...z7) into a block
    // of AoSoA data a=(x0..x7), b=(y0..y7), c=(z0..z7).
    void WCORE_VECCALL demux(__m256& a, __m256& b, __m256& c);
    void WCORE_VECCALL demux(__m256i& a, __m256i& b, __m256i& c);

    // Calculate vertical cross products a x b -> c.
    void WCORE_VECCALL cross(__m256 ax, __m256 ay, __m256 az, __m256 bx, __m256 by, __m256 bz, __m256& cx, __m256& cy, __m256& cz) noexcept;

    // Calculate vertical dot products, returns a.b
    __m256 WCORE_VECCALL dot(__m256 ax, __m256 ay, __m256 az, __m256 bx, __m256 by, __m256 bz) noexcept;

    // For each 32b lane: return mask=0xffffffff ? y : x.
    __m256 WCORE_VECCALL blend_in(__m256 x, __m256 y, __m256 mask) noexcept;

    // Extract float at position index from v and return.
    float WCORE_VECCALL extract(__m256 v, int index);

    // Extract int at position index from v and return.
    int WCORE_VECCALL extract(__m256i v, int index);

    // Finds the lane index at which the minimum occurs. If there are collisions,
    // the lowest index is returned.
    int WCORE_VECCALL find_min_index(__m256 v);

    // Update minima and maxima (at xmin, xmax) by values in x.
    void reduce_minmax(const float* x, int n, float* xmin, float* xmax);

    float reduce_add(const float* x, int n);
    double reduce_add2(const float* x, int n);
    int reduce_add_i32(const int* x, int n);

    // Round integers at x up to multiples of r and reduce-add. r must be a power of 2.
    int reduce_roundup_add_i32(const int* x, int n, int r);

    // Count the number of bits at x. n is the number of bits to be added, however this will
    // be internally rounded up to the closest byte.
    int64_t reduce_add_i1(const void* x, int n);

    // Clamp packed integers vertically.
    __m256i WCORE_VECCALL clamp(__m256i x, __m256i x0, __m256i x1);

    // Y = diag(x) * A
    void dxa(const float* x, const float* v, int n, int m, float* y); 

    // Y += alpha * X
    void axpy(float* y, const float* x, float alpha, int n);

    // Y = diag(x)^-1 * A
    void dxinva(const float* x, const float* v, int n, int m, float* y);

    // Y = alpha * A^T * diag(b) * A (Y is m*m)
    void atdba(const float* a, int n, int m, const float* b, float alpha, float* y);

    // trace(A^T * diag(b) * A)
    float tratdba(const float* a, int n, int m, const float* b);
    double tratdba2(const float* a, int n, int m, const float* b);

    // res = sum_i((cols_i-center) * weights_i)
    void wsumc(const float** cols, const float* center, const float* weights, int n, int m, float* res);

    // Compute dot product of vectors at x and y of length n.
    float dot(const float* x, const float* y, int n);

    // Compute dot product of vectors at x and y of length n but use a double
    // precision accumulator.
    double dot2(const float* x, const float* y, int n);

    // Create a mask vector where 0xffffffff marks positive lanes of x and 0x0 the remaining ones.
    __m256 WCORE_VECCALL mask_positive(__m256 x) noexcept;

    // Create a mask vector where 0xffffffff marks negative lanes of x and 0x0 the remaining ones.
    __m256 WCORE_VECCALL mask_negative(__m256 x) noexcept;

    // Multiply elements at x by f in-place.
    void scale(float* x, float f, int n);

    // Add f to elements at x, in-place.
    void add(float* x, float f, int n);

    // Assume mat contains a column-major matrix of dimensions (rows, cols). Normalize its
    // columns in-place, so that their Euclinean norm is 1.
    void normalize_columns(float* mat, int rows, int cols);

    // Throw an exception if any element of x is not finite.
    void check_finite(const float* x, size_t len);

    // Replace all non-normal elements of x with repl.
    void replace_nan(float* x, size_t len, float repl);

    #define ASSERT_NORMAL(x) { if(is_corrupted(x)) __debugbreak(); }
};