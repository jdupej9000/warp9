#pragma once

enum FILE_FORMAT {
    WAVEFRONT_OBJ = 0,
    MORPHO_LANDMARKS = 1
};

enum IMPORT_DATA {
    IMPORT_POS3F_SOA = 0,
    IMPORT_IDX3I_SOA = 1
};

extern "C" WCEXPORT int load_init(int format, int flags, void** ctx);
extern "C" WCEXPORT int load_push(void* ctx, const void* data, int size);
extern "C" WCEXPORT int load_get(void* ctx, int index, void* data, int size);
extern "C" WCEXPORT int load_free(void* ctx);
