#ifdef WARPCORE_TEST

#include "test_utils.h"
#include <format>
#include <algorithm>
#include <string>
#include <math.h>
#include <CppUnitTest.h>
#include "../impl/cpu_info.h"

using namespace Microsoft::VisualStudio::CppUnitTestFramework;
using namespace warpcore;
using namespace std;

namespace warpcore::test
{
    TEST_MODULE_INITIALIZE(_test_global_init)
    {
        warpcore::impl::init_cpuinfo();
    }

    void require_isa(WCORE_OPTPATH isa)
    {
        if (((int)warpcore::impl::get_optpath() & (int)isa) != (int)isa) {
            Assert::Fail(L"ISA not supported on this CPU.");
        }
    }

    p3f _p3f(float x, float y, float z)
    {
        return _mm_setr_ps(x, y, z, 0);
    }

    p3i _p3i(int x, int y, int z)
    {
        return _mm_setr_epi32(x, y, z, 0);
    }

    void assert_float_eq(float a, float b, float tol)
    {
        if (fabsf(b - a) > tol) {
            wstring msg = format(L"Wanted: {}, Got: {}", a, b);
            Assert::Fail(msg.c_str());
        }
    }

    void assert_p3f_eq(p3f a, p3f b, float tol)
    {
        float err = 0;
        err = max(err, fabsf(p3f_get<0>(a) - p3f_get<0>(b)));
        err = max(err, fabsf(p3f_get<1>(a) - p3f_get<1>(b)));
        err = max(err, fabsf(p3f_get<2>(a) - p3f_get<2>(b)));

        if (fabsf(err) > tol) {
            wstring msg = format(L"Wanted: ({}, {}, {}), Got: ({}, {}, {})",
                p3f_get<0>(a), p3f_get<1>(a), p3f_get<2>(a),
                p3f_get<0>(b), p3f_get<1>(b), p3f_get<2>(b));

            Assert::Fail(msg.c_str());
        }
    }

    void assert_p3i_eq(p3i a, p3i b)
    {
        if (p3i_get(a, 0) != p3i_get(b, 0) ||
            p3i_get(a, 1) != p3i_get(b, 1) ||
            p3i_get(a, 2) != p3i_get(b, 2)) {
            wstring msg = format(L"Wanted: ({}, {}, {}), Got: ({}, {}, {})",
                p3i_get(a, 0), p3i_get(a, 1), p3i_get(a, 2),
                p3i_get(b, 0), p3i_get(b, 1), p3i_get(b, 2));

            Assert::Fail(msg.c_str());
        }
    }
};

#endif