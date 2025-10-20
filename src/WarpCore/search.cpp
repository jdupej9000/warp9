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
    const search_query_config* cfg;
    int* hit;
    void* info;
    bool invert_dir;
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

extern "C" int search_info(const void* ctx, int kind, int param, void* res, int ressize)
{
    if (!ctx)
        return 0;

    int ret = 0;

    int structure = *(const int*)ctx;
    if (structure == SEARCH_TRIGRID3) {
        const trigrid* grid = (const trigrid*)ctx;

        switch (kind) {
        case SEARCHINFO_AABB:
            ret = 24;
            if (ressize >= ret) {
                memcpy(res, grid->x0, sizeof(float) * 3);
                memcpy((uint8_t*)res + 12, grid->x1, sizeof(float) * 3);
            }
            break;
        }
    }

    return ret;
}

extern "C" int search_query(const void* ctx, int kind, search_query_config* cfg, const float* orig, const float* dir, int n, int* hit, void* info)
{
    if(!ctx)
        return WCORE_INVALID_ARGUMENT;

    int structure = *(const int*)ctx;
    if(structure == SEARCH_TRIGRID3) {
        query_info qi { 
            .g = (const trigrid*)ctx, 
            .cfg = cfg, 
            .hit = hit, 
            .info = info, 
            .invert_dir = (kind & SEARCH_INVERT_DIRECTION) != 0 
        };

        switch(kind & ~SEARCH_INVERT_DIRECTION) {
        case SEARCH_NN_DPTBARY:
            for (int i = 0; i < n; i++) {
                qi.hit[i] = trigrid_nn<PtTri_DPtBary>(qi.g, orig + 3 * i, cfg->max_dist, (float*)qi.info + PtTri_DPtBary::ResultSize * i);
            }
            return WCORE_OK;

        case SEARCH_RAYCAST_T:
            if (qi.invert_dir) {
                for (int i = 0; i < n; i++) {
                    float dd[3]{ -dir[3 * i], -dir[3 * i + 1], -dir[3 * i + 2] };
                    qi.hit[i] = trigrid_raycast<RayTri_T>(qi.g, orig + 3 * i, dd, ((float*)qi.info) + i);
                }
            } else {
                for (int i = 0; i < n; i++)
                    qi.hit[i] = trigrid_raycast<RayTri_T>(qi.g, orig + 3 * i, dir + 3 * i, ((float*)qi.info) + i);
            }
            return WCORE_OK;

        case SEARCH_RAYCAST_TBARY:           
            foreach_row2<float, 3, query_info&>(orig, dir, n, qi,
                [](const float* o, const float* d, int i, query_info& qi) {
                    if (qi.invert_dir) {
                        alignas(16) float dd[3]{ -d[0], -d[1], -d[2] };
                        qi.hit[i] = trigrid_raycast<RayTri_TBary>(qi.g, o, dd, ((float*)qi.info) + 4 * i);
                    } else {
                        qi.hit[i] = trigrid_raycast<RayTri_TBary>(qi.g, o, d, ((float*)qi.info) + 4 * i);
                    }                      
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
        return pttri<PtTri_Blank>(orig, vert, n, n, nullptr, &t);
    }

    return -1;
}