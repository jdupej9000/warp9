#include "tps.h"
#include <math.h>
#include "../config.h"
#include <lapacke.h>
#include <cblas.h>
#include "utils.h"

namespace warpcore::impl
{
	tps3d::tps3d(int n)
	{
		m_data = new float[2 * 3 * m_n + 4 * 3];
	}

	tps3d::~tps3d(void)
	{
		if (m_data != nullptr)
			delete[] m_data;
	}

	p3f tps3d::transform(p3f x, bool noaffine) noexcept
	{
		p3f ret = x;

		if (!noaffine) {
			const float* a = ptr_a();
			ret = p3f_set(
				a[0] + p3f_dot(x, p3f_set(a + 1)),
				a[4] + p3f_dot(x, p3f_set(a + 5)),
				a[8] + p3f_dot(x, p3f_set(a + 9)));
		}

		// TODO: optimize
		const float* p = ptr_p();
		const float* w = ptr_w();
		int n = num_pts();
		for (int i = 0; i < n; i++) {			
			float d = p3f_dist(p3f_set(p[i], p[i + n], p[i + 2 * n]), x);
			float u = d * d * d;
			ret = p3f_fma(u, p3f_set(p[i], p[i + n], p[i + 2 * n]), ret);
		}

		return ret;
	}

	void tps3d::transform_soa(float* x, int m, const void* allow, bool noaffine, bool neg) noexcept
	{
		const int32_t* allow_mask = (const int32_t*)allow;
		uint32_t toggle = neg ? 0xffffffff : 0x0;

		// TODO: optimize
		alignas(16) float row[4];
		for (int i = 0; i < m; i++) {
			if (((allow_mask[i >> 5] ^ neg) >> (i & 0x1f)) & 0x1) {
				get_row<float, 3>(x, m, i, row);
				p3f transformed = transform(p3f_set(row), noaffine);
				_mm_storeu_ps(row, transformed);
				put_row<float, 3>(x, m, i, row);
			}
		}
	}

	/*void tps3d::transform_soa(float* x, int m, bool noaffine) noexcept
	{
		if (!noaffine) {
			const float* a = ptr_a();
			cblas_sgemm(CblasColMajor, CblasNoTrans, CblasNoTrans,
				m, 3, 3,
				1.0f, x, m, a + 1, 4, 0.0f,
				x, m);

			add(x, a[0], m);
			add(x + m, a[4], m);
			add(x + 2 * m, a[8], m);
		}

		const float* p = ptr_p();
		const float* w = ptr_w();
		int n = num_pts();
		for (int i = 0; i < m; i++) {
			float d = p3f_dist(p3f_set(p[i], p[i + n], p[i + 2 * n]), x);
			float u = d * d * d;
			ret = p3f_fma(u, p3f_set(p[i], p[i + n], p[i + 2 * n]), ret);
		}
	}*/

	void tps_fit3d_soa(tps3d* tps, const float* src, const float* dest)
	{
		int n = tps->num_pts();
		int n4 = n + 4;

		float* m = new float[n4 * n4];
		float* b = new float[n4 * 3];
		float* rhs = new float[n4 * 3];

		for (int i = 0; i < n; i++) {
			m[i] = 1;
			m[n4 + i] = src[i];
			m[2 * n4 + i] = src[n + i];
			m[3 * n4 + i] = src[2 * n + i];

			m[n + n4 * (4 + i)] = 1;
			m[n + 1 + n4 * (4 + i)] = src[i];
			m[n + 2 + n4 * (4 + i)] = src[n + i];
			m[n + 3 + n4 * (4 + i)] = src[2 * n + i];

			for (int j = i; j < n; j++) {
				float dx = src[i] - src[j];
				float dy = src[i + n] - src[j + n];
				float dz = src[i + 2 * n] - src[j + 2 * n];
				float u = sqrtf(dx * dx + dy * dy + dz * dz);
				u = u * u * u;

				m[i + n4 * (4 + j)] = u;
				m[j + n4 * (4 + i)] = u;
			}

			b[i] = dest[i];
			b[i + n4] = dest[i + n];
			b[i + 2 * n4] = dest[i + 2 * n];
		}

		int* piv = new int[n4];
		std::memset(piv, 0, n4 * sizeof(int));
		LAPACKE_sgesv(LAPACK_COL_MAJOR, n4, 3, b, n4, piv, rhs, n4);
		delete[] piv;

		memcpy(tps->ptr_p(), src, sizeof(float) * 3 * n);

		memcpy(tps->ptr_w(), rhs + 4, sizeof(float) * n);
		memcpy(tps->ptr_w() + n, rhs + 4 + n4, sizeof(float) * n);
		memcpy(tps->ptr_w() + 2 * n, rhs + 4 + 2 * n4, sizeof(float) * n);

		memcpy(tps->ptr_a(), rhs, sizeof(float) * 4);
		memcpy(tps->ptr_a() + 4, rhs + n4, sizeof(float) * 4);
		memcpy(tps->ptr_a() + 8, rhs + 2 * n4, sizeof(float) * 4);

		delete[] rhs;
		delete[] m;
		delete[] b;
	}
};