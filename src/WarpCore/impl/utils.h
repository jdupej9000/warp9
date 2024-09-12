#pragma once

#include <cstddef>
#include <float.h>
#include <immintrin.h>
#include <memory.h>

namespace warpcore::impl
{
    int decode_zigzag(unsigned int x);
    int round_down(int x, int blk);
	bool is_power_of_two(size_t x);
    float cumsum(const float* x, int n, float* sums);
    void reduce_idxmin(const __m256 d, const __m256i idx, float& bestDist, int& bestIdx);
    void range(const float* x, int n, float& min, float& max);
    void foreach_voxel_central(int radius, int cx, int cy, int cz, int maxx, int maxy, int maxz, void (*fun)(int x, int y, int z));

    template<typename T>
    int binary_search(const T* x, int n, const T& v)
    {
        if (n < 1 || v < x[0])
            return 0;

        if (v > x[n-1])
            return n - 1;
        
        int il = 0;
        int ir = n - 1;

        int i = (il + ir) / 2;
        while (i != il) {
            if (x[i] <= v) {
                il = i;
            } else {
                ir = i;
            }
            i = (il + ir) / 2;		
        }		

        if (x[i] <= v)
            i = ir;

        return i;
    }

    template<typename T, int NDim>
    void get_row(const T* x, int n, int i, T* r)
    {
        for (int j = 0; j < NDim; j++)
            r[j] = x[j * n + i];
    }

    template<typename T, int NDim>
    void foreach_row(const T* x, int n, void (*fun)(const T* r, int i))
    {
        alignas(32) T r[NDim];
        for (int i = 0; i < n; i++) {
            get_row<T, NDim>(x, n, i, r);
            fun(r, i);
        }
    }

    template<typename T, int NDim, typename TAux>
    void foreach_row(const T* x, int n, TAux aux, void (*fun)(const T* r, int i, TAux aux))
    {
        alignas(32) T r[NDim];
        for (int i = 0; i < n; i++) {
            get_row<T, NDim>(x, n, i, r);
            fun(r, i, aux);
        }
    }


    template<typename T, int NDim>
    void foreach_row2(const T* x, const T* y, int n, void (*fun)(const T* r1, const T* r2, int i))
    {
        alignas(32) T r1[NDim];
        alignas(32) T r2[NDim];
        for (int i = 0; i < n; i++) {
            get_row<T, NDim>(x, n, i, r1);
            get_row<T, NDim>(y, n, i, r2);
            fun(r1, r2, i);
        }
    }

    template<typename T, int NDim, typename TAux>
    void foreach_row2(const T* x, const T* y, int n, TAux aux, void (*fun)(const T* r1, const T* r2, int i, TAux aux))
    {
        alignas(32) T r1[NDim];
        alignas(32) T r2[NDim];
        for (int i = 0; i < n; i++) {
            get_row<T, NDim>(x, n, i, r1);
            get_row<T, NDim>(y, n, i, r2);
            fun(r1, r2, i, aux);
        }
    }

    template<typename TCtx>
    void foreach_voxel_central(int radius, int cx, int cy, int cz, int maxx, int maxy, int maxz, TCtx ctx, void (*fun)(int x, int y, int z, TCtx ctx))
    {
		#define FUN(x,y,z) if((x) >= 0 && (x) < maxx && (y) >= 0 && (y) < maxy && (z) >= 0 && (z) < maxz) fun((x), (y), (z), ctx)
    	FUN(cx, cy, cz);

		// TODO: this order could still be more optimal
		for (int r = 1; r < radius; r++) {
			for(int i = 0; i < 2 * radius - 1; i++) {
				int du = decode_zigzag(i);
				for(int j = 0; j < 2 * radius - 1; j++) {
					int dv = decode_zigzag(j);
					FUN(cx + du, cy + dv, cz + r);
                    FUN(cx - du, cy - dv, cz - r);
                    FUN(cx + du, cy + r, cz + dv);
                    FUN(cx - du, cy - r, cz - dv);
                    FUN(cx + r, cy + du, cz + dv);
                    FUN(cx - r, cy - du, cz - dv);
				}
			}
		}
    }

    
    template<int NDim>
    inline float distsq(const float* x, int n, int i0, int i1)
    {
        float sum = 0;
        for(int j = 0; j < NDim; j++) {
            const float d = x[j * n + i0] - x[j * n + i1];
            sum += d * d;
        }

        return sum;
    }

    template<typename T, int NDim, int NStride=1>
    void aos_to_soa(const T* x, int n, T* y)
    {
        for(int d = 0; d < NDim; d++) {
            for(int i = 0; i < n; i++) {
                *(y++) = x[(i * NDim + d) * NStride];
            }
        }
    }    

    template<int NDim>
    int nearest(const float* x, int n, const float* t)
    {
        const __m256i rng = _mm256_setr_epi32(0, 1, 2, 3, 4, 5, 6, 7);
        __m256 bestDist = _mm256_set1_ps(FLT_MAX);
        __m256i bestIdx = _mm256_setzero_si256();

        const int nch = round_down(n, 8);
        for(int i = 0; i < nch; i += 8) {
            __m256 d = _mm256_setzero_ps();
            for(int k = 0; k < NDim; k++) {
                __m256 a = _mm256_loadu_ps(x + k * n + i);
                __m256 b = _mm256_broadcast_ss(t + k);
                __m256 c = _mm256_sub_ps(a, b);
                d = _mm256_fmadd_ps(c, c, d);
            }

            __m256 mask = _mm256_cmp_ps(bestDist, d, _CMP_GT_OQ);
            __m256i rngi = _mm256_add_epi32(rng, _mm256_set1_epi32(i));
            bestDist = _mm256_blendv_ps(bestDist, d, mask);
            bestIdx = _mm256_blendv_epi8(bestIdx, rngi, _mm256_castps_si256(mask));
        } 

        float retDist = FLT_MAX;
        int retIdx = 0;
        reduce_idxmin(bestDist, bestIdx, retDist, retIdx);

        for(int i = nch; i < n; i++) {
            float d = 0;
            for(int k = 0; k < NDim; k++) {
                const float c = x[k * n + i] - t[k];
                d += c * c;
            }

            if(d < retDist) {
                retDist = d;
                retIdx = i;
            }
        }

        return retIdx;
    }
}