#pragma once

#include "../config.h"
#include "../p3f.h"
#include "search_impl.h"
#include <vector>

#include <iostream>

namespace warpcore::impl
{
    struct trigrid_cell {
        int n;
        float* vert;
        int* idx; 
    };

    struct trigrid {
        int __magic;
        
        float x0[4];
        float x1[4];
        float dx[4];
        int ncell[4];

        trigrid_cell* cells;
            
        std::vector<float> buff_vert;
        std::vector<int> buff_idx;
    };


    void trigrid_build(trigrid* grid, const float* vert, const int* idx, int nv, int nt, int k);
    void trigrid_destroy(trigrid* grid);
    int trigrid_nn(const trigrid* grid, const float* pt, float clamp, float* proj);
    const trigrid_cell* get_trigrid_cell(const trigrid* g, int x, int y, int z) noexcept;
    int get_trigrid_cell_idx(const trigrid* g, int x, int y, int z) noexcept;
    bool is_cell_in_grid(const trigrid* g, int x, int y, int z) noexcept;

    template<typename TCtx>
    bool traverse_3ddda(p3f p0, p3f p1, p3i dim, TCtx ctx, bool (*fun)(p3i pt, TCtx ctx))
    {
        p3i dimm1 = _mm_sub_epi32(dim, p3i_set(1));
        p3f d = p3f_sub(p1, p0);
        p3f dzero = p3f_mask_is_almost_zero(d);
        p3i step = p3f_isign(d);
        p3f fstep = p3i_to_p3f(step);
       
        // TODO: prevent divisions by (almost) zero
        p3f delta = p3f_switch(p3f_div(fstep, d), p3f_set(1e10f), dzero);
        
        p3f frac0 = p3f_sub(p0, p3f_floor(p0));
        p3f frac1 = p3f_add(p3f_sub(p3f_set(1.0f), p0), p3f_floor(p0));
        p3f tmax = p3f_switch(frac1, frac0, _mm_castsi128_ps(step)); // step == -1 => frac0, else => frac0
        tmax = p3f_mul(tmax, delta);

        p3i cur = p3f_to_p3i(p0);
        if (!fun(p3i_clamp(cur, _mm_setzero_si128(), dimm1), ctx))
            return true;

        for (;;)
        {
            p3f min_mask = p3f_min_mask_full(tmax);
            if (p3i_is_zero(_mm_castps_si128(min_mask)))
                break;

            cur = p3i_add(cur, _mm_and_si128(step, _mm_castps_si128(min_mask)));
            tmax = p3f_add(tmax, _mm_and_ps(delta, min_mask));

            if (!p3i_in_aabb(cur, _mm_setzero_si128(), dim))
                break;

            if (!fun(cur, ctx))
                return true;
        }

        return false;
        
    }

    template<typename TRayTriTraits>
    int trigrid_raycast(const trigrid* grid, const float* orig, const float* dir, float* t)
    {
        using namespace warpcore;

        struct raycast_ctx {
            const trigrid* g;
            p3f o, d;
            int idx;
            float* t;
            int ntested;
            float toffs;
        };

        p3f o = p3f_set(orig);
        const p3f d = p3f_set(dir);
        const p3f grid0 = p3f_set(grid->x0);
        const p3f grid1 = p3f_set(grid->x1);
        const p3f gridd = p3f_set(grid->dx);

        float aabbt0 = 0.0f, aabbt1 = 0.0f;
        if (!intersect_ray_aabb(o, d, grid0, grid1, aabbt0, aabbt1))
            return -1; // no intersection

        p3f e = p3f_fma(aabbt1, d, o);
        if (aabbt0 > 0)
            o = p3f_fma(aabbt0, d, o);
 
        raycast_ctx ctx { .g = grid, .o = o, .d = d, .idx = -1, .t = t, .ntested = 0, .toffs = aabbt0 };

        traverse_3ddda<raycast_ctx&>(
            p3f_mul(p3f_sub(o, grid0), gridd), 
            p3f_mul(p3f_sub(e, grid0), gridd),
            p3i_set(grid->ncell),
            ctx,
            [](p3i c, raycast_ctx& ctx) -> bool {
                alignas(16) int cidx[4];
                _mm_store_si128((__m128i*)cidx, c);
                const trigrid_cell* cell = get_trigrid_cell(ctx.g, cidx[0], cidx[1], cidx[2]);
              
                int ne = cell->n;
                if(ne == 0) 
                    return true; // continue along the ray, this cell is just empty

                alignas(16) float orig[4], dir[4];
                _mm_store_ps(orig, ctx.o);
                _mm_store_ps(dir, ctx.d);

                ctx.ntested += ne;

                int collision = raytri<TRayTriTraits>(orig, dir, cell->vert, ne, ne, ctx.t);
                if(collision >= 0) {
                    ctx.idx = cell->idx[collision];
                    ctx.t[0] += ctx.toffs;
                    return false; // intersection found, stop searching
                }

                return true;
            });

        return ctx.idx;
    }

