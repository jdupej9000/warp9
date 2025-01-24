#include "../config.h"

namespace warpcore::impl
{
	void pca_mean(const float** data, int n, int m, float* mean);
	void pca_covmat(const float** data, const float* mean, const void* allow, int n, int m, float* cov);
};