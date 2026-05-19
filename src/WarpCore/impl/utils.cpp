#include "utils.h"
#include "vec_math.h"
#include <cstring>
#include <cfloat>
#include <algorithm>
#include <cstdint>
#include <intrin.h>
#include <stdio.h>
#include "../config.h"
#include <lapacke.h>
#include <cblas.h>

using namespace std;

namespace warpcore::impl
{
	int decode_zigzag(unsigned int x)
	{
		return (x >> 1) ^ (-(int)(x & 1));
	}

	int round_down(int x, int blk)
	{
		return x & -blk;
	}

	int round_up(int x, int blk)
	{
		return (x + blk - 1) & -blk;
	}

	bool is_power_of_two(size_t x)
	{
		return (x & (x - 1)) == 0;
	}

	float cumsum(const float* x, int n, float* sums)
	{
		float sum = 0;
		for (int j = 0; j < n; j++) {
			sum += x[j];
			sums[j] = sum;
		}

		return sum;
	}

	int WCORE_VECCALL reduce_idxmin(const __m256 d, const __m256i idx, float& bestDist, int& bestIdx)
	{
		float newBestDist = reduce_min(d);
		if (newBestDist < bestDist) {
			__m256 bestMask = _mm256_cmp_ps(d, _mm256_broadcast_ss(&newBestDist), _CMP_EQ_OQ);
			int bestPos = _tzcnt_u32(_mm256_movemask_ps(bestMask));

			if (bestPos < 8) {
				bestIdx = extract(idx, bestPos);
				bestDist = newBestDist;
				return bestPos;
			}			
		}

		return 0;
	}
		
	bool solve_ls_chol(float* b, const float* a, const float* y, int nrow, int ncol, int nrhs, bool yrowmajor)
	{
		assert(y != nullptr);
		assert(a != nullptr);
		assert(b != nullptr);
		assert(nrow > 0);
		assert(ncol > 0);
		assert(nrhs > 0);

		// A' . A
		float* ata = new float[ncol * ncol];
		cblas_ssyrk(CblasColMajor, CblasUpper, CblasTrans,
			ncol, nrow, 
			1.0f, a, nrow,
			0.0f, ata, ncol);

		// A' y
		if (nrhs == 1) {
			cblas_sgemv(CblasColMajor, CblasTrans,
				nrow, ncol,
				1.0f, a, nrow,
				y, 1,
				0.0f, b, 1);
		} else {
			cblas_sgemm(CblasColMajor, CblasTrans, yrowmajor ? CblasTrans : CblasNoTrans,
				ncol, nrhs, nrow,
				1.0f, a, nrow, y, nrow,
				0.0f, b, ncol);
		}

		int info = LAPACKE_sposv(LAPACK_COL_MAJOR, 'U',
			ncol, nrhs,
			ata, ncol,
			b, ncol);

		delete[] ata;

		return info == 0;
	}

	bool solve_ls_qr(float* b, const float* a, const float* y, int nrow, int ncol, int nrhs, bool yrowmajor)
	{
		assert(y != nullptr);
		assert(a != nullptr);
		assert(b != nullptr);
		assert(nrow > 0);
		assert(ncol > 0);
		assert(nrhs > 0);

		float* a2 = new float[nrow * ncol];
		memcpy(a2, a, sizeof(float) * nrow * ncol);

		// Make y2 always col-major.
		float* y2 = new float[nrow * nrhs];
		if (yrowmajor) {
			assert(nrhs == 3);
			aos_to_soa<float, 3>(y, nrow, y2);
		} else {
			memcpy(y2, y, sizeof(float) * nrow * nrhs);
		}

		// Minimze norm2(Ab - y) using QR fact.
		int info = LAPACKE_sgels(LAPACK_COL_MAJOR, 'N',
			nrow, ncol, nrhs,
			a2, nrow,
			y2, nrow);

		// Extract the solutions.		
		for (int i = 0; i < nrhs; i++)
			memcpy(b + i * ncol, y2 + i * nrow, sizeof(float) * ncol);
		
		delete[] a2;
		delete[] y2;

		return info == 0;
	}
}