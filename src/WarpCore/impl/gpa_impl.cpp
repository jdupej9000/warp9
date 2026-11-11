#include "gpa_impl.h"
#include "vec_math.h"
#include "pcl_utils.h"
#include "utils.h"
#include <immintrin.h>
#include <cmath>
#include <cstring>
#include <exception>
#include <Eigen>

using namespace Eigen;

namespace warpcore::impl
{
    void opa_cov(const float* x, const float* y, int d, int m, const float* xoff, float xcs, float* cov);
    void opa_cov(const float* x, const float* y, const void* allow, int d, int m, const float* xoff, float xcs, float* cov);
    float mat3_det(const float* m);
    void mat3_transpose(float* m);
    void extract_mat3_transpose(float* dest, __m128 row0, __m128 row1, __m128 row2);
  
    void opa_cov(const float* x, const float* y, int d, int m, const float* xoff, float xcs, float* cov)
    {
        __m128 offset = _mm_loadu_ps(xoff);
        __m128 rcs = _mm_set1_ps(1.0f / xcs);
        __m128 cov0 = _mm_setzero_ps(), cov1 = _mm_setzero_ps(), cov2 = _mm_setzero_ps();

        for (int i = 0; i < m; i++) {
            __m128 xi = _mm_mul_ps(rcs, _mm_sub_ps(_mm_loadu_ps(x + 3 * i), offset));
            __m128 yi = _mm_loadu_ps(y + 3 * i);

            cov0 = _mm_fmadd_ps(yi, _mm_shuffle_ps(xi, xi, 0b00000000), cov0);
            cov1 = _mm_fmadd_ps(yi, _mm_shuffle_ps(xi, xi, 0b01010101), cov1);
            cov2 = _mm_fmadd_ps(yi, _mm_shuffle_ps(xi, xi, 0b10101010), cov2);
        }

        extract_mat3_transpose(cov, cov0, cov1, cov2);
    }

    void opa_cov(const float* x, const float* y, const void* allow, int d, int m, const float* xoff, float xcs, float* cov)
    {
        __m128 offset = _mm_loadu_ps(xoff);
        __m128 rcs = _mm_set1_ps(1.0f / xcs);
        __m128 cov0 = _mm_setzero_ps(), cov1 = _mm_setzero_ps(), cov2 = _mm_setzero_ps();

        int dummy = 0;
        FOR_MASKED(i, m, allow, dummy, false, {
            __m128 xi = _mm_mul_ps(rcs, _mm_sub_ps(_mm_loadu_ps(x + 3 * i), offset));
            __m128 yi = _mm_loadu_ps(y + 3 * i);

            cov0 = _mm_fmadd_ps(yi, _mm_shuffle_ps(xi, xi, 0b00000000), cov0);
            cov1 = _mm_fmadd_ps(yi, _mm_shuffle_ps(xi, xi, 0b01010101), cov1);
            cov2 = _mm_fmadd_ps(yi, _mm_shuffle_ps(xi, xi, 0b10101010), cov2);
        });
       
        extract_mat3_transpose(cov, cov0, cov1, cov2);
    }

    int opa_fit(const float* x, const float* y, const void* allow, int d, int m, float* xoffs, float* xcs, float* rot)
    {
        WCORE_ASSERT(d == 3); // static cov size

        float cs = 0;
        if (allow) {
            pcl_center(x, allow, d, m, xoffs);
            cs = pcl_cs(x, allow, d, m, xoffs);
        }
        else {
            pcl_center(x, d, m, xoffs);
            cs = pcl_cs(x, d, m, xoffs);
        }

        *xcs = cs;

        // Kabsch algorithm follows.
        float cov[9];
        memset(cov, 0, sizeof(float) * 9);

        if (allow) {
            opa_cov(x, y, allow, d, m, xoffs, cs, cov);
        }
        else {
            opa_cov(x, y, d, m, xoffs, cs, cov);
        }

        using Mat3 = Matrix<float, 3, 3, ColMajor>;     
        Map<const Mat3> C(cov);
        JacobiSVD<Mat3> svd(C, ComputeFullU | ComputeFullV);

        if (svd.info() != Success) {
            return -1;
        }

        Mat3 U = svd.matrixU();
        Mat3 V = svd.matrixV();

        float detuv = U.determinant() * V.determinant();
        U.col(2) *= detuv;

        Mat3 H = U * V.transpose();       
        Map<Mat3> R(rot);
        R = H.transpose();

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

        scale(mean, 1.0f / n, d * m);
    }

    void rigid_combine(rigid3* ret, const rigid3* f, const rigid3* g)
    {
        const float* H = g->rot;
        const float* R = f->rot;

        // returns h(x) = g(f(x))
        ret->cs = f->cs * g->cs;

        float csr = ret->cs;     
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

    void mat3_transpose(float* m)
    {
        // move as integers
        int* mm = (int*)m;
        int t = 0;
        #define SWAP(i,j) { t = mm[i]; mm[i] = mm[j]; mm[j] = t;}
        
        SWAP(1, 3);
        SWAP(2, 6);
        SWAP(5, 7);
    }

    void extract_mat3_transpose(float* dest, __m128 row0, __m128 row1, __m128 row2)
    {
        int* icov = (int*)dest;
        icov[0] = _mm_extract_ps(row0, 0);
        icov[1] = _mm_extract_ps(row1, 0);
        icov[2] = _mm_extract_ps(row2, 0);
        icov[3] = _mm_extract_ps(row0, 1);
        icov[4] = _mm_extract_ps(row1, 1);
        icov[5] = _mm_extract_ps(row2, 1);
        icov[6] = _mm_extract_ps(row0, 2);
        icov[7] = _mm_extract_ps(row1, 2);
        icov[8] = _mm_extract_ps(row2, 2);
    }
};