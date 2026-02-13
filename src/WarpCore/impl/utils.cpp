#include "utils.h"
#include "vec_math.h"
#include <cstring>
#include <cfloat>
#include <algorithm>
#include <cstdint>
#include <intrin.h>
#include <stdio.h>

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

	size_t compress(int dim, float* xc, const float* x, const void* allow, size_t n, bool neg)
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

					for(int k = 0; k < dim; k++)
						*(xc++) = x[(ib + i) * dim + k];
				}
			}

			ret += _mm_popcnt_u32(m);
		}

		int32_t m = *(allow_mask) ^ mask_mod;
		for (size_t i = 0; i < std::min(BLK, n - nb); i++) {
			if ((m >> i) & 0x1) {
				for (int k = 0; k < dim; k++)
					*(xc++) = x[(nb + i) * dim + k];
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
				} else if (zero) {
					x[ib + i] = 0;
				}
			}
		}

		int32_t m = *(allow_mask) ^ mask_mod;
		for (size_t i = 0; i < std::min(BLK, n - nb); i++) {
			if ((m >> i) & 0x1) {
				x[nb + i] = *(xc++);
				ret++;
			} else if (zero) {
				x[nb + i] = 0;
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
			// TODO: use expand_search_pattern_index
			int ax = a & 0x3ff; int ay = (a >> 10) & 0x3ff; int az = (a >> 20) & 0x3ff;
			int bx = b & 0x3ff; int by = (b >> 10) & 0x3ff; int bz = (b >> 20) & 0x3ff;
			return (ax * ax + ay * ay + az * az) < (bx * bx + by * by + bz * bz);
			});
	}

	void expand_search_pattern_index(int idx, int& dx, int& dy, int& dz)
	{
		dx = idx & 0x3ff; 
		dy = (idx >> 10) & 0x3ff; 
		dz = (idx >> 20) & 0x3ff;
	}

	void debug_matrix(const char* prefix, int index, const float* data, int rows, int cols)
	{
		constexpr size_t BUFF_SIZE = 512;
		char path[BUFF_SIZE];
		sprintf_s(path, BUFF_SIZE, "%s-%i.csv", prefix, index);

		FILE* f;
		fopen_s(&f, path, "w");

		if (f == nullptr)
			return;

		for (int j = 0; j < rows; j++) {
			for (int i = 0; i < cols; i++) {
				fprintf_s(f, "%f", data[j + i * rows]);

				if (i == cols - 1)
					fprintf_s(f, "\n");
				else
					fprintf_s(f, ",");
			}
		}

		fclose(f);
	}

	void debug_pcl(const char* prefix, int index, int iterartion, const float* data, int num_vert, bool soa)
	{
		constexpr size_t BUFF_SIZE = 512;
		char path[BUFF_SIZE];
		sprintf_s(path, BUFF_SIZE, "%s-%i-%i.xyz", prefix, index, iterartion);

		FILE* f;
		fopen_s(&f, path, "w");

		if (f == nullptr)
			return;

		if (soa) {
			for (int i = 0; i < num_vert; i++) {
				fprintf_s(f, "%f %f %f\n", data[i], data[num_vert + i], data[2 * num_vert + i]);
			}
		} else {
			for (int i = 0; i < num_vert; i++) {
				fprintf_s(f, "%f %f %f\n", data[3 * i], data[3 * i + 1], data[3 * i + 2]);
			}
		}

		fclose(f);
	}
}