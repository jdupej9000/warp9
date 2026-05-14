#ifdef WARPCORE_TEST

#include <CppUnitTest.h>
#include "test_utils.h"
#include <iostream>
#include "../impl/search_impl.h"

using namespace Microsoft::VisualStudio::CppUnitTestFramework;
using namespace warpcore::impl;
using namespace std;

namespace warpcore::test
{
	void make_two_triangles(float* vertAosoa);
	int _pttri_case(const float* vertAosoa, p3f pt, float expU, float expV, int expIdx=0);

	TEST_CLASS(pttri_test)
	{
	public:
		TEST_METHOD(_pttri_onetriangle)
		{
			float vx[9 * 8];
			make_two_triangles(vx);

			int num_err = 0;
			num_err += _pttri_case(vx, _p3f(0, 2, 0), 0.5, 0); 
			num_err += _pttri_case(vx, _p3f(0, 1, 0), 0, 0);
			num_err += _pttri_case(vx, _p3f(0, 0, 0), 0, 0); 
			num_err += _pttri_case(vx, _p3f(1, 0, 0), 0, 0); 
			num_err += _pttri_case(vx, _p3f(1.5, 0, 0), 0, 0.5);
			num_err += _pttri_case(vx, _p3f(2, 0, 0), 0, 1);
			num_err += _pttri_case(vx, _p3f(2.5, 0, 0), 0, 1);
			num_err += _pttri_case(vx, _p3f(2.5, 1, 0), 0, 1);
			num_err += _pttri_case(vx, _p3f(1.5, 2, 0), 0.5, 0.5); 
			num_err += _pttri_case(vx, _p3f(1, 4, 0), 1, 0);
			num_err += _pttri_case(vx, _p3f(0, 5, 0), 1, 0);
			num_err += _pttri_case(vx, _p3f(0, 3, 0), 1, 0);
			num_err += _pttri_case(vx, _p3f(1, 1, 0), 0, 0); 
			num_err += _pttri_case(vx, _p3f(1, 2, 0), 0.5, 0); 
			num_err += _pttri_case(vx, _p3f(1, 5, 0), 1, 0);
			num_err += _pttri_case(vx, _p3f(1.5, 1, 0), 0, 0.5); 

			num_err += _pttri_case(vx, _p3f(0, 2, -1), 0.5, 0);
			num_err += _pttri_case(vx, _p3f(0, 1, -1), 0, 0);
			num_err += _pttri_case(vx, _p3f(0, 0, -1), 0, 0);
			num_err += _pttri_case(vx, _p3f(1, 0, -1), 0, 0);
			num_err += _pttri_case(vx, _p3f(1.5, 0, -1), 0, 0.5);
			num_err += _pttri_case(vx, _p3f(2, 0, -1), 0, 1);
			num_err += _pttri_case(vx, _p3f(2.5, 0, -1), 0, 1);
			num_err += _pttri_case(vx, _p3f(2.5, 1, -1), 0, 1);
			num_err += _pttri_case(vx, _p3f(1.5, 2, -1), 0.5, 0.5);
			num_err += _pttri_case(vx, _p3f(1, 4, -1), 1, 0);
			num_err += _pttri_case(vx, _p3f(0, 5, -1), 1, 0);
			num_err += _pttri_case(vx, _p3f(0, 3, -1), 1, 0);
			num_err += _pttri_case(vx, _p3f(1, 1, -1), 0, 0);
			num_err += _pttri_case(vx, _p3f(1, 2, -1), 0.5, 0);
			num_err += _pttri_case(vx, _p3f(1, 5, -1), 1, 0);
			num_err += _pttri_case(vx, _p3f(1.5, 1, -1), 0, 0.5);

			num_err += _pttri_case(vx, _p3f(0, 2, 1), 0.5, 0);
			num_err += _pttri_case(vx, _p3f(0, 1, 1), 0, 0);
			num_err += _pttri_case(vx, _p3f(0, 0, 1), 0, 0);
			num_err += _pttri_case(vx, _p3f(1, 0, 1), 0, 0);
			num_err += _pttri_case(vx, _p3f(1.5, 0, 1), 0, 0.5);
			num_err += _pttri_case(vx, _p3f(2, 0, 1), 0, 1);
			num_err += _pttri_case(vx, _p3f(2.5, 0, 1), 0, 1);
			num_err += _pttri_case(vx, _p3f(2.5, 1, 1), 0, 1);
			num_err += _pttri_case(vx, _p3f(1.5, 2, 1), 0.5, 0.5);
			num_err += _pttri_case(vx, _p3f(1, 4, 1), 1, 0);
			num_err += _pttri_case(vx, _p3f(0, 5, 1), 1, 0);
			num_err += _pttri_case(vx, _p3f(0, 3, 1), 1, 0);
			num_err += _pttri_case(vx, _p3f(1, 1, 1), 0, 0);
			num_err += _pttri_case(vx, _p3f(1, 2, 1), 0.5, 0);
			num_err += _pttri_case(vx, _p3f(1, 5, 1), 1, 0);
			num_err += _pttri_case(vx, _p3f(1.5, 1, 1), 0, 0.5);

			num_err += _pttri_case(vx, _p3f(0, 2, 1000), 0.5, 0, 1);
			num_err += _pttri_case(vx, _p3f(0, 1, 1000), 0, 0, 1);
			num_err += _pttri_case(vx, _p3f(0, 0, 1000), 0, 0, 1);
			num_err += _pttri_case(vx, _p3f(1, 0, 1000), 0, 0,1 );
			num_err += _pttri_case(vx, _p3f(1.5, 0, 1000), 0, 0.5, 1);
			num_err += _pttri_case(vx, _p3f(2, 0, 1000), 0, 1, 1);
			num_err += _pttri_case(vx, _p3f(2.5, 0, 1000), 0, 1, 1);
			num_err += _pttri_case(vx, _p3f(2.5, 1, 1000), 0, 1, 1);
			num_err += _pttri_case(vx, _p3f(1.5, 2, 1000), 0.5, 0.5, 1);
			num_err += _pttri_case(vx, _p3f(1, 4, 1000), 1, 0, 1);
			num_err += _pttri_case(vx, _p3f(0, 5, 1000), 1, 0, 1);
			num_err += _pttri_case(vx, _p3f(0, 3, 1000), 1, 0, 1);
			num_err += _pttri_case(vx, _p3f(1, 1, 1000), 0, 0, 1);
			num_err += _pttri_case(vx, _p3f(1, 2, 1000), 0.5, 0, 1);
			num_err += _pttri_case(vx, _p3f(1, 5, 1000), 1, 0, 1);
			num_err += _pttri_case(vx, _p3f(1.5, 1, 1000), 0, 0.5, 1);
			Assert::AreEqual(0, num_err);
		}

		TEST_METHOD(_pttri_sanity)
		{
			float vx[9 * 8];
			make_two_triangles(vx);

			int numErr = 0;
			for (int i = 0; i < 100; i++) {
				for (int j = 0; j < 100; j++) {
					for (int k = 0; k < 100; k++) {
						p3f pt = _p3f((float)i * 0.1f, (float)j * 0.1f, (float)k * 0.1f);

						p3f bary{}, hit{};
						float dist = FLT_MAX;
						int hitIdx = _pttri(pt, vx, 2, bary, hit, dist);
						float u = p3f_get<0>(bary);
						float v = p3f_get<1>(bary);
						dist = sqrtf(dist);

						if (!(u >= 0 && u <= 1) ||
							!(v >= 0 && v <= 1) ||
							!(u + v <= 1) ||
							!(hitIdx == 0) ||
							!(dist < 17) ||
							!(p3f_get<0>(hit) >= 1 && p3f_get<0>(hit) <= 2) ||
							!(p3f_get<1>(hit) >= 1 && p3f_get<1>(hit) <= 3) ||
							!(abs(p3f_get<2>(hit)) < 1e-4f)) {

							if (numErr < 50) {
								cerr << "at [" << i << "," << j << "," << k << "]: pt=(" <<
									p3f_get<0>(pt) << "," << p3f_get<1>(pt) << "," << p3f_get<2>(pt) << "), " <<
									"u=" << u << ", v=" << v << " hit=(" <<
									p3f_get<0>(hit) << "," << p3f_get<1>(hit) << "," << p3f_get<2>(hit) << "), dist=" <<
									dist << ", idx=" << hitIdx << endl;
							}

							numErr++;
						}
					}
				}
			}

			Assert::AreEqual(0, numErr);
		}
	};

