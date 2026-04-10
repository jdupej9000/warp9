#ifdef WARPCORE_TEST

#include <CppUnitTest.h>
#include "test_utils.h"
#include "../p3f.h"

using namespace Microsoft::VisualStudio::CppUnitTestFramework;

namespace warpcore::test
{
	TEST_CLASS(p3f_test)
	{
	public:
		TEST_METHOD(_p3f_set_ptr)
		{
			float a[3] = { 1.0f, 2.0f, 3.0f };
			assert_p3f_eq(_p3f(1.0f, 2.0f, 3.0f), 
				p3f_set(a));
		}

		TEST_METHOD(_p3f_abs)
		{
			assert_p3f_eq(_p3f(0, 0, 0),
				p3f_abs(p3f_zero()));

			assert_p3f_eq(_p3f(1, 2, 3),
				p3f_abs(p3f_set(-1.0f, -2.0f, -3.0f)));
		}

		TEST_METHOD(_p3i_to_p3f)
		{
			assert_p3f_eq(_p3f(0, 0, 0),
				p3i_to_p3f(_p3i(0, 0, 0)));

			assert_p3f_eq(_p3f(1, 2, 3),
				p3i_to_p3f(_p3i(1, 2, 3)));

			assert_p3f_eq(_p3f(-10, 20, -30),
				p3i_to_p3f(_p3i(-10, 20, -30)));
		}

		TEST_METHOD(_p3f_to_p3i)
		{
			assert_p3i_eq(_p3i(0, 0, 0),
				p3f_to_p3i(_p3f(0, 0, 0)));

			assert_p3i_eq(_p3i(0, 0, 1),
				p3f_to_p3i(_p3f(0.4999f, 0.5f, 1.0f)));
		}
	};
}

#endif