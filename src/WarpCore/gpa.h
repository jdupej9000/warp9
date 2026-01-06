#pragma once

#include "defs.h"
#include "config.h"

struct pclstat3 {
    float x0[3];
    float x1[3];
    float center[3];
    float size;
};

struct gparesult {
    int32_t iter;
    float err;
};

extern "C" WCEXPORT int gpa_fit(const void** data, int d, int n, int m, rigid3* xforms, void* mean, gparesult* res);
extern "C" WCEXPORT int rigid_transform(const void* x, int d, int m, const rigid3* xform, void* res);
extern "C" WCEXPORT int pcl_stat(const void* x, int d, int m, pclstat3* stat);
extern "C" WCEXPORT int opa_fit(const void* templ, const void* floating, const void* allow, int d, int m, rigid3* xform);