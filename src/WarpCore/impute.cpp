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
	bool negate_mask = !!(info->flags & PCL_IMPUTE_NEGATE_MASK);

	if (info->method == PCL_IMPUTE_METHOD::TPS_DECIMATED) {
		// Maybe remove this method.

		int max_valid = info->d * info->n;
		float* data_valid = new float[max_valid];
		int num_valid = compress(info->d, data_valid, (const float*)data, valid_mask, info->n, !negate_mask);
		
		// Cluster allowed vertices (if their valid_mask==1) to TPS_CLUSTERS number of
		// centers. Fit a TPS at those centers bending templ into data. For each 
		// disallowed point (if their valid_mask==0), transform temp with TPS and
		// replace the respective row of data.
		// TODO: optimize allocations
		int num_tps_points = std::min(info->decim_count, num_valid);
		int* ci = new int[num_tps_points];
		int* labels = new int[num_valid];
		float* cx = new float[num_valid * 3];

		kmeans<3>(data_valid, num_valid, num_tps_points, cx, labels, ci);	
		for (int i = 0; i < num_tps_points; i++) {
			const float* row = cx + 3 * i;
			ci[i] = nearest<3>(data_valid, num_valid, row);
		}
		delete[] labels;
		delete[] cx;
		
		std::sort(ci, ci + num_tps_points);

		for (int i = 1; i < num_tps_points; i++) {
			if (ci[i - 1] == ci[i]) {
				throw new std::exception();
			}
		}

		expand_indices(ci, valid_mask, num_tps_points, info->n, !negate_mask);

		float* tps_src = new float[2 * 3 * num_tps_points];
		float* tps_dest = tps_src + 3 * num_tps_points;
		get_rows<float, 3>((const float*)data, info->n, ci, num_tps_points, tps_dest);
		get_rows<float, 3>((const float*)templ, info->n, ci, num_tps_points, tps_src);
		delete[] ci;

		tps3 tps{ num_tps_points };
		tps.fit(tps_src, tps_dest);
		tps.transform((float*)data, (const float*)templ, info->n, valid_mask, false, !negate_mask);

		ret = WCORE_OK;
		
		delete[] data_valid;
	} else if (info->method == PCL_IMPUTE_METHOD::TPS_GRIDSEL) {
		const float* fdata = (const float*)data;

		int grid_dim = info->decim_count;
		std::vector<int> controlpts{};
		int num_allowed = grid_select(controlpts, fdata, info->n, grid_dim, valid_mask, negate_mask);
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
