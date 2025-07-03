#pragma once

#include "defs.h"
#include "config.h"

enum class PCL_IMPUTE_METHOD : int32_t {
	TPS_DECIMATED = 0
};

struct impute_info {
	PCL_IMPUTE_METHOD method;
	int32_t d, n;
	int32_t decim_count;
};

extern "C" WCEXPORT int pcl_impute(const impute_info* info, void* data, const void* templ, const void* valid_mask);
