#pragma once

#ifdef WARPCORE_TEST
#include "../p3f.h"
#include "../defs.h"
#include <intrin.h>

namespace warpcore::test
{
	void require_isa(WCORE_OPTPATH isa);
	warpcore::p3f _p3f(float x, float y, float z);
	warpcore::p3i _p3i(int x, int y, int z);
	void assert_float_eq(float a, float b, float tol=1e-5f);
	void assert_p3f_eq(warpcore::p3f a, warpcore::p3f b, float tol=1e-5f);
	void assert_p3i_eq(warpcore::p3i a, warpcore::p3i b);
};

#endif
