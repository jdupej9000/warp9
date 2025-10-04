#include "gpa_impl.h"
#include "vec_math.h"
#include "pcl_utils.h"
#include "utils.h"
#include <immintrin.h>
#include <cmath>
#include <cstring>
#include <lapacke.h>
#include <cblas.h>
#include <exception>

namespace warpcore::impl
{
    float opa_covcomp(const float* x, const float* y, int m, float off, float rcs);
    void opa_cov(const float* x, const float* y, int d, int m, const float* xoff, float xcs, float* cov);
    float mat3_det(const float* m);
  
    float opa_covcomp(const float* x, const float* y, int m, float off, float rcs) 
    {
        int m8 = round_down(m, 8);
        const __m256 off8 = _mm256_set1_ps(off);
        const __m256 rcs8 = _mm256_set1_ps(rcs);

        __m256 accum = _mm256_setzero_ps();
        for(int i = 0; i < m8; i += 8) {
            const __m256 xcomp = _mm256_mul_ps(rcs8, _mm256_sub_ps(_mm256_loadu_ps(x + i), off8));
            accum = _mm256_fmadd_ps(xcomp, _mm256_loadu_ps(y + i), accum);
        }

        float ret = 0;
        for(int i = m8; i < m; i++) {
            float xcomp = rcs * (x[i] - off);
            ret += xcomp * y[i];
        }

        return ret + reduce_add(accum);
    }

    void opa_cov(const float* x, const float* y, int d, int m, const float* xoff, float xcs, float* cov)
    {
        for(int i = 0; i < d; i++) {
            const float* xi = x + i * m;

            for(int j = 0; j < d; j++) {
                const float* yj = y + j * m;
                *(cov++) = opa_covcomp(xi, yj, m, xoff[i], 1.0f / xcs);
            }
        }
    }

    int opa_fit(const float* x, const float* y, int d, int m, float* xoffs, float* xcs, float* rot)
    {
        WCORE_ASSERT(d == 3); // static cov size

        pcl_center(x, d, m, xoffs);
        float cs = pcl_cs(x, d, m, xoffs);
        *xcs = cs;

        //float* cov = STACK_ALLOC(float, d * d * 3 + 2 * d);
        float cov[33 + 10];
        memset(cov, 0x0, sizeof(float) * 43);
        float* u = cov + d * d;
        float* vt = u + d * d;
        float* s = vt + d * d;
        float* superb = s + d;
        opa_cov(x, y, d, m, xoffs, cs, cov);

        int info = LAPACKE_sgesvd(LAPACK_COL_MAJOR, 'A', 'A', d, d, cov, d, s, u, d, vt, d, superb);
        if (info != 0)
            return -1;

        float h[9];
        memset(h, 0, sizeof(float) * d * d);
        cblas_sgemm(CblasColMajor, CblasTrans, CblasTrans, d, d, d, 1.0f, vt, d, u, d, 0.0f, h, d);
 
        if (mat3_det(h) <= 0) {
            throw std::exception{ "Got a reflection." };
        } else {
            memcpy(rot, h, sizeof(float) * d * d);
        }


        // delete[] cov; NOT NEEDED

        return 0;
    }

    void gpa_init_mean(const float* x, int d, int m, float* mean)
    {
        WCORE_ASSERT(d == 3);
        
        //float* offs = STACK_ALLOC(float, d);
        float offs[3];
        pcl_center(x, d, m, offs);
        float cs = pcl_cs(x, d, m, offs);

        pcl_transform(x, d, m, false, 1.0f / cs, offs, mean);
        // delete[] offs; NOT NEEDED
    }

    void gpa_update_mean(const float** data, int d, int n, int m, const rigid3* xforms, float* mean) 
    {
        pcl_transform(data[0], d, m, false, 1.0f / xforms[0].cs, xforms[0].offs, xforms[0].rot, mean);
        for(int i = 1; i < n; i++)
            pcl_transform(data[i], d, m, true, 1.0f / xforms[i].cs, xforms[i].offs, xforms[i].rot, mean);

        cblas_sscal(d * m, 1.0f / n, mean, 1);
    }

    void rigid_combine(rigid3* ret, const rigid3* f, const rigid3* g)
    {
        const float* H = g->rot;
        const float* R = f->rot;

        // returns h(x) = g(f(x))
        ret->cs = f->cs * g->cs;

        float csr = 1.0f / f->cs;
        ret->offs[0] = f->offs[0] + csr * (R[0] * g->offs[0] + R[1] * g->offs[1] + R[2] * g->offs[2]);
        ret->offs[1] = f->offs[1] + csr * (R[3] * g->offs[0] + R[4] * g->offs[1] + R[5] * g->offs[2]);
        ret->offs[2] = f->offs[2] + csr * (R[6] * g->offs[0] + R[7] * g->offs[1] + R[8] * g->offs[2]);

        ret->rot[0] = H[0] * R[0] + H[3] * R[1] + H[6] * R[2];
        ret->rot[1] = H[1] * R[0] + H[4] * R[1] + H[7] * R[2];
        ret->rot[2] = H[2] * R[0] + H[5] * R[1] + H[8] * R[2];

        ret->rot[3] = H[0] * R[3] + H[3] * R[4] + H[6] * R[5];
        ret->rot[4] = H[1] * R[3] + H[4] * R[4] + H[7] * R[5];
        ret->rot[5] = H[2] * R[3] + H[5] * R[4] + H[8] * R[5];
        
        ret->rot[6] = H[0] * R[6] + H[3] * R[7] + H[6] * R[8];
        ret->rot[7] = H[1] * R[6] + H[4] * R[7] + H[7] * R[8];
        ret->rot[8] = H[2] * R[6] + H[5] * R[7] + H[8] * R[8];
    }

    float mat3_det(const float* m)
    {
        float x = m[0] * m[4] * m[8] + m[3] * m[7] * m[2] + m[6] * m[1] * m[5];
        float y = m[6] * m[4] * m[2] + m[0] * m[7] * m[5] + m[3] * m[1] * m[8];
        return x - y;
    }
};