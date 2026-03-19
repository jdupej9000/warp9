#pragma once
#include "../p3f.h"

namespace warpcore::impl
{
	class transform
	{
	public:
		virtual p3f transform(p3f x) const noexcept = 0;
		virtual void transform(float* y, const float* x, int n, const void* allow, bool neg_allow) const = 0;
	};
};