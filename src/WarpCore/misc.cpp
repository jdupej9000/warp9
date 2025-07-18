#include "misc.h"
#include <sstream>
#include <cstring>
#include <string.h>
#include <openblas_config.h>
#include <cblas.h>
#include <cuda_runtime.h>
#include "impl/kmeans.h"
#include "impl/cpu_info.h"
#include "defs.h"

using namespace std;
using namespace warpcore::impl;

extern "C" int wcore_get_info(int index, char* buffer, int bufferSize)
{
    stringstream ss{};
    int device, version = 0;

    switch (index) {
    case WCINFO_VERSION:
        ss << "warpcore " << (WCORE_VER / 100) % 100 << "." << WCORE_VER % 100;
        break;
    case WCINFO_COMPILER:
#if defined(__INTEL_LLVM_COMPILER)
        ss << "Intel(R) oneAPI DPC++/C++ Compiler " <<
            (__INTEL_LLVM_COMPILER / 10000) << "." <<
            (__INTEL_LLVM_COMPILER / 100) % 100 << "." <<
            __INTEL_LLVM_COMPILER % 100;
#elif defined(_MSC_FULL_VER)
        ss << "MSVC++ " << ((_MSC_FULL_VER / 10000000) % 100) << "." << 
            ((_MSC_FULL_VER / 100000) % 100) << "."
            << (_MSC_FULL_VER % 100000);
#else
        ss << "unknown";
#endif
        break;

    case WCINFO_OPT_PATH:
        switch (get_optpath()) {
            case WCORE_OPTPATH::AVX2:       ss << "avx2"; break;
            case WCORE_OPTPATH::AVX512:     ss << "avx512"; break;
            default:                        ss << "unknown"; break;
        }
        break;

    case WCINFO_CPU_NAME: 
        ss << get_cpuname();
        break;

    case WCINFO_OPENBLAS_VERSION:
        ss << OPENBLAS_VERSION;
        break;

    case WCINFO_OPENBLAS_CONFIG:
        ss << openblas_get_config();
        break;

    case WCINFO_CUDA_DEVICE: 
        if (cudaGetDevice(&device) != cudaSuccess) {
            ss << "Cannot get a CUDA device.";
        } else {
            cudaDeviceProp props;
            cudaGetDeviceProperties(&props, device);
            ss << props.name;
        }
        break;
    
    case WCINFO_CUDA_RUNTIME_VERSION:
        if (cudaRuntimeGetVersion(&version) == cudaSuccess && version != 0) {
            ss << version / 1000 << "." << (version / 10) % 100;
        } else {
            ss << "Cannot get version.";
        }
        break;

    case WCINFO_CUDA_DRIVER_VERSION:
        if (cudaDriverGetVersion(&version) == cudaSuccess && version != 0) {
            ss << version / 1000 << "." << (version / 10) % 100;;
        }
        else {
            ss << "Cannot get version.";
        }
        break;

    default:
        ss << "Invalid index.";
        break;
    }

    size_t sslen = ss.tellp();
    
    if(bufferSize > 0)
        strncpy_s(buffer, bufferSize, ss.str().c_str(), std::min(sslen, (size_t)bufferSize));
    
    return (int)sslen;
}

extern "C" int set_optpath(int path)
{
    restrict_optpath((WCORE_OPTPATH)path);
    return (int)get_optpath();
}

extern "C" int clust_kmeans(const float* x, int d, int n, int k, float* cent, int* label)
{
    if (x == nullptr || d != 3 || cent == nullptr || label == nullptr)
        return WCORE_INVALID_ARGUMENT;

    warpcore::impl::kmeans<3>(x, n, k, cent, label, nullptr);

    return WCORE_OK;
}