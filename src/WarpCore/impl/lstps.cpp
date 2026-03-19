#include "lstps.h"
#include <memory.h>

namespace warpcore::impl
{
	p3f lstps::transform(p3f x) const noexcept
	{
	}

	void lstps::transform(float* y, const float* x, int n, const void* allow, bool neg_allow) const
	{
	}

	static lstps* lstps::fit(const float* x, const float* y, int n, const int* ctl_indices, int num_ctl_indices)
	{
		int nrow = 4 + n;
		int ncol = 4 + num_ctls;		

		// Construct the M matrix.
		float* m = new float[nrow * ncol];
		memset(m, 0, sizeof(float) * nrow * ncol);
		for (int i = 0; i < n; i++) {
			p3f xi = p3f_set(x + 3 * i);

			m[ncol * i] = 1.0f;
			m[ncol * i + 1] = p3f_get<0>(xi);
			m[ncol * i + 2] = p3f_get<1>(xi);
			m[ncol * i + 3] = p3f_get<2>(xi);

			for (int j = 0; j < num_ctl_indices; j++) {
				p3f xj = p3f_set(x + 3 * ctl_indices[j]);
				float u = p3f_dist(xi, xi);
				m[4 + j + ncol * i] = u * u * u;
			}
		}

		// bottom-left zeros already set

		for (int j = 0; j < num_ctl_indices; j++) {
			p3f xj = p3f_set(x + 3 * ctl_indices[j]);
			m[ncol * (j + n) + j + 4] = 1.0f;
			m[ncol * (j + n + 1) + j + 4] = p3f_get<0>(xj);
			m[ncol * (j + n + 2) + j + 4] = p3f_get<1>(xj);
			m[ncol * (j + n + 3) + j + 4] = p3f_get<2>(xj);
		}

		// Construct the T matrix.
		float* t = new float[ncol * 3];
		memcpy(t, y, sizeof(float) * 3 * n);
		memset(t + 3 * n, 0, sizeof(float) * 12);

		// TODO: w = inv(M'M).M'.t
		// plug the rest into ordinary TPS

		delete[] m;
		delete[] t;
	}
};
