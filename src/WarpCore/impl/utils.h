#pragma once

#include "../config.h"
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
    void WCORE_VECCALL reduce_idxmin(const __m256 d, const __m256i idx, float& bestDist, int& bestIdx);
    //void range(const float* x, int n, float& min, float& max);
    size_t compress(float* xc, const float* x, const void* allow, size_t n, bool neg);
    void expand(float* x, const float* xc, const void* allow, size_t n, bool neg, bool zero);
    size_t compress(int dim, float* xc, const float* x, const void* allow, size_t n, bool neg);
    void expand_indices(int* idx, const void* allow, size_t num_idx, int max_idx, bool neg);

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
    void get_rows(const T* x, int n, const int* idx, int nidx, T* r)
    {
        for (int i = 0; i < nidx; i++) {
            for (int j = 0; j < NDim; j++) {
                r[NDim * i + j] = x[NDim * idx[i] + j];
            }
        }
    }

    template<typename T, int NDim>
    void put_row(T* x, int n, int i, const T* r)
    {
        for (int j = 0; j < NDim; j++)
            x[j * n + i] = r[j];
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
    void foreach_voxel_central(int radius, int cx, int cy, int cz, int maxx, int maxy, int maxz, TCtx ctx, bool (*fun)(int x, int y, int z, int r, TCtx ctx))
    {
		#define FUN(x,y,z,l) if((x) >= 0 && (x) < maxx && (y) >= 0 && (y) < maxy && (z) >= 0 && (z) < maxz) if(!fun((x), (y), (z), (l), ctx)) return;
    	FUN(cx, cy, cz, 0);

		// TODO: this order could still be more optimal
		for (int r = 1; r < radius; r++) {
			for(int i = 0; i < 2 * r - 1; i++) {
				int du = decode_zigzag(i);
				for(int j = 0; j < 2 * r - 1; j++) {
					int dv = decode_zigzag(j);
					FUN(cx + du, cy + dv, cz + r, r);
                    FUN(cx - du, cy - dv, cz - r, r);
                    FUN(cx + du, cy + r, cz + dv, r);
                    FUN(cx - du, cy - r, cz - dv, r);
                    FUN(cx + r, cy + du, cz + dv, r);
                    FUN(cx - r, cy - du, cz - dv, r);
				}
			}
		}
    }

    template<int NDim>
    float distsq(const float* x, int i0, int i1)
    {
        float sum = 0;
        for(int j = 0; j < NDim; j++) {
            const float d = x[j + i0 * NDim] - x[j + i1 * NDim];
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

    template<typename T, int NDim, int NStride = 1>
    void soa_to_aos(T* x, int n, const T* y)
    {
        for (int d = 0; d < NDim; d++) {
            for (int i = 0; i < n; i++) {
                x[(i * NDim + d) * NStride] = *(y++);
            }
        }
    }

    template<int NDim>
    int nearest(const float* x, int n, const float* t)
    {
        /*const __m256i rng = _mm256_setr_epi32(0, 1, 2, 3, 4, 5, 6, 7);
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
        reduce_idxmin(bestDist, bestIdx, retDist, retIdx);*/

        const int nch = 0;
        float retDist = FLT_MAX;
        int retIdx = 0;
        for(int i = nch; i < n; i++) {
            float d = 0;
            for(int k = 0; k < NDim; k++) {
                const float c = x[k + NDim * i] - t[k];
                d += c * c;
            }

            if(d < retDist) {
                retDist = d;
                retIdx = i;
            }
        }

        return retIdx;
    }

    template<int NDim>
    inline int nearest(const float* x, int* ci, int n, int k, int i)
    {
        float d = FLT_MAX;
        int c = 0;
        for (int j = 0; j < k; j++) {
            const float d0 = distsq<NDim>(x, i, ci[j]);
            if (d0 < d) {
                d = d0;
                c = j;
            }
        }

        return c;
    }
}