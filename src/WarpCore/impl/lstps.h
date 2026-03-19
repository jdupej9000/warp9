#pragma once
#include "transform.h"

namespace warpcore::impl
{
	class lstps : public transform
	{
	public:
		virtual p3f transform(p3f x) const noexcept;
		virtual void transform(float* y, const float* x, int n, const void* allow, bool neg_allow) const;

		static lstps* fit(const float* x, const float* y, int n, const int* ctl_indices, int num_ctl_indices);
	}
};