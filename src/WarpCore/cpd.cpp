#define _USE_MATH_DEFINES

#include "cpd.h"
#include "impl/cpd_impl.h"
#include <memory>
#include <cstring>
#include <chrono>

extern bool cpd_init_cuda(int m, int n, const void* x, void** ppDevCtx);
extern void cpd_deinit_cuda(void* pDevCtx);
extern void cpd_estep_cuda(void* pDevCtx, const float* x, const float* t, int m, int n, float w, float sigma2, float denom, float* pt1p1px);
extern float cpd_estimate_sigma_cuda(void* pDevCtx, const float* x, const float* t, int m, int n);
int cpd_get_convergence(const cpdinfo* cpd, int it, float sigma2, float sigma2_old, float err, float err_old);

constexpr float CPD_SIGMA2_CONV_TRESH = 1e-8f;
constexpr float CPD_DELTA_SIGMA2_CONV_THRESH = 1e-6f;
constexpr float CPD_REL_SIGMA2_CONV_THRESH = 1e-5f;
constexpr float CPD_REL_ERROR_CONV_THRESH = 1e-3f;

using namespace warpcore::impl;

extern "C" int cpd_init(cpdinfo* cpd, int method, const void* y, void* init)
{
    if(cpd == NULL || (y == NULL && init != NULL) || cpd->d != 3)
        return WCORE_INVALID_ARGUMENT;

    // We determine the number of eigenvectors as ceil(cbrt(m)). For numerics' sake, we
    // sample the G matrix in more columns than we need eigenvectors. After orthogonalization,
    // we discard the extras.
    const int num_eigs = (cpd->neigen > 0) ? cpd->neigen : cpd_lowrank_numcols(cpd->m);
    const int num_g_cols = num_eigs + num_eigs / 4;

    cpd->neigen = num_eigs;

    const int m = cpd->m;

    if(init == NULL)
        return sizeof(float) * (m * num_g_cols + 2 * m);

    float* lambda = (float*)init;
    float* lambda_inv = lambda + m;
    float* q = lambda_inv + m;

    switch(method)
    {
        //case CPD_INIT_EIGENVECTORS:
        //    break;

        case CPD_INIT_CLUSTERED:
            cpd_init_clustered((const float*)y, m, num_eigs, num_g_cols, cpd->beta, q, lambda);
            break;

        default:
            return WCORE_INVALID_ARGUMENT;
    }

    for(int i = 0; i < num_eigs; i++)
        lambda_inv[i] = 1.0f / lambda[i];

    return WCORE_OK;
}

