#include "utils.h"
#include "vec_math.h"
#include <cstring>
#include <cfloat>
#include <algorithm>
#include <cstdint>
#include <immintrin.h>

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

	void range(const float* x, int n, float& min, float& max)
	{
		__m256 r0v = _mm256_set1_ps(FLT_MAX), r1v = _mm256_set1_ps(FLT_MIN);

		int n8 = round_down(n, 8);
		for (int i = 0; i < n8; i += 8) {
			const __m256 xv = _mm256_loadu_ps(x + i);
			r0v = _mm256_min_ps(r0v, xv);
			r1v = _mm256_max_ps(r1v, xv);
		}

		float r0 = reduce_min(r0v);
		float r1 = reduce_max(r1v);

		for (int i = n8; i < n; i++) {
			r0 = std::min(r0, x[i]);
			r1 = std::max(r1, x[i]);
		}

		min = r0;
		max = r1;
	}

	size_t compress(float* xc, const float* x, const void* allow, size_t n, bool neg)
	{
		constexpr size_t BLK = 32;
		size_t nb = round_down((int)n, (int)BLK);
		const int32_t* allow_mask = (const int32_t*)allow;
		int32_t mask_mod = neg ? 0xffffffff : 0x0;
		size_t ret = 0;

		for (size_t ib = 0; ib < nb; ib += BLK) {
			int32_t m = *(allow_mask++) ^ mask_mod;
			for (size_t i = 0; i < BLK; i++) {
				if ((m >> i) & 0x1) {
					*(xc++) = x[ib + i];
					ret++;
				}
			}
		}

		int32_t m = *(allow_mask) ^ mask_mod;
		for (size_t i = 0; i < std::min(BLK, n - nb); i++) {
			if ((m >> i) & 0x1) {
				*(xc++) = x[nb + i];
				ret++;
			}
		}

		return ret;
	}

	void expand(float* x, const float* xc, const void* allow, size_t n, bool neg, bool zero)
	{
		constexpr size_t BLK = 32;
		size_t nb = round_down((int)n, (int)BLK);
		const int32_t* allow_mask = (const int32_t*)allow;
		int32_t mask_mod = neg ? 0xffffffff : 0x0;
		size_t ret = 0;

		for (size_t ib = 0; ib < nb; ib += BLK) {
			int32_t m = *(allow_mask++) ^ mask_mod;
			for (size_t i = 0; i < BLK; i++) {
				if ((m >> i) & 0x1) {
					x[ib + i] = *(xc++);
					ret++;
				}
				else if (zero) {
					x[ib + i] = 0;
				}
			}
		}

		int32_t m = *(allow_mask) ^ mask_mod;
		for (size_t i = 0; i < std::min(BLK, n - nb); i++) {
			if ((m >> i) & 0x1) {
				x[nb + i] = *(xc++);
				ret++;
			}
			else if (zero) {
				x[nb + i] = 0;
			}
		}
	}

	void expand_indices(int* idx, const void* allow, size_t num_idx, int max_idx)
	{
		// replace each idx with the index of the idx'th allowed bit
		// idx must be sorted

		// example: 
		//            1:v  3:v    5:not found
		// allow = 000011001010001
		// idx   = 1,3,5
		// idx  <- 5,10,-1

		const int32_t* allow_mask = (const int32_t*)allow;
		int num_allowed = 0;

		constexpr int BLK = 32;
		int max_idx_b = max_idx / BLK;
		int allow_idx = 0;
		int j = 0;
		for (int i = 0; i < max_idx_b; i++) {
			int nab = _mm_popcnt_u32(allow_mask[i]);
			
			if (idx[j] >= num_allowed + nab) {
				// one or more mappings occur in this BLK-sized range

				int sumw = 0;
				for (int k = 0; k < BLK; k++) {
					if ((allow_mask[i] >> k) & 0x1) {
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
		int32_t mask_last = allow_mask[max_idx_b];
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
}