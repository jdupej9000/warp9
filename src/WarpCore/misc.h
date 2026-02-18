#pragma once
#include "config.h"

enum WCORE_INFO_INDEX
{
    WCINFO_VERSION = 0,
    WCINFO_COMPILER = 1,
    WCINFO_OPT_PATH = 2,
    WCINFO_CPU_NAME = 3,
    WCINFO_BUILD_DATE = 4,
    WCINFO_GIT_HASH = 5,
    WCINFO_OPENMP_THREADS = 6,
    WCINFO_OPENBLAS_VERSION = 1000,
    WCINFO_OPENBLAS_CONFIG = 1001,
    WCINFO_CUDA_DEVICE = 2000,
    WCINFO_CUDA_DEVICE_SM_COUNT = 2001,
    WCINFO_CUDA_DEVICE_MEMORY = 2002,
    WCINFO_CUDA_DEVICE_COMPUTE_CAP = 2003,
    WCINFO_CUDA_RUNTIME_VERSION = 2100,
    WCINFO_CUDA_DRIVER_VERSION = 2101
};

extern "C" WCEXPORT int wcore_get_info(int index, char* buffer, int bufferSize);
extern "C" WCEXPORT int set_optpath(int path);
extern "C" WCEXPORT int clust_kmeans(const float* x, int d, int n, int k, float* cent, int* label);