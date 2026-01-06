#include "../config.h"
#include "../defs.h"

namespace warpcore::impl
{
    int opa_fit(const float* x, const float* y, const void* allow, int d, int m, float* xoffs, float* xcs, float* rot);
    void gpa_init_mean(const float* x, int d, int m, float* mean);
    void gpa_update_mean(const float** data, int d, int n, int m, const rigid3* xforms, float* mean);
    void rigid_combine(rigid3* ret, const rigid3* f, const rigid3* g);
};