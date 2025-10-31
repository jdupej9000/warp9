#pragma once

#include "config.h"

enum class WCORE_OPTPATH : int
{
    AVX2 = 0x1,
    AVX512 = 0x2,
    HYBRID = 0x10000000,
    Best = 0x7fffffff
};

enum WCORE_STATUS {
    WCORE_OK = 0,
    WCORE_INVALID_ARGUMENT = -1,
    WCORE_INVALID_DIMENSION = -2,
    WCORE_NONCONVERGENCE = -3
};

struct rigid3 {
    float offs[3];
    float cs;
    float rot[9]; 
};
