#include "tri_grid.h"
#include "pcl_utils.h"
#include "vec_math.h"
#include "search_impl.h"
#include "utils.h"
#include <immintrin.h>
#include <cmath>

#include <iostream>

using namespace warpcore;

namespace warpcore::impl
{
    void make_cellidx_ranges_aosoa(const trigrid* grid, const float* vert, const int* idx, int nv, int nt, int* range);
    void make_cell_histogram(const trigrid* grid, const int* idx_range, int nt, int* hist);
    void populate_index_arrays(trigrid* grid, const int* idx_range, int nt, int* counter);
    void populate_grid_cell(float* dest, const float* vert, const int* idx, const int* face, int nvcell, int nv, int nt);

    void trigrid_build(trigrid* grid, const float* vert, const int* idx, int nv, int nt, int k)
    {
        grid->ncell[0] = k;
        grid->ncell[1] = k;
        grid->ncell[2] = k;

        const int num_cells = grid->ncell[0] * grid->ncell[1] * grid->ncell[2];
        grid->cells = new trigrid_cell[num_cells];

        for (int i = 0; i < 4; i++)
        {
            grid->x0[i] = FLT_MAX;
            grid->x1[i] = FLT_MIN;
        }

        pcl_aabb(vert, 3, nv, grid->x0, grid->x1);
        for(int i = 0; i < 3; i++) {
            // Inflate the AABB slightly.
            float eps = (grid->x1[i] - grid->x0[i]) * 0.001f;
            grid->x0[i] -= eps;
            grid->x1[i] += eps;
            grid->dx[i] = (float)(grid->ncell[i]) / (grid->x1[i] - grid->x0[i]);
        }

        grid->x0[3] = grid->x1[3] = grid->dx[3] = 0;

        int* idx_range = new int[6 * nt + 6 * 8];
        memset(idx_range, 0, sizeof(int) * (6 * nt + 6 * 8));
        make_cellidx_ranges_aosoa(grid, vert, idx, nv, nt, idx_range);

        int* hist = new int[num_cells];
        make_cell_histogram(grid, idx_range, nt, hist);
        const int ng = reduce_add_i32(hist, num_cells);

        grid->buff_vert.resize(ng * 9);
        grid->buff_idx.resize(ng);
        float* vert_base = grid->buff_vert.data();
        int* idx_base = grid->buff_idx.data();
        int offs = 0;
        for(int i = 0; i < num_cells; i++) {
            grid->cells[i].n = hist[i];
            grid->cells[i].vert = vert_base + 9 * offs;
            grid->cells[i].idx = idx_base + offs;
            offs += hist[i];
        }

        memset(hist, 0, sizeof(int) * num_cells);
        populate_index_arrays(grid, idx_range, nt, hist);

        for(int i = 0; i < num_cells; i++)
            populate_grid_cell(grid->cells[i].vert, vert, idx, grid->cells[i].idx, grid->cells[i].n, nv, nt);

        delete[] hist;
        delete[] idx_range;
    }

    void trigrid_destroy(trigrid* grid)
    {
        if(grid->cells) {
            delete[] grid->cells;
            grid->cells = nullptr;
        }

       grid->buff_vert.clear();
       grid->buff_idx.clear();
    }

