#include "impute.h"
#include "impl/utils.h"
#include "impl/tps.h"
#include "impl/kmeans.h"
#include "impl/pcl_utils.h"
#include <algorithm>
#include <vector>

using namespace warpcore::impl;

extern "C" WCEXPORT int pcl_impute(const impute_info* info, void* data, const void* templ, const void* valid_mask)
{
	// data: AOS, n x f32x3
	// templ: AOS, n x f32x3
	// valid_mask: Bit mask, per row. Not repeated.

	if (data == nullptr || templ == nullptr || valid_mask == nullptr)
		return WCORE_INVALID_ARGUMENT;

	if (info->d != 3 || info->n < info->decim_count)
		return WCORE_INVALID_ARGUMENT;

	int ret = WCORE_INVALID_ARGUMENT;
	bool negate_mask = (info->flags & PCL_IMPUTE_NEGATE_MASK) != 0;

	if (info->method == PCL_IMPUTE_METHOD::TPS_GRIDSEL) {
		const float* fdata = (const float*)data;

		int grid_dim = info->decim_count;
		std::vector<int> controlpts{};
		int num_allowed = grid_select_central(controlpts, fdata, info->n, grid_dim, valid_mask, negate_mask);
		int num_ctl = (int)controlpts.size();

		if (num_allowed == info->n) // no imputation needed
			return WCORE_OK;

		if (num_ctl < 4) 
			return WCORE_INVALID_DATA;
	
		tps3 tps{ num_ctl };
		tps.fit(info->n, (const float*)templ, (const float*)data, controlpts.data());
		tps.transform((float*)data, (const float*)templ, info->n, valid_mask, false, !negate_mask);
	}

	return ret;
}

extern "C" WCEXPORT int tps_fit(int d, int m, const float* src, const float* dest, void** ctx)
{
	tps3* tps = new tps3{ m };
	*ctx = tps;
	tps->fit(src, dest);
	return WCORE_OK;
}

extern "C" WCEXPORT int tps_transform(void* ctx, int m, const float* x, float* y)
{
	tps3* tps = (tps3*)ctx;
	tps->transform(y, x, m, nullptr);
	return WCORE_OK;
}

extern "C" WCEXPORT int tps_free(void* ctx)
{
	tps3* tps = (tps3*)ctx;
	delete tps;
	return WCORE_OK;
}
