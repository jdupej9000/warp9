#include "../config.h"
#include <vector>

namespace warpcore::impl
{
    void pcl_center(const float* x, int d, int m, float* c);
    float pcl_cs(const float* x, int d, int m, const float* offs);
    void pcl_transform(const float* x, int d, int m, bool add, float sc, const float* offs, const float* rot, float* y);
    void pcl_transform(const float* x, int d, int m, bool add, float sc, const float* offs, float* y);
    float pcl_rmse(const float* x, const float* y, int d, int m);
    void pcl_aabb(const float* x, int d, int m, float* x0, float* x1);
    int pcl_aabb(const float* x, int d, int m, float* x0, float* x1, const void* allow, bool neg_allow);
    int grid_select(std::vector<int>& indices, const float* x, size_t n, int grid_dim, const void* allow, bool neg);
};