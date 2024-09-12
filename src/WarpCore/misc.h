#pragma once
#include "config.h"

enum WCORE_INFO_INDEX
{
    WCINFO_VERSION = 0,
    WCINFO_MKL_VERSION = 1,
    WCINFO_MKL_ISA = 2
};

extern "C" WCEXPORT int wcore_get_info(int index, char* buffer, int bufferSize);