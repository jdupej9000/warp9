#include "gpa.h"
#include "impl/gpa_impl.h"
#include "impl/pcl_utils.h"
#include <cfloat>
#include <cstring>
#include <memory>

extern "C" int gpa_fit(const void** data, int d, int n, int m, rigid3* xforms, void* mean, gparesult* res)
{
    constexpr int MAX_IT = 150;
    constexpr float TOL_REL = 1e-5f;

    if(d != 3 || m < 4 || xforms == NULL)
        return WCORE_INVALID_ARGUMENT;

    float* temp_mean = new float[d * m];
    float* m1 = (float*)mean;
    float* m2 = temp_mean;

    warpcore::impl::gpa_init_mean((const float*)data[0], d, m, m1);
    int it = 0;
    float err = 1;
    while(it < MAX_IT) {
        for(int i = 0; i < n; i++) {
            int opa_res = warpcore::impl::opa_fit((const float*)data[i], m1, d, m, xforms[i].offs, &xforms[i].cs, xforms[i].rot);
        }

        warpcore::impl::gpa_update_mean((const float**)data, d, n, m, xforms, m2);
        const float new_err = warpcore::impl::pcl_rmse(m1, m2, d, m);
        if((err-new_err) / new_err < TOL_REL) 
            break;
    
        std::swap(m1, m2);
        err = new_err;
        it++;
    }
    
    if(mean != m1)
        memcpy(mean, temp_mean, sizeof(float) * d * m);

    delete[] temp_mean;

    if (res != NULL)
    {
        res->iter = it;
        res->err = err;
    }

    return (it < MAX_IT) ? WCORE_OK : WCORE_NONCONVERGENCE;
}

extern "C" int opa_fit(const void* templ, const void* floating, int d, int m, rigid3* xform)
{
    if (templ == nullptr || floating == nullptr || d != 3 || m < 4 || xform == NULL)
        return WCORE_INVALID_ARGUMENT;

    const float* t = (const float*)templ;
    const float* x = (const float*)floating;

    // make a normalized template (center at 0, cs = 1)
    float* temp_mean = new float[d * m];    
    rigid3 outer;
    warpcore::impl::pcl_center(t, d, m, outer.offs);
    outer.cs = warpcore::impl::pcl_cs(t, d, m, outer.offs);
    warpcore::impl::pcl_transform(x, d, m, false, 1.0f / outer.cs, outer.offs, temp_mean);
    outer.rot[0] = 1; outer.rot[1] = 0; outer.rot[2] = 0;
    outer.rot[3] = 0; outer.rot[4] = 1; outer.rot[5] = 0;
    outer.rot[6] = 0; outer.rot[7] = 0; outer.rot[8] = 1;

    // transform floating onto the normalized template
    rigid3 inner;
    warpcore::impl::opa_fit(x, temp_mean, d, m, inner.offs, &inner.cs, inner.rot);

    // xform(x) = outer(inner(x))
    warpcore::impl::rigid_combine(xform, &inner, &outer);

    delete[] temp_mean;

    return WCORE_OK;
}

extern "C" int rigid_transform(const void* x, int d, int m, const rigid3* xform, void* res)
{
    if(d != 3 || m < 4 || xform == NULL)
        return WCORE_INVALID_ARGUMENT;
    
    warpcore::impl::pcl_transform((const float*)x, d, m, false, 1.0f / xform->cs, xform->offs, xform->rot, (float*)res);

    return WCORE_OK;
}

extern "C" int pcl_stat(const void* x, int d, int m, pclstat3* stat)
{
    if(d != 3 || x == NULL || m < 1 || stat == NULL)
        return WCORE_INVALID_ARGUMENT;

    const float* xf = (const float*)x;
    warpcore::impl::pcl_center(xf, d, m, stat->center);
    stat->size = warpcore::impl::pcl_cs(xf, d, m, stat->center);
    warpcore::impl::pcl_aabb(xf, d, m, stat->x0, stat->x1);
    return WCORE_OK;
}