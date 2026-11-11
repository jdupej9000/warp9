#include "utils.h"
#include "vec_math.h"
#include <cstring>
#include <cfloat>
#include <algorithm>
#include <cstdint>
#include <intrin.h>
#include <stdio.h>
#include "../config.h"
#include <Eigen>

using namespace std;
using namespace Eigen;

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
		
	/*bool solve_ls_chol(float* b, const float* a, const float* y, int nrow, int ncol, int nrhs, bool yrowmajor)
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
	}*/

	bool solve_ls_qr(float* b, const float* a, const float* y, int nrow, int ncol, int nrhs, bool yrowmajor)
	{
		assert(y != nullptr);
		assert(a != nullptr);
		assert(b != nullptr);
		assert(nrow > 0);
		assert(ncol > 0);
		assert(nrhs > 0);

		using ColMajorMatrix = Matrix<float, Dynamic, Dynamic, ColMajor>;
		using RowMajorMatrix = Matrix<float, Dynamic, Dynamic, RowMajor>;

		Map<const ColMajorMatrix> A(a, nrow, ncol);
		Map<ColMajorMatrix> B(b, ncol, nrhs);

		if (yrowmajor) {		
			Map<const RowMajorMatrix> Y(y, nrow, nrhs);
			B = A.householderQr().solve(Y);
		}
		else {			
			Map<const ColMajorMatrix> Y(y, nrow, nrhs);
			B = A.householderQr().solve(Y);
		}

		// or: B = A.colPivHouseholderQr().solve(Y);
		// or: B = A.completeOrthogonalDecomposition().solve(Y);

		return true;
	}
}