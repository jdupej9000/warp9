#pragma once

#include "defs.h"
#include "config.h"

enum class PCL_IMPUTE_METHOD : int32_t {
	TPS_GRIDSEL = 0,
	LSTPS_GRIDSEL = 1
};

enum PCL_IMPUTE_FLAGS {
	PCL_IMPUTE_NEGATE_MASK = 1,
	PCL_IMPUTE_ALL = 2
};

enum class TRANSFORM_KIND : int32_t {
	TPS = 0,
	LSTPS = 1
};

enum TRANSFORM_FLAGS {
};

struct impute_info {
	PCL_IMPUTE_METHOD method;
	int32_t d, n;
	int32_t decim_count;
	PCL_IMPUTE_FLAGS flags;
};

struct fit_transform_info {
	TRANSFORM_KIND kind;
	int flags;
	int dimension;

	int num_ctl_points;
	const int* ctl_idx;
};

extern "C" WCEXPORT int pcl_impute(const impute_info* info, void* data, const void* templ, const void* valid_mask);
extern "C" WCEXPORT int transform_fit(const fit_transform_info* info, int m, const float* src, const float* dest, void** ctx);
extern "C" WCEXPORT int transform_apply(void* ctx, int m, const float* x, float* y);
extern "C" WCEXPORT int transform_destroy(void* ctx);