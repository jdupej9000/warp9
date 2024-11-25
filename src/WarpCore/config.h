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

#define WCORE_ASSERT(x) assert(x)
