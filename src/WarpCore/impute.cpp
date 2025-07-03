#include "impute.h"
#include "impl/utils.h"
#include "impl/tps.h"
#include "impl/kmeans.h"
#include <algorithm>

using namespace warpcore::impl;

extern "C" WCEXPORT int pcl_impute(const impute_info* info, void* data, const void* templ, const void* valid_mask)
{
	if (data == nullptr || templ == nullptr || valid_mask == nullptr)
		return WCORE_INVALID_ARGUMENT;

	if (info->d != 3 || info->n < info->decim_count)
		return WCORE_INVALID_ARGUMENT;

	int ret = WCORE_INVALID_ARGUMENT;

	int max_valid = info->d * info->n;
	float* data_valid = new float[max_valid];
	int num_valid = compress(data_valid, (const float*)data, valid_mask, max_valid, false);

	if (info->method == PCL_IMPUTE_METHOD::TPS_DECIMATED) {
		constexpr int TPS_CLUSTERS = 300;
		int num_tps_points = std::min(TPS_CLUSTERS, num_valid);
		int* ci = new int[num_tps_points];
		int* labels = new int[num_valid];
		float* cx = new float[num_valid * 3];

		kmeans<3>(data_valid, num_valid, num_tps_points, cx, labels, ci);		
		delete[] labels;
		delete[] cx;
		
		std::sort(ci, ci + num_tps_points);
		expand_indices(ci, valid_mask, num_tps_points, info->n);

		float* tps_src = new float[2 * 3 * num_tps_points];
		float* tps_dest = tps_src + 3 * num_tps_points;
		get_rows<float, 3>((const float*)data, info->n, ci, num_tps_points, tps_src);
		get_rows<float, 3>((const float*)templ, info->n, ci, num_tps_points, tps_dest);
		delete[] ci;

		tps3d tps{ num_tps_points };
		tps_fit3d_soa(&tps, tps_src, tps_dest);
		tps.transform_soa((float*)data, info->n, valid_mask, false, true);

		ret = WCORE_OK;
	}

	delete[] data_valid;

	return ret;
}