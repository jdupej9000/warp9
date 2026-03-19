#pragma once
#include "../p3f.h"

namespace warpcore::impl
{
	class transform
	{
	public:
		virtual p3f apply(p3f x) const noexcept = 0;
		virtual void apply(float* y, const float* x, int n, const void* allow, bool neg_allow) const = 0;
	};
};