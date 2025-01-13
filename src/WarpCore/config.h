#pragma once

#include <stdint.h>
#include <assert.h>

#ifdef _MSC_VER

    #define STACK_ALLOC(T,N) (T*)_malloca((N) * sizeof(T))
    
    #ifdef WARPCORE_IMPORTS
    #define WCEXPORT __declspec(dllimport)
    #else
    #define WCEXPORT __declspec(dllexport)
    #endif

#else

    #define STACK_ALLOC(T,N) (T*)alloca((N) * sizeof(T))
    #define WCEXPORT

#endif

#define WCORE_VER (0001)

#define WCORE_ASSERT(x) assert(x)

#if defined(_MSC_VER)
#define _CRT_USE_C_COMPLEX_H
#include <complex.h>
#define LAPACK_COMPLEX_CUSTOM
#define lapack_complex_float _Fcomplex
#define lapack_complex_double _Dcomplex
#endif