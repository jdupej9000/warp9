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
    constexpr int VectorSize = 8;

    void make_cellidx_ranges_aosoa(const trigrid* grid, const float* vert, const int* idx, int nv, int nt, int* range);
    void make_cell_histogram(const trigrid* grid, const int* idx_range, int nt, int* hist);
    void populate_index_arrays(trigrid* grid, const int* idx_range, int nt, int* counter);
    void populate_grid_cell(float* dest, const float* vert, const int* idx, const int* face, int nvcell, int nv, int nt);

    void trigrid_build(trigrid* grid, const float* vert, const int* idx, int nv, int nt, int k)
    {
        grid->ncell[0] = k;
        grid->ncell[1] = k;
        grid->ncell[2] = k;
        grid->nt = nt;

        const int num_cells = grid->ncell[0] * grid->ncell[1] * grid->ncell[2];
        grid->cells = new trigrid_cell[num_cells];

        for (int i = 0; i < 4; i++) {
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

        int* idx_range = new int[6 * nt + 6 * VectorSize];
        memset(idx_range, 0, sizeof(int) * (6 * nt + 6 * VectorSize));
        make_cellidx_ranges_aosoa(grid, vert, idx, nv, nt, idx_range);
             
        int* hist = new int[num_cells];
        make_cell_histogram(grid, idx_range, nt, hist);
        const int ng = round_down(reduce_add_i32(hist, num_cells), VectorSize) + VectorSize * k * k * k;

        grid->buff_vert = (float*)_aligned_malloc(ng * 9 * sizeof(float), VectorSize * sizeof(float));
        grid->buff_idx = (int*)_aligned_malloc(ng * sizeof(int), VectorSize * sizeof(float));
 
        float* vert_base = grid->buff_vert;
        int* idx_base = grid->buff_idx;
        int offs = 0;
        for(int i = 0; i < num_cells; i++) {
            int na = round_down(hist[i], VectorSize) + VectorSize;
            grid->cells[i].n = hist[i];
            grid->cells[i].nalign = na;
            grid->cells[i].vert = vert_base + 9 * offs;
            grid->cells[i].idx = idx_base + offs;
            offs += na;
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

        _aligned_free(grid->buff_vert);
        _aligned_free(grid->buff_idx);

        grid->buff_vert = nullptr;
        grid->buff_idx = nullptr;
    }

    void populate_index_arrays(trigrid* grid, const int* idx_range, int nt, int* counter)
    {
        // This weird loop is to accomodate the AoSoA structure of idx_range. That is organized
        // in chunks with: 8 minX, 8 minY, .. 8 maxZ

        for(int i = 0; i < nt; i += VectorSize) {
            const int* idx_range_chunk = idx_range + 6 * i;

            int j8 = std::min(VectorSize, nt - i);
            for(int j = 0; j < j8; j++) {
                int x0 = idx_range_chunk[0 * VectorSize + j];
                int x1 = idx_range_chunk[1 * VectorSize + j];
                int y0 = idx_range_chunk[2 * VectorSize + j];
                int y1 = idx_range_chunk[3 * VectorSize + j];
                int z0 = idx_range_chunk[4 * VectorSize + j];
                int z1 = idx_range_chunk[5 * VectorSize + j];
    
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

            int j8 = std::min(VectorSize, nt - i);
            for(int j = 0; j < j8; j++) {
                int x0 = idx_range_chunk[0 * VectorSize + j];
                int x1 = idx_range_chunk[1 * VectorSize + j];
                int y0 = idx_range_chunk[2 * VectorSize + j];
                int y1 = idx_range_chunk[3 * VectorSize + j];
                int z0 = idx_range_chunk[4 * VectorSize + j];
                int z1 = idx_range_chunk[5 * VectorSize + j];

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
            const int fidx = 3 * face[i];

            int i8 = i / VectorSize;
            int ii = i & (VectorSize - 1);
            float* dest_blk = dest + i8 * 9 * VectorSize;

            const int idx0 = idx[fidx] * 3;            
            dest_blk[ii + 0 * VectorSize] = vert[idx0 + 0];
            dest_blk[ii + 1 * VectorSize] = vert[idx0 + 1];
            dest_blk[ii + 2 * VectorSize] = vert[idx0 + 2];

            const int idx1 = idx[fidx + 1] * 3;
            dest_blk[ii + 3 * VectorSize] = vert[idx1 + 0];
            dest_blk[ii + 4 * VectorSize] = vert[idx1 + 1];
            dest_blk[ii + 5 * VectorSize] = vert[idx1 + 2];

            const int idx2 = idx[fidx + 2] * 3;
            dest_blk[ii + 6 * VectorSize] = vert[idx2 + 0];
            dest_blk[ii + 7 * VectorSize] = vert[idx2 + 1];
            dest_blk[ii + 8 * VectorSize] = vert[idx2 + 2];
        }
    }

    inline static void load_safe(const int* idx, int n, __m256i& a, __m256i& b, __m256i& c)
    {
        if (n >= 3 * VectorSize) {
            a = _mm256_loadu_si256((const __m256i*)idx);
            b = _mm256_loadu_si256((const __m256i*)(idx + VectorSize));
            c = _mm256_loadu_si256((const __m256i*)(idx + 2 * VectorSize));
            return;
        }

        if (n >= VectorSize) {
            a = _mm256_loadu_si256((const __m256i*)idx);

            if (n == VectorSize)
                return;
        } else {
            const __m256i mask = _mm256_cmpgt_epi32(_mm256_set1_epi32(n), _mm256_setr_epi32(0, 1, 2, 3, 4, 5, 6, 7));
            a = _mm256_maskload_epi32(idx, mask);
            return;
        }

        if (n >= 2 * VectorSize) {
            b = _mm256_loadu_si256((const __m256i*)(idx + VectorSize));

            if (n == 2 * VectorSize)
                return;
        } else {
            const __m256i mask = _mm256_cmpgt_epi32(_mm256_set1_epi32(n - VectorSize), _mm256_setr_epi32(0, 1, 2, 3, 4, 5, 6, 7));
            b = _mm256_maskload_epi32(idx + VectorSize, mask);
            return;
        }

        const __m256i mask = _mm256_cmpgt_epi32(_mm256_set1_epi32(n - 2 * VectorSize), _mm256_setr_epi32(0, 1, 2, 3, 4, 5, 6, 7));
        c = _mm256_maskload_epi32(idx + 2 * VectorSize, mask);
    }
    
    void make_cellidx_ranges_aosoa(const trigrid* grid, const float* vert, const int* idx, int nv, int nt, int* range)
    {
        constexpr int ROUND_FLOOR = _MM_FROUND_TO_NEG_INF | _MM_FROUND_NO_EXC;
        constexpr int ROUND_CEIL = _MM_FROUND_TO_NEG_INF | _MM_FROUND_NO_EXC;
        const __m256i seq = _mm256_setr_epi32(0, 1, 2, 3, 4, 5, 6, 7);
        const __m256i three = _mm256_set1_epi32(3);

        for(int i = 0; i < nt; i += VectorSize) {
            const __m256i mask = _mm256_cmpgt_epi32(_mm256_set1_epi32(nt - i), seq);

            __m256i idx0, idx1, idx2;
            load_safe(idx + 3 * i, 3 * (nt - i), idx0, idx1, idx2);
            demux(idx0, idx1, idx2);
            idx0 = _mm256_mullo_epi32(idx0, three);
            idx1 = _mm256_mullo_epi32(idx1, three);
            idx2 = _mm256_mullo_epi32(idx2, three);

            __m256i idxi = _mm256_and_si256(idx0, mask); // mask out out-of-range elements (only in the last pass)
            __m256 x = _mm256_i32gather_ps(vert, idxi, 4);
            __m256 xmin = x, xmax = x;
            __m256 y = _mm256_i32gather_ps(vert + 1, idxi, 4);
            __m256 ymin = y, ymax = y;
            __m256 z = _mm256_i32gather_ps(vert + 2, idxi, 4);
            __m256 zmin = z, zmax = z;

            idxi = _mm256_and_si256(idx1, mask);
            x = _mm256_i32gather_ps(vert, idxi, 4);
            xmin = _mm256_min_ps(xmin, x);
            xmax = _mm256_max_ps(xmax, x);
            y = _mm256_i32gather_ps(vert + 1, idxi, 4);
            ymin = _mm256_min_ps(ymin, y);
            ymax = _mm256_max_ps(ymax, y);
            z = _mm256_i32gather_ps(vert + 2, idxi, 4);
            zmin = _mm256_min_ps(zmin, z);
            zmax = _mm256_max_ps(zmax, z);

            idxi = _mm256_and_si256(idx2, mask);
            x = _mm256_i32gather_ps(vert, idxi, 4);
            xmin = _mm256_min_ps(xmin, x);
            xmax = _mm256_max_ps(xmax, x);
            y = _mm256_i32gather_ps(vert + 1, idxi, 4);
            ymin = _mm256_min_ps(ymin, y);
            ymax = _mm256_max_ps(ymax, y);
            z = _mm256_i32gather_ps(vert + 2, idxi, 4);
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
