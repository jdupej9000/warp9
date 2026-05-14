#pragma once

namespace warpcore::impl
{
    // Cells contain unshared vertex data in AoSoA ordering. The stride is set to 
    // one AVX register worth: ceil(N/8) * {8*x0, 8*y0,...8*z2}. Primitive indices
    // map to the AoSoA blocks. The pointers vert, idx point to locations in
    // trigrid::buff_vert, buff_idx and are aligned to AVX register size (32B). If
    // the number of vertices in a cell (trigrid_cell::n) is not divisible by 8, the
    // upper parts of each register in an AoSoA block must be masked away when read.
    // trigrid_cell::nalign is merely trigrid_cell::n rounded up to multiples of 8.
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
    const trigrid_cell* get_trigrid_cell(const trigrid* g, int x, int y, int z) noexcept;
    int get_trigrid_cell_idx(const trigrid* g, int x, int y, int z) noexcept;
    bool is_cell_in_grid(const trigrid* g, int x, int y, int z) noexcept;
};