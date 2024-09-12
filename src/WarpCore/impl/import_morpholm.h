#pragma once

#include "config.h"

namespace warpcore::impl
{
    void* init_morpholmload_ctx(int flags);
    void deinit_morpholmload_ctx(void* ctx);
    int add_data_morpholmload(void* ctx, const char* data, int size);
    int get_pos3fsoa_morpholmload(void* ctx, void* data, int size);
};