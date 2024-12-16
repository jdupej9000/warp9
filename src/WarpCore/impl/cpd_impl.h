#pragma once

#include "../config.h"


namespace warpcore::impl 
{
    int cpd_lowrank_numcols(int m);
    int cpd_tmp_size(int m, int n, int k);
    void cpd_init_clustered(const float* y, int m, int k, float beta, float* q, float* lambda);
    float cpd_estimate_sigma(const float* x, const float* y, int m, int n, float* tmp);
    void cpd_estep(const float* x, const float* t, int m, int n, float w, float sigma2, float denom, float* psum, float* pt1, float* p1, float* px);
    float cpd_mstep(const float* y, const float* pt1, const float* p1, const float* px, const float* q, const float* l, const float* linv, int m, int n, int k, float sigma2, float lambda, float* t, float* tmp);
    float cpd_update_sigma2(const float* x, const float* t, const float* pt1, const float* p1, const float* px, int m, int n);
    int cpd_make_sortedx(const float* x, int n, float* sortedx);
    void cpd_narrow_trunc_window(const float* sortedxcol, const float* tcol, int m, int n, float thresh, int* bounds);
    void cpd_init_trunc_window(int n, int m, int* bounds);
};
