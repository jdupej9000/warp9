#pragma once

#include "../config.h"
#include <immintrin.h>

namespace warpcore::impl
{
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

    void WCORE_VECCALL demux(__m256i& a, __m256i& b, __m256i& c);

    void WCORE_VECCALL cross(__m256 ax, __m256 ay, __m256 az, __m256 bx, __m256 by, __m256 bz, __m256& cx, __m256& cy, __m256& cz) noexcept;
    __m256 WCORE_VECCALL dot(__m256 ax, __m256 ay, __m256 az, __m256 bx, __m256 by, __m256 bz) noexcept;
    __m256 WCORE_VECCALL blend_in(__m256 x, __m256 y, __m256 mask) noexcept;

    float WCORE_VECCALL extract(__m256 v, int index);
    int WCORE_VECCALL extract(__m256i v, int index);
    int WCORE_VECCALL find_min_index(__m256 v);

    void reduce_minmax(const float* x, int n, float* xmin, float* xmax);

    float reduce_add(const float* x, int n);
    float reduce_add_i32(const int* x, int n);
    int64_t reduce_add_i1(const void* x, int n);

    __m256i WCORE_VECCALL clamp(__m256i x, __m256i x0, __m256i x1);

    // Y = diag(x) * A
    void dxa(const float* x, const float* v, int n, int m, float* y); 

    // Y = diag(x)^-1 * A
    void dxinva(const float* x, const float* v, int n, int m, float* y);

    // Y = alpha * A^T * diag(b) * A (Y is m*m)
    void atdba(const float* a, int n, int m, const float* b, float alpha, float* y);

    // trace(A^T * diag(b) * A)
    float tratdba(const float* a, int n, int m, const float* b);

    // res = sum_i((cols_i-center) * weights_i)
    void wsumc(const float** cols, const float* center, const float* weights, int n, int m, float* res);

    float dot(const float* x, const float* y, int n);
    void scale(float* x, float f, int n);
    void normalize_columns(float* mat, int rows, int cols);
};