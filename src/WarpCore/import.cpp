#include "warpcore.h"
#include "impl/import_obj.h"
#include "impl/import_morpholm.h"

extern "C" int load_init(int format, int flags, void** ctx)
{
    switch(format) {
    case WAVEFRONT_OBJ:
        *ctx = warpcore::impl::init_objload_ctx(flags);
        break;

    case MORPHO_LANDMARKS:
        *ctx = warpcore::impl::init_morpholmload_ctx(flags);
        break;

    default:
        return -1;
    }

    return 0;
}

extern "C" int load_push(void* ctx, const void* data, int size)
{
    int format = *(const int*)ctx;

    switch(format) {
    case WAVEFRONT_OBJ:
        return warpcore::impl::add_data_objload(ctx, (const char*)data, size);

    case MORPHO_LANDMARKS:
        return warpcore::impl::add_data_morpholmload(ctx, (const char*)data, size);

    default:
        return -1;
    }

    return 0;
}

extern "C" int load_get(void* ctx, int index, void* data, int size)
{
    int format = *(const int*)ctx;

    if(format == WAVEFRONT_OBJ) {

        switch(index) {
        case IMPORT_POS3F_SOA:
            return warpcore::impl::get_pos3fsoa_objload(ctx, data, size);

        case IMPORT_IDX3I_SOA:
            return warpcore::impl::get_idx3isoa_objload(ctx, data, size);  
        }

    } else if(format == MORPHO_LANDMARKS) {

      switch(index) {
      case IMPORT_POS3F_SOA:
            return warpcore::impl::get_pos3fsoa_morpholmload(ctx, data, size);
      }
    } 

    return -1;
}

extern "C" int load_free(void* ctx)
{
    int format = *(const int*)ctx;

    switch(format) {
    case WAVEFRONT_OBJ:
        warpcore::impl::deinit_objload_ctx(ctx);
        break;

    case MORPHO_LANDMARKS:
        warpcore::impl::deinit_morpholmload_ctx(ctx);
        break;

    default:
        return -1;
    }

    return 0;
}