    template<typename TPtTriTraits>
    int trigrid_nn(const trigrid* grid, const float* pt, float clamp, float* proj)
    {
        struct trigrid_nn_ctx {
            const trigrid* grid;
            float* best;
            int* bestIdx;
            const float* pt;
            float* proj;
            int* coarseRadius;
            int cx, cy, cz;
        };

        const p3f p = p3f_set(pt);
        const p3f r = p3f_mul(p3f_set(grid->dx), p3f_sub(p, p3f_set(grid->x0)));

        int cx, cy, cz;
        p3f_to_int(r, cx, cy, cz);
        cx = std::min(std::max(0, cx), grid->ncell[0] - 1);
        cy = std::min(std::max(0, cy), grid->ncell[1] - 1);
        cz = std::min(std::max(0, cz), grid->ncell[2] - 1);

        float best = clamp * clamp;
        int bestIdx = -1;
        p3f crad = p3f_clamp(p3f_add(0.5f, p3f_mul(clamp, p3f_set(grid->dx))), 1.0f, grid->ncell[0]);
        alignas(16) int coarseRadius[4];
        _mm_store_si128((__m128i*)coarseRadius, p3f_to_p3i(crad));
      
        trigrid_nn_ctx ctx{
            .grid = grid,
            .best = &best,
            .bestIdx = &bestIdx,
            .pt = pt,
            .proj = proj,
            .coarseRadius = coarseRadius,
            .cx = cx,
            .cy = cy,
            .cz = cz
        };

        int initialRadius = std::max(std::max(coarseRadius[0], coarseRadius[1]), coarseRadius[2]);
        foreach_voxel_central<trigrid_nn_ctx&>(initialRadius, cx, cy, cz, grid->ncell[0], grid->ncell[1], grid->ncell[2], ctx,
            [](int dx, int dy, int dz, int dr, trigrid_nn_ctx& ctx) noexcept -> bool {

                if (dr > ctx.coarseRadius[0] && 
                    dr > ctx.coarseRadius[1] && 
                    dr > ctx.coarseRadius[2])
                    return false;

                // early out if the cell cannot possibly contain any closer triangles
                if (abs(dx - ctx.cx) > ctx.coarseRadius[0] || 
                    abs(dy - ctx.cy) > ctx.coarseRadius[1] || 
                    abs(dz - ctx.cz) > ctx.coarseRadius[2])
                    return true;

                // x,y,z are in range, guaranteed by foreach_voxel_central
                const int idx = dx + ctx.grid->ncell[0] * dy + ctx.grid->ncell[0] * ctx.grid->ncell[1] * dz;
                const trigrid_cell* cell = ctx.grid->cells + idx;

                if (cell->n > 0) {
                    float d2 = FLT_MAX;
                    alignas(32) float cellResult[TPtTriTraits::ResultSize];
                    const int hitIdx = pttri<TPtTriTraits>(ctx.pt, cell->vert, cell->n, cell->n, cellResult, &d2);

                    if (d2 < *ctx.best) {
                        *ctx.best = d2;
                        *ctx.bestIdx = cell->idx[hitIdx];
                        memcpy(ctx.proj, cellResult, sizeof(float) * TPtTriTraits::ResultSize);

                        float d = sqrtf(d2);
                        p3f crad = p3f_clamp(p3f_add(0.5f, p3f_mul(d, p3f_set(ctx.grid->dx))), 1.0f, ctx.grid->ncell[0]);
                        _mm_store_si128((__m128i*)ctx.coarseRadius, p3f_to_p3i(crad));
                    }
                }

                return true;
            });

        return bestIdx;
    }
};