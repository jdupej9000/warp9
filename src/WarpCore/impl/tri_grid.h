#pragma once

namespace warpcore::impl
{
    struct trigrid_cell {
        int n, nalign;
        float* vert;
        int* idx; 
    };

    struct trigrid {
        int __magic;
        int nt;
        
        float x0[4];
        float x1[4];
        float dx[4];
        int ncell[4];

        trigrid_cell* cells;

        float* buff_vert;
        int* buff_idx;
    };

    void trigrid_build(trigrid* grid, const float* vert, const int* idx, int nv, int nt, int k);
    void trigrid_destroy(trigrid* grid);
    int trigrid_nn(const trigrid* grid, const float* pt, float clamp, float* proj);
    const trigrid_cell* get_trigrid_cell(const trigrid* g, int x, int y, int z) noexcept;
    int get_trigrid_cell_idx(const trigrid* g, int x, int y, int z) noexcept;
    bool is_cell_in_grid(const trigrid* g, int x, int y, int z) noexcept;
};