extern "C" int cpd_process(cpdinfo* cpd, const void* x, const void* y, const void* init, void* t, cpdresult* result)
{
    if(cpd == NULL || y == NULL || x == NULL || t == NULL || init == NULL)
        return WCORE_INVALID_ARGUMENT;

    if(cpd->d != 3)
        return WCORE_INVALID_ARGUMENT;

    int debug = 0;
    auto t0 = std::chrono::high_resolution_clock::now();

    bool use_cuda = cpd->flags & CPD_USE_GPU;
    const int m = cpd->m, n = cpd->n;
    const int num_eigs = (cpd->neigen > 0) ? cpd->neigen : cpd_lowrank_numcols(m);
    const int tmp_size = cpd_tmp_size(m, n, num_eigs);

    // Do not change the order of these arrays. CUDA part relies on this structure.
    float* pp = new float[2 * n + 4 * m + tmp_size + 3 * m];
    float* psum = pp;
    float* pt1 = psum + n;
    float* p1 = pt1 + n;
    float* px = p1 + m;
    float* tmp = px + 3 * m;
    float* ttemp = tmp + tmp_size;

    float* cuda_ctx = nullptr;
    if (use_cuda) {
        if (!cpd_init_cuda(m, n, x, (void**)&cuda_ctx)) {
            delete[] pp;
            return CPD_CONV_INTERNAL_ERROR;
        }
    }

    float sigma2 = cpd->sigma2init;

    if (cpd->sigma2init <= 0.0f) {
        if (use_cuda)
            sigma2 = cpd_estimate_sigma_cuda(cuda_ctx, (const float*)x, (const float*)y, m, n);
        else
            sigma2 = cpd_estimate_sigma((const float*)x, (const float*)y, m, n, pp);
    }

    std::memset(pp, 0, sizeof(float) * (2 * n + 4 * m + tmp_size));

    const float* q = (const float*)init + 2 * m;
    const float* l = (const float*)init;
    const float* linv = (const float*)init + m;
    const int maxit = cpd->maxit;

    std::memcpy(t, y, sizeof(float) * 3 * m);

    int conv = 0;
    float l0 = 0;
    float tol = FLT_MAX, tol_old = FLT_MAX;
    int it = 0;
    int64_t etime = 0;

    while (!conv) {
        float l0_old = l0;
        float denom = cpd->w / (1.0f - cpd->w) * powf(2.0f * (float)M_PI * sigma2, 1.5f) * (float)m / (float)n;

        auto te0 = std::chrono::high_resolution_clock::now();
        if (use_cuda) {
            cpd_estep_cuda(cuda_ctx, (const float*)x, (const float*)t, m, n, cpd->w, sigma2, denom, pt1);
        } else {
            cpd_estep((const float*)x, (const float*)t, m, n, cpd->w, sigma2, denom, psum, pt1, p1, px);
        }
        auto te1 = std::chrono::high_resolution_clock::now();
        etime += std::chrono::duration_cast<std::chrono::microseconds>(te1-te0).count();

        memset(tmp, 0, tmp_size * sizeof(float));
        l0 += cpd_mstep((const float*)y, pt1, p1, px, q, l, linv, m, n, num_eigs, sigma2, cpd->lambda, (float*)ttemp, tmp);
        if (isnan(l0) || isnan(abs((l0 - l0_old) / l0))) {
            conv = CPD_CONV_NUMERIC_ERROR;
            debug = 1;
            break;
        }
        
        tol = abs((l0 - l0_old) / l0);
        const float sigma2_old = sigma2;
        sigma2 = cpd_update_sigma2((const float*)x, (const float*)ttemp, pt1, p1, px, m, n);
        if (isnan(sigma2)) {
            conv = CPD_CONV_NUMERIC_ERROR;
            debug = 2;
            break;
        }

        conv |= cpd_get_convergence(cpd, it, sigma2, sigma2_old, tol, tol_old);

        if ((conv & CPD_CONV_NUMERIC_ERROR) == 0)
            std::memcpy(t, ttemp, sizeof(float) * 3 * m);
        else
            debug = 3;

        tol_old = tol;
        it++;
    }

    auto t1 = std::chrono::high_resolution_clock::now();

    delete[] pp;

    if (use_cuda)
        cpd_deinit_cuda(cuda_ctx);

    if (result) {
        result->iter = it;
        result->conv = conv;
        result->sigma2 = sigma2;
        result->err = tol;
        result->time_e = 1e-6f * etime;
        result->time = 1e-6f * std::chrono::duration_cast<std::chrono::microseconds>(t1-t0).count();
        result->debug = debug;
    }

    return (it < maxit) ? WCORE_OK : WCORE_NONCONVERGENCE;
}

int cpd_get_convergence(const cpdinfo* cpd, int it,float sigma2, float sigma2_old, float err, float err_old)
{
    int conv = 0;
    if (it >= cpd->maxit)
        conv |= CPD_CONV_ITER;

    if (sigma2 < CPD_SIGMA2_CONV_TRESH)
        conv |= CPD_CONV_SIGMA;

    if (abs(sigma2 - sigma2_old) < CPD_DELTA_SIGMA2_CONV_THRESH ||
        abs(sigma2 - sigma2_old) / sigma2 < CPD_REL_SIGMA2_CONV_THRESH)
        conv |= CPD_CONV_DSIGMA;

    if (isnan(abs(sigma2 - sigma2_old) / sigma2))
        conv |= CPD_CONV_NUMERIC_ERROR;

    if (err < cpd->tol)
        conv |= CPD_CONV_TOL;

    if (abs(err_old - err) / err_old < CPD_REL_ERROR_CONV_THRESH)
        conv |= CPD_CONV_RTOL;

    return conv;
}