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
		for(int j = 0; j < n; j++) {
			sum += x[j];
			sums[j] = sum;
		}

		return sum;
	}

	void reduce_idxmin(const __m256 d, const __m256i idx, float& bestDist, int& bestIdx)
	{
		__m128 m = _mm_min_ps(_mm256_extractf128_ps(d, 0), _mm256_extractf128_ps(d, 1));
		m = _mm_min_ps(m, _mm_movehl_ps(m, m));
		m = _mm_min_ss(m, _mm_movehdup_ps(m));
		float newBestDist = _mm_cvtss_f32(m);

		if(newBestDist < bestDist) {
			__m256 bestMask = _mm256_cmp_ps(d, _mm256_broadcastss_ps(m), _CMP_EQ_OQ);
			int bestPos = _tzcnt_u32( _mm256_movemask_ps(bestMask));

			if(bestPos < 8) {
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
		for(int i = 0; i < n8; i += 8) {
			const __m256 xv = _mm256_loadu_ps(x + i);
			r0v = _mm256_min_ps(r0v, xv);
			r1v = _mm256_max_ps(r1v, xv);
		}

		float r0 = reduce_min(r0v);
		float r1 = reduce_max(r1v);

		for(int i = n8; i < n; i++) {
			r0 = std::min(r0, x[i]);
			r1 = std::max(r1, x[i]);
		}

		min = r0;
		max = r1;
	}

}