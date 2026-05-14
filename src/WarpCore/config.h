#pragma once

#include <stdint.h>
#include <assert.h>

// Set to have CDP use double accumulators in some cases, instead of float. This should
// address some numerical stability issues.
#define WCORE_CPD_DOUBLE_ACCUM

#ifdef _MSC_VER

    #define STACK_ALLOC(T,N) (T*)_malloca((N) * sizeof(T))
    
    #ifdef WARPCORE_IMPORTS
    #define WCEXPORT __declspec(dllimport)
    #else
    #define WCEXPORT __declspec(dllexport)
    #endif

    #define WCORE_VECCALL __vectorcall

#else

    #define STACK_ALLOC(T,N) (T*)alloca((N) * sizeof(T))
    #define WCEXPORT

    #define WCORE_VECCALL

#endif

#define WCORE_VER (0002)

#define WCORE_ASSERT(x) assert(x)

#if defined(_MSC_VER)
#define _CRT_USE_C_COMPLEX_H
#include <complex.h>
#define LAPACK_COMPLEX_CUSTOM
#define lapack_complex_float _Fcomplex
#define lapack_complex_double _Dcomplex
#endif