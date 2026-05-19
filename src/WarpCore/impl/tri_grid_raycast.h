#include "../config.h"
#include "../p3f.h"
#include "search_impl.h"
#include "tri_grid.h"

namespace warpcore::impl
{
    // Traverse a regular grid of dimensions dim along the ray p0,p1. These coordinates are
    // in grid index dimensions. 3D-DDA algorithm is used. For each visited cell, fun is called.
    // If fun returns false at any cell, the traversal is stopped and this call returns true.
    // If fun never signals termination by returning false, false is returned here.
    template<typename TCtx>
    __declspec(noalias) bool WCORE_VECCALL traverse_3ddda(p3f p0, p3f p1, p3i dim, TCtx ctx, bool (*fun)(p3i pt, TCtx ctx))
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

        for (;;) {
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
    __declspec(noalias) int trigrid_raycast(const trigrid* grid, const float* orig, const float* dir, float* t)
    {
        using namespace warpcore;

        struct raycast_ctx
        {
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

        raycast_ctx ctx{ .g = grid, .o = o, .d = d, .idx = -1, .t = t, .ntested = 0, .toffs = aabbt0 };

        traverse_3ddda<raycast_ctx&>(
            p3f_mul(p3f_sub(o, grid0), gridd),
            p3f_mul(p3f_sub(e, grid0), gridd),
            p3i_set(grid->ncell),
            ctx,
            [](p3i c, raycast_ctx& ctx) __declspec(noalias) -> bool {
                alignas(16) int ci[4];
                _mm_store_si128((__m128i*)ci, c);             
                const trigrid_cell* cell = get_trigrid_cell(ctx.g, ci[0], ci[1], ci[2]);
                                
                _mm_prefetch((const char*)cell->vert, _MM_HINT_T0);

                int ne = cell->n;
                if (ne == 0)
                    return true; // continue along the ray, this cell is just empty

                //ctx.ntested += ne;

                int collision = raytri<TRayTriTraits>(ctx.o, ctx.d, cell->vert, ne, ctx.t);
                if (collision >= 0) {
                    ctx.idx = cell->idx[collision];
                    ctx.t[0] += ctx.toffs;
                    return false; // intersection found, stop searching
                }

                return true;
            });

        return ctx.idx;
    }
};