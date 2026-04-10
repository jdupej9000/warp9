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

	void WCORE_VECCALL reduce_idxmin(const __m256 d, const __m256i idx, float& bestDist, int& bestIdx)
	{
		__m128 m = _mm_min_ps(_mm256_extractf128_ps(d, 0), _mm256_extractf128_ps(d, 1));
		m = _mm_min_ps(m, _mm_movehl_ps(m, m));
		m = _mm_min_ss(m, _mm_movehdup_ps(m));
		float newBestDist = _mm_cvtss_f32(m);

		if (newBestDist < bestDist) {
			__m256 bestMask = _mm256_cmp_ps(d, _mm256_broadcastss_ps(m), _CMP_EQ_OQ);
			int bestPos = _tzcnt_u32(_mm256_movemask_ps(bestMask));

			if (bestPos < 8) {
				alignas(32) int idxSpill[8];
				_mm256_storeu_si256((__m256i*)idxSpill, idx);
				bestIdx = idxSpill[bestPos];
				bestDist = newBestDist;
			}
		}
	}

	
	void expand_indices(int* idx, const void* allow, size_t num_idx, int max_idx, bool neg)
	{
		// replace each idx with the index of the idx'th allowed bit
		// idx must be sorted

		// example: 
		//            1:v  3:v    5:not found
		// allow = 000011001010001
		// idx   = 1,3,5
		// idx  <- 5,10,-1

		const uint32_t* allow_mask = (const uint32_t*)allow;
		int num_allowed = 0;
		int32_t mask_mod = neg ? 0xffffffff : 0x0;

		constexpr int BLK = 32;
		int max_idx_b = max_idx / BLK;
		int allow_idx = 0;
		int j = 0;
		for (int i = 0; i < max_idx_b; i++) {
			int32_t m = allow_mask[i] ^ mask_mod;
			int nab = _mm_popcnt_u32(m);
			
			if (num_allowed + nab >= idx[j]) {
				// one or more mappings occur in this BLK-sized range
				int sumw = num_allowed;				
				for (int k = 0; k < BLK; k++) {
					if ((m >> k) & 0x1) {
						sumw++;
						if (sumw == idx[j]) {
							idx[j] = BLK * i + k;
							j++;
							if (j >= num_idx) 
								return;
						}
					}
				}
			}

			num_allowed += nab;			
		}

		// finish off the last incomplete DWORD
		int32_t mask_last = allow_mask[max_idx_b] ^ mask_mod;
		int blk_left = std::min(BLK, max_idx - max_idx_b);
		int sumw = 0;
		for (int k = 0; k < BLK; k++) {
			if ((mask_last >> k) & 0x1) {
				sumw++;
				if (sumw == idx[j]) {
					idx[j] = BLK * max_idx_b + k;
					j++;
					if (j >= num_idx)
						return;
				}
			}
		}

		// mark unmatched indices
		for (; j < num_idx; j++)
			idx[j] = -1;
	}

	void prepare_search_pattern_uniform(int* pat, int nx, int ny, int nz)
	{
		int index = 0;
		for (int i = 0; i < nx; i++) {
			for (int j = 0; j < ny; j++) {
				for (int k = 0; k < nz; k++) {
					pat[index] = i | (j << 10) | (k << 20);
					index++;
				}
			}
		}

		int size = nx * ny * nz;

		std::sort(pat, pat + size, [](int a, int b) -> bool {
			int ax, ay, az, bx, by, bz;
			expand_search_pattern_index(a, ax, ay, az);
			expand_search_pattern_index(b, bx, by, bz);			
			return (ax * ax + ay * ay + az * az) < (bx * bx + by * by + bz * bz);
			});
	}

	void expand_search_pattern_index(int idx, int& dx, int& dy, int& dz)
	{
		dx = idx & 0x3ff; 
		dy = (idx >> 10) & 0x3ff; 
		dz = (idx >> 20) & 0x3ff;
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
		//if (info == 0) {			
		{
			for (int i = 0; i < nrhs; i++)
				memcpy(b + i * ncol, y2 + i * nrow, sizeof(float) * ncol);
		}

		delete[] a2;
		delete[] y2;

		return info == 0;
	}
}