	void make_two_triangles(float* vertAosoa)
	{
		memset(vertAosoa, 0, sizeof(float) * 9 * 8);
		
		// vertex A
		vertAosoa[0 * 8] = 1; vertAosoa[0 * 8 + 1] = 1;
		vertAosoa[1 * 8] = 1; vertAosoa[1 * 8 + 1] = 1;
		vertAosoa[2 * 8] = 0; vertAosoa[2 * 8 + 1] = 1000;

		// vertex B
		vertAosoa[3 * 8] = 1; vertAosoa[3 * 8 + 1] = 1;
		vertAosoa[4 * 8] = 3; vertAosoa[4 * 8 + 1] = 3;
		vertAosoa[5 * 8] = 0; vertAosoa[5 * 8 + 1] = 1000;

		// vertex C
		vertAosoa[6 * 8] = 2; vertAosoa[6 * 8 + 1] = 2;
		vertAosoa[7 * 8] = 1; vertAosoa[7 * 8 + 1] = 1;
		vertAosoa[8 * 8] = 0; vertAosoa[8 * 8 + 1] = 1000;
	}

	int _pttri_case(const float* vertAosoa, p3f pt, float expU, float expV, int expIdx)
	{
		constexpr float tol = 1e-3f;

		p3f bary{}, hit{};
		float dist = FLT_MAX;
		int hitIdx = _pttri(pt, vertAosoa, 2, bary, hit, dist);
		float u = p3f_get<0>(bary);
		float v = p3f_get<1>(bary);

		if (abs(u - expU) > tol ||
			abs(v - expV) > tol ||
			hitIdx != expIdx) {

			cerr << "wanted (" << expU << "," << expV << ") but got (" << u << "," << v << ") idx=" << hitIdx << endl;
			return 1;
		}

		cerr << "ok" << endl;

		return 0;
	}
}

#endif