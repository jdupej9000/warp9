#include "pca.h"
#include "impl/pca_impl.h"


using namespace warpcore::impl;

extern "C" WCEXPORT int pca_fit(pcainfo* pca, const void** data, const void* allow, void* mean_pcs, void* var)
{
	// mean_pcs holds the mean and the principal components - dimension is (1 + n) * m, no padding
	// allow is a bitfield - **bits** prediate rows of data. This can be nullpts, in which case it is assumed that all rows are enabled.
	// var holds the proportion of explained variance and can be nullptr to forgo calculating it.
	if (pca == NULL || data == NULL || mean_pcs == NULL)
		return WCORE_INVALID_ARGUMENT;

	// This will be later removed as support for implicit full allow is added.
	if (allow == NULL)
		return WCORE_INVALID_ARGUMENT;

	int n = pca->n;
	float* cov = new float[n * n];
	
	float* mean = (float*)mean_pcs;
	float* pcs = mean + pca->m;

	pca_mean((const float**)data, pca->n, pca->m, mean);
	pca_covmat((const float**)data, mean, allow, pca->n, pca->m, cov);

	if (pca->flags & PCA_SCALE_TO_UNITY) {
		pca_cov_to_cor(cov, n);
	}

	// cov is destroyed after this call
	pca_make_pcs((const float**)data, mean, cov, pca->n, pca->m, pca->npcs, (float*)var, pcs);

	delete[] cov;

	return WCORE_OK;
}

extern "C" WCEXPORT int pca_data_to_scores(pcainfo* pca, const void* data, const void* pcs, const void* allow, void* scores)
{
	if (pca == NULL || data == NULL || pcs == NULL || scores == NULL)
		return WCORE_INVALID_ARGUMENT;

	int m = pca->m;
	int n = pca->n;
	pca_make_scores((const float*)data, (const float*)pcs, (const float*)pcs + m, allow, n, m, (float*)scores);

	return WCORE_OK;
}

extern "C" WCEXPORT int pca_scores_to_data(pcainfo* pca, const void* scores, const void* pcs, void* data)
{
	if (pca == NULL || data == NULL || pcs == NULL || scores == NULL)
		return WCORE_INVALID_ARGUMENT;

	int m = pca->m;
	int n = pca->n;
	pca_predict((const float*)scores, (const float*)pcs, (const float*)pcs + m, n, m, (float*)data);

	return WCORE_OK;
}