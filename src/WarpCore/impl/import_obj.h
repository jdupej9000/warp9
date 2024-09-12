#pragma once
#include <vector>


namespace warpcore::impl
{
    void* init_objload_ctx(int flags);
    void deinit_objload_ctx(void* ctx);
    int get_pos3fsoa_objload(void* ctx, void* data, int size);
    int get_idx3isoa_objload(void* ctx, void* data, int size);
    int add_data_objload(void* ctx, const char* data, int size);
}; 