#pragma once

#include "defs.h"
#include "config.h"

enum CPD_INIT_METHOD {
    CPD_INIT_EIGENVECTORS = 0,
    CPD_INIT_CLUSTERED = 1
};

enum CPD_FLAGS {
    CPD_USE_GPU = 1
};

struct cpdinfo {
    int32_t m, n, d;
    float lambda, beta, w, sigma2init;
    int32_t maxit, neigen, flags;
    float tol;
    int32_t debug_key;
};

enum CPD_CONV {
    CPD_CONV_ITER = 1,
    CPD_CONV_TOL = 2,
    CPD_CONV_SIGMA = 4,
    CPD_CONV_DSIGMA = 8,
    CPD_CONV_RTOL = 16,
    CPD_CONV_NUMERIC_ERROR = 32,
    CPD_CONV_INTERNAL_ERROR = 1048576
};

struct cpdresult {
    int32_t iter;
    int32_t conv;
    float err, sigma2;
    float time, time_e;
    int32_t debug;
};


// Initializes low-rank Coherent point drift algoritm. This involves creating
// a decomposition of the motion smoothing matrix G with the given method. The
// result is stored in init ( diag(Lambda), Q ). If init is NULL, the result is
// the number of Bytes required to store the result. If an error occurs, the result
// is a negative value, one of CPD_STATUS;
extern "C" WCEXPORT int cpd_init(cpdinfo* cpd, int method, const void* y, void* init);

// Processes the point cloud with CPD (y is registered onto x, saving into t).
// Result (if not NULL) will contain the registration statistics. Function returns
// 0 if the registration is successful, otherwise a negative value is returned, one
// of CPDS_STATUS.
extern "C" WCEXPORT int cpd_process(const cpdinfo* cpd, const void* x, const void* y, const void* init, void* t, cpdresult* result);
