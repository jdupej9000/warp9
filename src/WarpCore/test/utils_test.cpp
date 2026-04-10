#ifdef WARPCORE_TEST

#include <CppUnitTest.h>
#include "test_utils.h"
#include "../impl/utils.h"

using namespace Microsoft::VisualStudio::CppUnitTestFramework;
using namespace warpcore::impl;

namespace warpcore::test
{
	TEST_CLASS(utils_test)
	{
	public:
		TEST_METHOD(_round_down)
		{
			Assert::AreEqual(0, round_down(0, 8));
			Assert::AreEqual(0, round_down(7, 8));
			Assert::AreEqual(8, round_down(8, 8));
			Assert::AreEqual(56, round_down(63, 8));
			Assert::AreEqual(64, round_down(64, 8));
		}

		TEST_METHOD(_round_up)
		{
			Assert::AreEqual(0, round_up(0, 8));
			Assert::AreEqual(8, round_up(7, 8));
			Assert::AreEqual(8, round_up(8, 8));
			Assert::AreEqual(64, round_up(63, 8));
			Assert::AreEqual(64, round_up(64, 8));
		}

		TEST_METHOD(_is_power_of_two)
		{
			Assert::IsTrue(is_power_of_two(0));
			Assert::IsTrue(is_power_of_two(1));
			Assert::IsTrue(is_power_of_two(2));
			Assert::IsTrue(is_power_of_two(4));
			Assert::IsTrue(is_power_of_two(1048576));

			Assert::IsFalse(is_power_of_two(3));
			Assert::IsFalse(is_power_of_two(1701));
			Assert::IsFalse(is_power_of_two(311));
		}

		TEST_METHOD(_solve_ls_qr)
		{
			float a[6] = { 1,2,3,1,1,1 };
			float y[3] = { 1,2,2 };
			float b[2] = { 0,0 };

			Assert::IsTrue(solve_ls_qr(b, a, y, 3, 2, 1, false));

			assert_float_eq(0.5f, b[0]);
			assert_float_eq(0.666666f, b[1]);
		}

		TEST_METHOD(_solve_ls_chol)
		{
			float a[6] = { 1,2,3,1,1,1 };
			float y[3] = { 1,2,2 };
			float b[2] = { 0,0 };

			Assert::IsTrue(solve_ls_chol(b, a, y, 3, 2, 1, false));

			assert_float_eq(0.5f, b[0]);
			assert_float_eq(0.666666f, b[1]);
		}
	};
}

#endif