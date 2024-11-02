#pragma once

#include <immintrin.h>

namespace warpcore::impl
{
    __m256 expf_fast(__m256 x);
    __m256 expf_schraudolph(__m256 x);
    
    double reduce_add(__m256d v);
    float reduce_add(__m256 v);
    int reduce_add_i32(__m256i v);

    float reduce_min(__m256 v);
    float reduce_max(__m256 v);

    void reduce_minmax(const float* x, int n, float* xmin, float* xmax);

    float reduce_add(const float* x, int n);
    float reduce_add_i32(const int* x, int n);

    __m256i clamp(__m256i x, __m256i x0, __m256i x1);

    // Y = diag(x) * A
    void dxa(const float* x, const float* v, int n, int m, float* y); 

    // Y = diag(x)^-1 * A
    void dxinva(const float* x, const float* v, int n, int m, float* y);

    // Y = alpha * A^T * diag(b) * A (Y is m*m)
    void atdba(const float* a, int n, int m, const float* b, float alpha, float* y);

    // trace(A^T * diag(b) * A)
    float tratdba(const float* a, int n, int m, const float* b);
};