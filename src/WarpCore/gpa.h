#pragma once

#include "defs.h"
#include "config.h"

struct pclstat3 {
    float x0[3];
    float x1[3];
    float center[3];
    float size;
};

extern "C" WCEXPORT int gpa_fit(const void** data, int d, int n, int m, rigid3* xforms, void* mean);
extern "C" WCEXPORT int rigid_transform(const void* x, int d, int m, const rigid3* xform, void* res);
extern "C" WCEXPORT int pcl_stat(const void* x, int d, int m, pclstat3* stat);