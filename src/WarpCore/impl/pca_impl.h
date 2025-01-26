#include "../config.h"

namespace warpcore::impl
{
	void pca_mean(const float** data, int n, int m, float* mean);
	void pca_covmat(const float** data, const float* mean, const void* allow, int n, int m, float* cov);
	void pca_make_pcs(const float** data, const float* mean, float* cov, int n, int m, int npcs, float* var, float* pcs);
	void pca_make_scores(const float* x, const float* mean, const float* pcs, const void* allow, int n, int m, float* sc);
	void pca_predict(const float* scores, const float* mean, const float* pcs, int n, int m, float* x);
};