#include "tps.h"
#include <math.h>
#include "../config.h"
#include <lapacke.h>
#include <cblas.h>
#include "utils.h"

namespace warpcore::impl
{
	tps3::tps3(int n)
	{
		m_n = n;
		m_data = new float[2 * 3 * m_n + 4 * 3];
	}

	tps3::tps3(tps3&& other) :
		m_n(other.m_n),
		m_data(other.m_data)
	{
		other.m_data = nullptr;
	}

	tps3::~tps3(void)
	{
		if (m_data != nullptr)
			delete[] m_data;
	}

	p3f tps3::transform(p3f x, bool noaffine) noexcept
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
			float d = p3f_dist(p3f_set(p + 3 * i), x);
			float u = d * d * d;
			ret = p3f_fma(u, p3f_set(w[i], w[i + n], w[i + 2 * n]), ret);
		}

		return ret;
	}

	// If allow[i] != neg, transforms x[i] and saves y[i] for i=0..m. x==y is permitted
	void tps3::transform(float* y, const float* x, int m, const void* allow, bool noaffine, bool neg) noexcept
	{
		if (allow != nullptr) {
			const int32_t* allow_mask = (const int32_t*)allow;
			uint32_t toggle = neg ? 0xffffffff : 0x0;

			// TODO: optimize
			for (int i = 0; i < m; i++) {
				if (((allow_mask[i >> 5] ^ toggle) >> (i & 0x1f)) & 0x1) {
					const float* row = x + 3 * i;
					p3f transformed = transform(p3f_set(row), noaffine);

					p3f_store(y + 3 * i, transformed);
				}
			}
		} else {
			for (int i = 0; i < m; i++) {
				const float* row = x + 3 * i;
				p3f transformed = transform(p3f_set(row), noaffine);
				p3f_store(y + 3 * i, transformed);				
			}
		}
	}

	void tps3::fit(const float* src, const float* dest)
	{
		const int n = m_n;
		const int n4 = n + 4;

		float* m = new float[n4 * n4];
		float* b = new float[n4 * 3];
		memset(m, 0, sizeof(float) * n4 * n4);
		memset(b, 0, sizeof(float) * 3 * n4);

		for (int i = 0; i < n; i++) {
			const float* srci = src + 3 * i;

			m[i] = 1;
			m[n4 + i] = srci[0];
			m[2 * n4 + i] = srci[1];
			m[3 * n4 + i] = srci[2];

			m[n + n4 * (4 + i)] = 1;
			m[n + 1 + n4 * (4 + i)] = srci[0];
			m[n + 2 + n4 * (4 + i)] = srci[1];
			m[n + 3 + n4 * (4 + i)] = srci[2];

			for (int j = i; j < n; j++) {
				const float* srcj = src + 3 * j;

				float dx = srci[0] - srcj[0];
				float dy = srci[1] - srcj[1];
				float dz = srci[2] - srcj[2];
				float u = sqrtf(dx * dx + dy * dy + dz * dz);
				u = u * u * u;

				m[i + n4 * (4 + j)] = u;
				m[j + n4 * (4 + i)] = u;
			}

			const float* desti = dest + 3 * i;
			b[i] = desti[0];
			b[i + n4] = desti[1];
			b[i + 2 * n4] = desti[2];
		}

		int* piv = new int[n4];
		memset(piv, 0, n4 * sizeof(int));
		LAPACKE_sgesv(LAPACK_COL_MAJOR, n4, 3, m, n4, piv, b, n4);
		delete[] piv;

		memcpy(ptr_p(), src, sizeof(float) * 3 * n);

		memcpy(ptr_w(), b + 4, sizeof(float) * n);
		memcpy(ptr_w() + n, b + 4 + n4, sizeof(float) * n);
		memcpy(ptr_w() + 2 * n, b + 4 + 2 * n4, sizeof(float) * n);

		memcpy(ptr_a(), b, sizeof(float) * 4);
		memcpy(ptr_a() + 4, b + n4, sizeof(float) * 4);
		memcpy(ptr_a() + 8, b + 2 * n4, sizeof(float) * 4);

		delete[] m;
		delete[] b;
	}

	void tps3::fit(int src_len, const float* src, const float* dest, const int* idx)
	{
		const int n = m_n;
		const int n4 = n + 4;

		float* m = new float[n4 * n4];
		float* b = new float[n4 * 3];
		memset(m, 0, sizeof(float) * n4 * n4);
		memset(b, 0, sizeof(float) * 3 * n4);

		for (int i = 0; i < n; i++) {
			int ii = idx[i];
			assert(ii < src_len);

			const float* srci = src + 3 * ii;

			m[i] = 1;
			m[n4 + i] = srci[0];
			m[2 * n4 + i] = srci[1];
			m[3 * n4 + i] = srci[2];

			m[n + n4 * (4 + i)] = 1;
			m[n + 1 + n4 * (4 + i)] = srci[0];
			m[n + 2 + n4 * (4 + i)] = srci[1];
			m[n + 3 + n4 * (4 + i)] = srci[2];

			for (int j = i; j < n; j++) {
				const float* srcj = src + 3 * idx[j];

				float dx = srci[0] - srcj[0];
				float dy = srci[1] - srcj[1];
				float dz = srci[2] - srcj[2];
				float u = sqrtf(dx * dx + dy * dy + dz * dz);
				u = u * u * u;

				m[i + n4 * (4 + j)] = u;
				m[j + n4 * (4 + i)] = u;
			}

			const float* desti = dest + 3 * idx[i];
			b[i] = desti[0];
			b[i + n4] = desti[1];
			b[i + 2 * n4] = desti[2];
		}

		int* piv = new int[n4];
		memset(piv, 0, n4 * sizeof(int));
		LAPACKE_sgesv(LAPACK_COL_MAJOR, n4, 3, m, n4, piv, b, n4);
		delete[] piv;

		float* p = ptr_p();
		for (int i = 0; i < n; i++) {
			int ii = 3 * idx[i];
			p[3 * i] = src[ii];
			p[3 * i + 1] = src[ii + 1];
			p[3 * i + 2] = src[ii + 2];
		}
		
		memcpy(ptr_w(), b + 4, sizeof(float) * n);
		memcpy(ptr_w() + n, b + 4 + n4, sizeof(float) * n);
		memcpy(ptr_w() + 2 * n, b + 4 + 2 * n4, sizeof(float) * n);

		memcpy(ptr_a(), b, sizeof(float) * 4);
		memcpy(ptr_a() + 4, b + n4, sizeof(float) * 4);
		memcpy(ptr_a() + 8, b + 2 * n4, sizeof(float) * 4);

		delete[] m;
		delete[] b;
	}
};