    void populate_index_arrays(trigrid* grid, const int* idx_range, int nt, int* counter)
    {
        // This weird loop is to accomodate the AoSoA structure of idx_range. That is organized
        // in chunks with: 8 minX, 8 minY, .. 8 maxZ

        for(int i = 0; i < nt; i+=8) {
            const int* idx_range_chunk = idx_range + 6 * i;

            int j8 = std::min(8, nt - i);
            for(int j = 0; j < j8; j++) {
                int x0 = idx_range_chunk[0 * 8 + j];
                int x1 = idx_range_chunk[1 * 8 + j];
                int y0 = idx_range_chunk[2 * 8 + j];
                int y1 = idx_range_chunk[3 * 8 + j];
                int z0 = idx_range_chunk[4 * 8 + j];
                int z1 = idx_range_chunk[5 * 8 + j];
    
                for (int z = z0; z <= z1; z++)
                for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++) {
                    const int idx = get_trigrid_cell_idx(grid, x, y, z);
                    grid->cells[idx].idx[counter[idx]] = i + j;
                    counter[idx]++;
                }
            }
        }
    }

    void make_cell_histogram(const trigrid* grid, const int* idx_range, int nt, int* hist) 
    {
        memset(hist, 0, sizeof(int) * grid->ncell[0] * grid->ncell[1] * grid->ncell[2]);

        for(int i = 0; i < nt; i+=8) {
            const int* idx_range_chunk = idx_range + 6 * i;

            int j8 = std::min(8, nt - i);
            for(int j = 0; j < j8; j++) {
                int x0 = idx_range_chunk[0 * 8 + j];
                int x1 = idx_range_chunk[1 * 8 + j];
                int y0 = idx_range_chunk[2 * 8 + j];
                int y1 = idx_range_chunk[3 * 8 + j];
                int z0 = idx_range_chunk[4 * 8 + j];
                int z1 = idx_range_chunk[5 * 8 + j];

                for (int z = z0; z <= z1; z++)
                for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++) {
                    const int idx = get_trigrid_cell_idx(grid, x, y, z);
                    hist[idx] ++;
                }
            }
        } 
    }

    void populate_grid_cell(float* dest, const float* vert, const int* idx, const int* face, int nvcell, int nv, int nt)
    {
        for(int i = 0; i < nvcell; i++) {
            const int fidx = face[i];

            const int idx0 = idx[fidx];
            dest[i + 0 * nvcell] = vert[idx0 + 0 * nv];
            dest[i + 1 * nvcell] = vert[idx0 + 1 * nv];
            dest[i + 2 * nvcell] = vert[idx0 + 2 * nv];

            const int idx1 = idx[fidx + nt];
            dest[i + 3 * nvcell] = vert[idx1 + 0 * nv];
            dest[i + 4 * nvcell] = vert[idx1 + 1 * nv];
            dest[i + 5 * nvcell] = vert[idx1 + 2 * nv];

            const int idx2 = idx[fidx + 2 * nt];
            dest[i + 6 * nvcell] = vert[idx2 + 0 * nv];
            dest[i + 7 * nvcell] = vert[idx2 + 1 * nv];
            dest[i + 8 * nvcell] = vert[idx2 + 2 * nv];
        }
    }

    void make_cellidx_ranges_aosoa(const trigrid* grid, const float* vert, const int* idx, int nv, int nt, int* range)
    {
        constexpr int ROUND_FLOOR = _MM_FROUND_TO_NEG_INF | _MM_FROUND_NO_EXC;
        constexpr int ROUND_CEIL = _MM_FROUND_TO_NEG_INF | _MM_FROUND_NO_EXC;
        const __m256i seq = _mm256_setr_epi32(0, 1, 2, 3, 4, 5, 6, 7);

        for(int i = 0; i < nt; i+=8) {
            const __m256i mask = _mm256_cmpgt_epi32(_mm256_set1_epi32(nt - i), seq);

            __m256i idxi = _mm256_loadu_si256((const __m256i*)(idx + i));
            idxi = _mm256_and_si256(idxi, mask); // mask out out-of-range elements (only in the last pass)
            __m256 x = _mm256_i32gather_ps(vert, idxi, 4);
            __m256 xmin = x, xmax = x;
            __m256 y = _mm256_i32gather_ps(vert + nv, idxi, 4);
            __m256 ymin = y, ymax = y;
            __m256 z = _mm256_i32gather_ps(vert + 2 * nv, idxi, 4);
            __m256 zmin = z, zmax = z;

            idxi = _mm256_loadu_si256((const __m256i*)(idx + i + nt));
            idxi = _mm256_and_si256(idxi, mask);
            x = _mm256_i32gather_ps(vert, idxi, 4);
            xmin = _mm256_min_ps(xmin, x);
            xmax = _mm256_max_ps(xmax, x);
            y = _mm256_i32gather_ps(vert + nv, idxi, 4);
            ymin = _mm256_min_ps(ymin, y);
            ymax = _mm256_max_ps(ymax, y);
            z = _mm256_i32gather_ps(vert + 2 * nv, idxi, 4);
            zmin = _mm256_min_ps(zmin, z);
            zmax = _mm256_max_ps(zmax, z);

            idxi = _mm256_loadu_si256((const __m256i*)(idx + i + 2 * nt));
            idxi = _mm256_and_si256(idxi, mask);
            x = _mm256_i32gather_ps(vert, idxi, 4);
            xmin = _mm256_min_ps(xmin, x);
            xmax = _mm256_max_ps(xmax, x);
            y = _mm256_i32gather_ps(vert + nv, idxi, 4);
            ymin = _mm256_min_ps(ymin, y);
            ymax = _mm256_max_ps(ymax, y);
            z = _mm256_i32gather_ps(vert + 2 * nv, idxi, 4);
            zmin = _mm256_min_ps(zmin, z);
            zmax = _mm256_max_ps(zmax, z);

            __m256 v0 = _mm256_broadcast_ss(grid->x0);
            __m256 dv = _mm256_broadcast_ss(grid->dx);
            __m256i xminidx = _mm256_cvtps_epi32(_mm256_round_ps(
                _mm256_mul_ps(_mm256_sub_ps(xmin, v0), dv), ROUND_FLOOR));
            __m256i xmaxidx = _mm256_cvtps_epi32(_mm256_round_ps(
                _mm256_mul_ps(_mm256_sub_ps(xmax, v0), dv), ROUND_CEIL));

            v0 = _mm256_broadcast_ss(grid->x0 + 1);
            dv = _mm256_broadcast_ss(grid->dx + 1);
            __m256i yminidx = _mm256_cvtps_epi32(_mm256_round_ps(
                _mm256_mul_ps(_mm256_sub_ps(ymin, v0), dv), ROUND_FLOOR));
            __m256i ymaxidx = _mm256_cvtps_epi32(_mm256_round_ps(
                _mm256_mul_ps(_mm256_sub_ps(ymax, v0), dv), ROUND_CEIL));

            v0 = _mm256_broadcast_ss(grid->x0 + 2);
            dv = _mm256_broadcast_ss(grid->dx + 2);
            __m256i zminidx = _mm256_cvtps_epi32(_mm256_round_ps(
                _mm256_mul_ps(_mm256_sub_ps(zmin, v0), dv), ROUND_FLOOR));
            __m256i zmaxidx = _mm256_cvtps_epi32(_mm256_round_ps(
                _mm256_mul_ps(_mm256_sub_ps(zmax, v0), dv), ROUND_CEIL));

            __m256i* chunk = (__m256i*)(range + 6 * i);
            _mm256_storeu_si256(chunk, xminidx);
            _mm256_storeu_si256(chunk + 1, xmaxidx);
            _mm256_storeu_si256(chunk + 2, yminidx);
            _mm256_storeu_si256(chunk + 3, ymaxidx);
            _mm256_storeu_si256(chunk + 4, zminidx);
            _mm256_storeu_si256(chunk + 5, zmaxidx);
        }
    }


    const trigrid_cell* get_trigrid_cell(const trigrid* g, int x, int y, int z) noexcept
    {
        return g->cells + get_trigrid_cell_idx(g, x, y, z);
    }

    int get_trigrid_cell_idx(const trigrid* g, int x, int y, int z) noexcept
    {
        return x + 
            g->ncell[0] * y + 
            g->ncell[0] * g->ncell[1] * z;
    }

    bool is_cell_in_grid(const trigrid* g, int x, int y, int z) noexcept
    {
        return x >= 0 && y >= 0 && z >= 0 && 
            x < g->ncell[0] && y < g->ncell[1] && z < g->ncell[2];
    }

};
