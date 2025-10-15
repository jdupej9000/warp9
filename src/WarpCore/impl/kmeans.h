#pragma once

#include "../config.h"
#include <float.h>
#include "random.h"
#include <cstring>
#include <stdio.h>
#include "utils.h"

namespace warpcore::impl
{
    /**
     * @brief K-means++ initialization algorithm. Adapted from https://rosettacode.org/wiki/K-means%2B%2B_clustering#C
     * 
     * @param x Input data organized as a column-major matrix where rows are data points.
     * @param n Number of data points.
     * @param k Number of clusters.
     * @param ci Cluster center indices. This must be preallocated to k integers.
     * @param label Output cluster labels. This must be preallocated to n integers.
     * @param d Temporary storage for distances. This must be preallocated to n floats.
     * @param dc Temporary storage for cumulative distances. This must be preallocated to n floats.
     */
    template<int NDim>
    void kmeanspp(const float* x, int n, int k, int* ci, int* label, float* d, float* dc)
    {
        uint64_t rnd = 0x123456789abcdefull;
        ci[0] = rand_xorshift32(rnd) % n;
        
        for(int i = 0; i < n; i++)
            d[i] = FLT_MAX;

        for(int c = 1; c < k; c++) {
            for(int j = 0; j < n; j++) {
                const float dd = distsq<NDim>(x, n, j, ci[c-1]);
                if(dd < d[j])
                    d[j] = dd;
            }

            const float sum = cumsum(d, n, dc);
            const float r = rand_xorshift32(rnd) / (float)UINT32_MAX * sum;
            ci[c] = binary_search(dc, n, r);
        }

        // Assign labels.
        for(int i = 0; i < n; i++)
            label[i] = nearest<NDim>(x, ci, n, k, i);
    }

    /**
     * @brief LLoyd's k-means algorithm. Adapted from https://rosettacode.org/wiki/K-means%2B%2B_clustering#C
     * 
     * @param x Input data organized as a column-major matrix where rows are data points.
     * @param n Number of data points.
     * @param k Number of clusters.
     * @param cent Cluster centers organized the same way as x. This must be preallocated to NDim * k floats.
     * @param label Output cluster labels. This must be preallocated to n integers.
     * @param convCount Convergence count.
     * @param maxIt Maximum number of iterations.
     */
    template<int NDim>
    void kmeans(const float* x, int n, int k, float* cent, int* label, int* ci = nullptr, int convCount = -1, int maxIt = 100)
    {
        if(convCount < 0)
            convCount = n / 1000;

        bool own_ci = false;
        if (ci == nullptr) {
            ci = new int[k];
            own_ci = true;
        }

        float* d = new float[2 * n];
        float* dc = d + n;

        kmeanspp<NDim>(x, n, k, ci, label, d, dc);
        int it = 0, changed = 0;
        do {
            std::memset(cent, 0, sizeof(float) * NDim * k);
            std::memset(ci, 0, sizeof(int) * k);

            for(int i = 0; i < n; i++) {
                const int j = label[i];
                ci[j]++;
                for(int d = 0; d < NDim; d++)
                    cent[d + NDim * j] += x[d + NDim * i];
            }

            for(int j = 0; j < k; j++) {
                if(ci[j] > 0) {
                    for(int d = 0; d < NDim; d++)
                        cent[d + NDim * j] /= ci[j];
                }
            }

            changed = 0;
            for(int i = 0; i < n; i++) {
                const float* row = x + NDim * i;
                const int j = nearest<NDim>(cent, k, row);
                if(j != label[i]) {
                    label[i] = j;
                    changed++;
                }
            }

            it++;
        } while(it < maxIt && changed > convCount);

        if(own_ci)
            delete[] ci;

        delete[] d;
    }
};