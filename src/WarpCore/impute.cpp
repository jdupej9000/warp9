#include "impute.h"
#include "impl/utils.h"
#include "impl/tps.h"
#include "impl/kmeans.h"
#include <algorithm>

using namespace warpcore::impl;

extern "C" WCEXPORT int pcl_impute(const impute_info* info, void* data, const void* templ, const void* valid_mask)
{
	// valid_mask: Bit mask, per row. Not repeated.

	if (data == nullptr || templ == nullptr || valid_mask == nullptr)
		return WCORE_INVALID_ARGUMENT;

	if (info->d != 3 || info->n < info->decim_count)
		return WCORE_INVALID_ARGUMENT;

	int ret = WCORE_INVALID_ARGUMENT;

	// Obtain a matrix of just allowed data.
	int max_valid = info->d * info->n;
	float* data_valid = new float[max_valid];
	int num_valid = compress(info->d, data_valid, (const float*)data, valid_mask, info->n, false);

	if (info->method == PCL_IMPUTE_METHOD::TPS_DECIMATED) {
		// Cluster allowed vertices (if their valid_mask==1) to TPS_CLUSTERS number of
		// centers. Fit a TPS at those centers bending templ into data. For each 
		// disallowed point (if their valid_mask==0), transform temp with TPS and
		// replace the respective row of data.
		// TODO: optimize allocations
		int num_tps_points = std::min(info->decim_count, num_valid);
		int* ci = new int[num_tps_points];
		int* labels = new int[num_valid];
		float* cx = new float[num_valid * 3];
		float row[3];

		kmeans<3>(data_valid, num_valid, num_tps_points, cx, labels, ci);	
		for (int i = 0; i < num_tps_points; i++) {
			get_row<float, 3>(cx, num_tps_points, i, row);
			ci[i] = nearest<3>(data_valid, num_valid, row);
		}
		delete[] labels;
		delete[] cx;
		
		std::sort(ci, ci + num_tps_points);
		expand_indices(ci, valid_mask, num_tps_points, info->n);

		float* tps_src = new float[2 * 3 * num_tps_points];
		float* tps_dest = tps_src + 3 * num_tps_points;
		get_rows<float, 3>((const float*)data, info->n, ci, num_tps_points, tps_dest);
		get_rows<float, 3>((const float*)templ, info->n, ci, num_tps_points, tps_src);
		delete[] ci;

		tps3d tps{ num_tps_points };
		tps_fit3d_soa(&tps, tps_src, tps_dest);
		tps.transform_soa((float*)data, (const float*)templ, info->n, valid_mask, false, true);

		ret = WCORE_OK;
	}

	delete[] data_valid;

	return ret;
}