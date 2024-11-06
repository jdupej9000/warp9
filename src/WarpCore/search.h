#pragma once

#include "defs.h"
#include "config.h"

enum SEARCH_STRUCTURE {
    SEARCH_TRIGRID3 = 0
};

enum SEARCHD_KIND {
    SEARCHD_NN_PCL_3 = 0,
    SEARCHD_RAYCAST_TRISOUP_3 = 1,
    SEARCHD_NN_TRISOUP_3 = 2
};

enum SEARCH_KIND {
    SEARCH_NN_DPTBARY = 0,
    SEARCH_RAYCAST_T = 1,
    SEARCH_RAYCAST_TBARY = 2,

    SEARCH_SOURCE_IS_AOS = 0x10000000
};

struct trigrid_config { 
    int num_cells; 
};

extern "C" WCEXPORT int search_build(int structure, const float* vert, const int* idx, int nv, int nt, const void* config, void** ctx);
extern "C" WCEXPORT int search_free(void* ctx);
extern "C" WCEXPORT int search_direct(int kind, const float* orig, const float* dir, const float* vert, int n);
extern "C" WCEXPORT int search_query(const void* ctx, int kind, const float* orig, const float* dir, int n, int* hit, void* info);