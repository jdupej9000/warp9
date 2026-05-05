#include "../config.h"
#include "../p3f.h"
#include "search_impl.h"
#include "tri_grid.h"

namespace warpcore::impl
{
    struct _nncell
    {
        _nncell(void) {}

        _nncell(const trigrid* g) :
            cx0(0), cy0(0), cz0(0), depth(0), _dummy(0)
        {
            cx1 = g->ncell[0];
            cy1 = g->ncell[1];
            cz1 = g->ncell[2];
        }

        int cx0, cx1, cy0, cy1, cz0, cz1;
        int depth, _dummy;

        bool is_leaf(void) const noexcept
        {
            return (cx0 + 1) == cx1 && (cy0 + 1) == cy1 && (cz0 + 1) == cz1;
        }

        bool is_degenerate(void) const noexcept
        {
            return (cx0 >= cx1) || (cy0 >= cy1) || (cz0 >= cz1);
        }

        p3f c0(void) const noexcept { return p3i_to_p3f(p3i_set(cx0, cy0, cz0)); }
        p3f c1(void) const noexcept { return p3i_to_p3f(p3i_set(cx1, cy1, cz1)); }

        inline void split_common(_nncell& left, _nncell& right) const noexcept
        {
            left.depth = depth + 1;
            right.depth = depth + 1;
        }

        void split_x(_nncell& left, _nncell& right) const noexcept
        {
            split_common(left, right);
            int half = (cx0 + cx1) >> 1;
            left.cx0 = cx0; left.cx1 = half;
            left.cy0 = cy0; left.cy1 = cy1;
            left.cz0 = cz0; left.cz1 = cz1;
            right.cx0 = half; right.cx1 = cx1;
            right.cy0 = cy0; right.cy1 = cy1;
            right.cz0 = cz0; right.cz1 = cz1;
        }

        void split_y(_nncell& left, _nncell& right) const noexcept
        {
            split_common(left, right);
            int half = (cy0 + cy1) >> 1;
            left.cx0 = cx0; left.cx1 = cx1;
            left.cy0 = cy0; left.cy1 = half;
            left.cz0 = cz0; left.cz1 = cz1;
            right.cx0 = cx0; right.cx1 = cx1;
            right.cy0 = half; right.cy1 = cy1;
            right.cz0 = cz0; right.cz1 = cz1;
        }

        void split_z(_nncell& left, _nncell& right) const noexcept
        {
            split_common(left, right);
            int half = (cz0 + cz1) >> 1;
            left.cx0 = cx0; left.cx1 = cx1;
            left.cy0 = cy0; left.cy1 = cy1;
            left.cz0 = cz0; left.cz1 = half;
            right.cx0 = cx0; right.cx1 = cx1;
            right.cy0 = cy0; right.cy1 = cy1;
            right.cz0 = half; right.cz1 = cz1;
        }
    };

    struct _nntask
    {
        _nntask(const trigrid* g, const float* p, float clamp, float* proj) :
            grid(g), bestDist(clamp), result(proj), bestIdx(-1)
        {
            pt = p3f_set(p);
            g0 = p3f_set(g->x0);
            gd = p3f_set(p3f_recip(p3f_set(g->dx))); // outer p3f_set is to zero the .w element, which would be Inf otherwise
        }
        const trigrid* grid;
        float* result;
        float bestDist;
        int bestIdx;
        p3f pt, g0, gd;        
    };

    template<typename TPtTriTraits>
    void nn_inner(_nntask& task, const _nncell& ctx)
    {
        if (ctx.is_degenerate())
            return;

        p3f box0 = p3f_fma(task.gd, ctx.c0(), task.g0);
        p3f box1 = p3f_fma(task.gd, ctx.c1(), task.g0);
        p3f closest_aabb = p3f_proj_to_aabb(task.pt, box0, box1);

        if (p3f_dist(task.pt, closest_aabb) > task.bestDist)
            return;

        if (ctx.is_leaf()) {
            int cell_idx = ctx.cx0 +
                task.grid->ncell[0] * ctx.cy0 +
                task.grid->ncell[0] * task.grid->ncell[1] * ctx.cz0;

            const trigrid_cell* cell = task.grid->cells + cell_idx;

            if (cell->n > 0) {
                float hitDist = FLT_MAX;
                alignas(32) float cellResult[TPtTriTraits::ResultSize];
                const int hitIdx = pttri<TPtTriTraits>(task.pt, cell->vert, cell->n, cell->n, cellResult, &hitDist);

                if (hitDist < task.bestDist) {
                    task.bestDist = hitDist;
                    task.bestIdx = hitIdx;
                    memcpy(task.result, cellResult, sizeof(float) * TPtTriTraits::ResultSize);
                }
            }
        } else {
            int axis = ctx.depth % 3;
            _nncell left, right;
            switch (axis) {
            case 0: ctx.split_x(left, right); break;
            case 1: ctx.split_y(left, right); break;
            case 2: ctx.split_z(left, right); break;
            }

            p3f half_diff = p3f_sub(task.pt, p3f_fma(task.gd, left.c1(), task.g0));
            alignas(32) float half_diff_arr[4];
            p3f_store(half_diff_arr, half_diff);

            if (half_diff_arr[axis] <= 0) {
                nn_inner<TPtTriTraits>(task, left);
                nn_inner<TPtTriTraits>(task, right);
            } else {
                nn_inner<TPtTriTraits>(task, right);
                nn_inner<TPtTriTraits>(task, left);
            }
        }
    }

    template<typename TPtTriTraits>
    int trigrid_nn(const trigrid* grid, const float* pt, float clamp, float* proj)
    {
        _nntask task{ grid, pt, clamp, proj };
        _nncell cell{ grid };

        nn_inner<TPtTriTraits>(task, cell);
        return task.bestIdx;
    }
};