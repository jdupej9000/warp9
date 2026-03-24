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
		tps.apply((float*)data, (const float*)templ, info->n, valid_mask, false, !negate_mask);
	}

	return ret;
}

extern "C" WCEXPORT int transform_fit(const fit_transform_info* info, int m, const float* src, const float* dest, void** ctx)
{
	if (info == nullptr || ctx == nullptr)
		return WCORE_INVALID_ARGUMENT;

	if (info->dimension != 3)
		return WCORE_INVALID_DIMENSION;

	switch (info->kind) {
	case TRANSFORM_KIND::TPS: {
		tps3* tps = new tps3{ m };
		*ctx = tps;
		tps->fit(src, dest);
		return WCORE_OK;
	}

	case TRANSFORM_KIND::LSTPS: {
		if (info->num_ctl_points < 4 || info->num_ctl_points > m || info->ctl_idx == nullptr)
			return WCORE_INVALID_ARGUMENT;

		tps3* tps = new tps3{ info->num_ctl_points };
		*ctx = tps;
		tps->fit_ls(src, dest, m, info->ctl_idx);
		return WCORE_OK;
	}
	}

	return WCORE_INVALID_ARGUMENT;
}

extern "C" WCEXPORT int transform_apply(void* ctx, int m, const float* x, float* y)
{
	transform* xform = (transform*)ctx;
	xform->apply(y, x, m, nullptr);
	return WCORE_OK;
}

extern "C" WCEXPORT int transform_destroy(void* ctx)
{
	transform* tps = (transform*)ctx;
	delete tps;
	return WCORE_OK;
}
