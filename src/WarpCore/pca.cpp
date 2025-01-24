#include "pca.h"
#include "impl/pca_impl.h"


using namespace warpcore::impl;

extern "C" WCEXPORT int pca_fit(pcainfo* pca, const void** data, const void* allow, void* mean_pcs, void* lambda)
{
	if (pca == NULL || data == NULL || mean_pcs == NULL)
		return WCORE_INVALID_ARGUMENT;

	// This will be later removed as support for implicit full allow is added.
	if (allow == NULL)
		return WCORE_INVALID_ARGUMENT;

	int m = pca->m;
	float* cov = new float[m * m];
	
	float* mean = (float*)mean_pcs;
	float* pcs = mean + pca->n;

	pca_mean((const float**)data, pca->n, pca->m, mean);
	pca_covmat((const float**)data, mean, allow, pca->n, pca->m, cov);



	delete[] cov;

	return WCORE_OK;
}

extern "C" WCEXPORT int pca_data_to_scores(pcainfo* pca, const void* data, const void* pcs, void* scores)
{
	if (pca == NULL || data == NULL || pcs == NULL || scores == NULL)
		return WCORE_INVALID_ARGUMENT;

	return WCORE_OK;
}

extern "C" WCEXPORT int pca_scores_to_data(pcainfo* pca, const void* scores, const void* pcs, void* data)
{
	if (pca == NULL || data == NULL || pcs == NULL || scores == NULL)
		return WCORE_INVALID_ARGUMENT;

	return WCORE_OK;
}