#define _USE_MATH_DEFINES
//#define DEBUG_CPD

#include "cpd.h"
#include "impl/utils.h"
#include "impl/cpd_impl.h"
#include <memory>
#include <cstring>
#include <chrono>

extern bool cpd_init_cuda(int m, int n, const void* x, void** ppDevCtx, void** ppStream);
extern void cpd_deinit_cuda(void* pDevCtx, void* pStream);
extern float cpd_estep_cuda(void* pDevCtx, void* pStream, const float* x, const float* t, int m, int n, float w, float sigma2, float denom, float* pt1p1px);
extern float cpd_estimate_sigma_cuda(void* pDevCtx, void* pStream, const float* x, const float* t, int m, int n);
int cpd_get_convergence(const cpdinfo* cpd, int it, float sigma2, float sigma2_old, float err, float err_old);

constexpr float CPD_SIGMA2_CONV_TRESH = 1e-8f;
constexpr float CPD_DELTA_SIGMA2_CONV_THRESH = 1e-6f;
constexpr float CPD_REL_SIGMA2_CONV_THRESH = 1e-5f;
constexpr float CPD_REL_ERROR_CONV_THRESH = 1e-5f;

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

extern "C" int cpd_process(const cpdinfo* cpd, const void* x, const void* y, const void* init, void* t, cpdresult* result)
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
    float* pp = new float[2 * n + 4 * m + tmp_size + 3 * m + 2 * 3 * m + 3 * n];
    float* pt1 = pp;
    float* p1 = pt1 + n;
    float* px = p1 + m;
    float* psum = px + 3 * m;
    float* tmp = psum + n;
    float* ttemp = tmp + tmp_size;
    float* xt = ttemp + 3 * m;
    float* yt = xt + 3 * n;
    float* tt = yt + 3 * m;

    aos_to_soa<float, 3>((const float*)x, n, xt);
    aos_to_soa<float, 3>((const float*)y, m, yt);

    float* cuda_ctx = nullptr;
    void* cuda_stream = nullptr;
    if (use_cuda) {
        if (!cpd_init_cuda(m, n, xt, (void**)&cuda_ctx, &cuda_stream)) {
            delete[] pp;
            return CPD_CONV_INTERNAL_ERROR;
        }
    }

    float sigma2 = cpd->sigma2init;

    if (cpd->sigma2init <= 0.0f) {
        if (use_cuda)
            sigma2 = cpd_estimate_sigma_cuda(cuda_ctx, cuda_stream, xt, yt, m, n);
        else
            sigma2 = cpd_estimate_sigma(xt, yt, m, n, pp);
    }

    std::memset(pp, 0, sizeof(float) * (2 * n + 4 * m + tmp_size));

    const float* q = (const float*)init + 2 * m;
    const float* l = (const float*)init;
    const float* linv = (const float*)init + m;
    const int maxit = cpd->maxit;

    std::memcpy(tt, yt, sizeof(float) * 3 * m);

    int conv = 0;
    double l0 = 0;
    float tol = FLT_MAX, tol_old = FLT_MAX;
    int it = 0;
    int64_t etime = 0;

    //debug_pcl("cpd", cpd->debug_key, 0, tt, m, true);

    while (!conv) {
        double l0_old = l0;
        float denom = cpd->w / (1.0f - cpd->w) * powf(2.0f * (float)M_PI * sigma2, 1.5f) * (float)m / (float)n;

        auto te0 = std::chrono::high_resolution_clock::now();
        if (use_cuda) {
            l0 = cpd_estep_cuda(cuda_ctx, cuda_stream, xt, tt, m, n, cpd->w, sigma2, denom, pp);
        } else {
            cpd_estep(xt, tt, m, n, cpd->w, sigma2, denom, psum, pt1, p1, px);
        }

#if defined(DEBUG_CPD)
        debug_matrix("pt1", it, pt1, n, 1);
        debug_matrix("psum", it, psum, n, 1);
        debug_matrix("p1", it, p1, m, 1);
        debug_matrix("px", it, px, m, 3);
#endif
        
        auto te1 = std::chrono::high_resolution_clock::now();
        etime += std::chrono::duration_cast<std::chrono::microseconds>(te1-te0).count();

        memset(tmp, 0, tmp_size * sizeof(float));
        l0 += cpd_mstep(yt, pt1, p1, px, q, l, linv, m, n, num_eigs, sigma2, cpd->lambda, (float*)ttemp, tmp);
        if (isnan(l0) || isnan(abs((l0 - l0_old) / l0))) {
            conv = CPD_CONV_NUMERIC_ERROR;

            if (isnan(l0))
                debug = 1;
            else
                debug = 2;

            break;
        }
        
        tol = abs((l0 - l0_old) / l0);
        const float sigma2_old = sigma2;
        sigma2 = cpd_update_sigma2(xt, ttemp, pt1, p1, px, m, n);
        if (isnan(sigma2)) {
            conv = CPD_CONV_NUMERIC_ERROR;
            debug = 3;
            break;
        }

        conv |= cpd_get_convergence(cpd, it, sigma2, sigma2_old, tol, tol_old);

        if ((conv & CPD_CONV_NUMERIC_ERROR) == 0 && tol > 0) // tol > 0 is important
            std::memcpy(tt, ttemp, sizeof(float) * 3 * m);
        else
            debug = 4;

        //debug_pcl("cpd", cpd->debug_key, it + 1, tt, m, true);

        tol_old = tol;
        it++;
    }

    auto t1 = std::chrono::high_resolution_clock::now();

    soa_to_aos<float, 3>((float*)t, m, tt);

    delete[] pp;

    if (use_cuda)
        cpd_deinit_cuda(cuda_ctx, cuda_stream);

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