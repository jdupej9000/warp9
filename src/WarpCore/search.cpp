#include "search.h"
#include "impl/tri_grid.h"
#include "impl/vec_math.h"
#include "impl/utils.h"
#include "impl/search_impl.h"
#include <float.h>
#include <math.h>
#include <memory>
#include <cstring>

using namespace warpcore::impl;

struct query_info {
    const trigrid* g;
    int* hit;
    void* info;
};

extern "C" int search_build(int structure, const float* vert, const int* idx, int nv, int nt, const void* config, void** ctx)
{
    if(structure == SEARCH_TRIGRID3) {
        const trigrid_config* cfg = (const trigrid_config*)config;
        int num_cells = (cfg && cfg->num_cells > 0) ? cfg->num_cells : 16;
        trigrid* g = new trigrid;
        g->__magic = SEARCH_TRIGRID3;
        *ctx = g;
        trigrid_build(g, vert, idx, nv, nt, num_cells);
        return WCORE_OK;
    }
     
    return WCORE_INVALID_ARGUMENT;
}

extern "C" int search_free(void* ctx)
{
    int structure = *(const int*)ctx;
    if(structure == SEARCH_TRIGRID3) {
        trigrid_destroy((trigrid*)ctx);
        delete (trigrid*)ctx;
        return WCORE_OK;
    }

    return WCORE_INVALID_ARGUMENT;
}

extern "C" int search_query(const void* ctx, int kind, const float* orig, const float* dir, int n, int* hit, void* info)
{
    if(!ctx)
        return WCORE_INVALID_ARGUMENT;

      int structure = *(const int*)ctx;
    if(structure == SEARCH_TRIGRID3) {
        query_info qi { .g = (const trigrid*)ctx, .hit = hit, .info = info };

        switch(kind & 0xf) {
        case SEARCH_NN:
            foreach_row<float, 3, query_info&>(orig, n, qi,
                [](const float* o, int i, query_info& qi) {
                    qi.hit[i] = trigrid_nn(qi.g, o, 1e10f, (float*)qi.info + 4 * i);
                });
            return WCORE_OK;

        case SEARCH_RAYCAST_T:
            foreach_row2<float, 3, query_info&>(orig, dir, n, qi,
                [](const float* o, const float* d, int i, query_info& qi) {
                    qi.hit[i] = trigrid_raycast_new<RayTri_T>(qi.g, o, d, ((float*)qi.info) + i);
                });
            return WCORE_OK;

        case SEARCH_RAYCAST_T | SEARCH_SOURCE_IS_AOS:
            for (int i = 0; i < n; i++)
            {
                qi.hit[i] = trigrid_raycast_new<RayTri_T>(qi.g, orig + 3 * i, dir + 3 * i, ((float*)qi.info) + i);
            }
            return WCORE_OK;

        case SEARCH_RAYCAST_TBARY:
            foreach_row2<float, 3, query_info&>(orig, dir, n, qi,
                [](const float* o, const float* d, int i, query_info& qi) {
                    qi.hit[i] = trigrid_raycast_new<RayTri_TBary>(qi.g, o, d, ((float*)qi.info) + 4 * i);
                });
            return WCORE_OK;

        default:
            return WCORE_INVALID_ARGUMENT;
        }
    }

    return WCORE_INVALID_ARGUMENT;
}

extern "C" int search_direct(int kind, const float* orig, const float* dir, const float* vert, int n)
{
    float t = FLT_MAX;

    switch(kind) {
    case SEARCHD_NN_PCL_3:
        return nearest<3>(vert, n, orig);

    case SEARCHD_RAYCAST_TRISOUP_3:
        return raytri<RayTri_T>(orig, dir, vert, n, n, &t);

    case SEARCHD_NN_TRISOUP_3:
        return pttri(orig, vert, n, n, nullptr);
    }

    return -1;
}