#ifdef WARPCORE_TEST

#include <CppUnitTest.h>
#include "test_utils.h"
#include "../impl/vec_math.h"

using namespace Microsoft::VisualStudio::CppUnitTestFramework;
using namespace warpcore::impl;

namespace warpcore::test
{
	TEST_CLASS(vec_math_test)
	{
	public:
		TEST_METHOD(_reduce_add_f32ptr)
		{
			float a[18] = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18 };
			assert_float_eq(171.0f, reduce_add(a, 18));
		}
	};
}

#endif