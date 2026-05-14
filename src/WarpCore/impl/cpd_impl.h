#pragma once

#include "../config.h"


namespace warpcore::impl 
{
    int cpd_lowrank_numcols(int m);
    int cpd_tmp_size(int m, int n, int k);
    void cpd_init_clustered(const float* y, int m, int k, int kextra, float beta, float* q, float* lambda);
    double cpd_estimate_sigma(const float* x, const float* y, int m, int n, float* tmp);
    double cpd_estep(const float* x, const float* t, int m, int n, float w, float sigma2, float denom, float* psum, float* pt1, float* p1, float* px);
    double cpd_mstep(const float* y, const float* pt1, const float* p1, const float* px, const float* q, const float* l, const float* linv, int m, int n, int k, float sigma2, float lambda, float* t, float* tmp);
    double cpd_update_sigma2(const float* x, const float* t, const float* pt1, const float* p1, const float* px, int m, int n);
};
