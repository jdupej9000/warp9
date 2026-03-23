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
				float u = p3f_dist(p3f_set(srci), p3f_set(srcj));
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

		set_p(src);
		set_aw(b);

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

		init_m(m, src, n, idx, n);
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
				float u = p3f_dist(p3f_set(srci), p3f_set(srcj));
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

		set_p(src, idx);
		set_aw(b);

		delete[] m;
		delete[] b;
	}

	void tps3::fit_ls(const float* src, const float* dest, int n, const int* ctl_idx)
	{
		int nrow = 4 + n;
		int ncol = 4 + m_n; // num_ctl_indices = m_n

		// Construct the M matrix.
		float* m = new float[nrow * ncol];
		init_m(m, src, n, ctl_idx, m_n);

		// Construct the T matrix.
		float* t = new float[nrow * Dim];
		memcpy(t, dest, sizeof(float) * Dim * n);
		memset(t + Dim * n, 0, sizeof(float) * Dim * 4);

		// Solve for B.
		float* b = new float[ncol * Dim];
		if (!solve_ls_chol(b, m, t, nrow, ncol, Dim, true)) {
			throw std::exception{};
		}

		// Copy results to the TPS structure.
		set_p(src, ctl_idx);
		set_aw(b);

		delete[] m;
		delete[] t;
		delete[] b;
	}

	void tps3::set_aw(const float* b)
	{
		int ncol = 4 + m_n;

		for (int j = 0; j < Dim; j++) {
			memcpy(ptr_a() + j * 4, b + j * ncol, sizeof(float) * 4);
			memcpy(ptr_w() + j * m_n, b + 4 + j * ncol, sizeof(float) * m_n);
		}
	}

	void tps3::set_p(const float* p)
	{
		memcpy(ptr_p(), p, sizeof(float) * 3 * m_n);
	}

	void tps3::set_p(const float* p, const int* ctl_idx)
	{
		float* pp = ptr_p();
		for (int i = 0; i < m_n; i++) {
			int ii = Dim * ctl_idx[i];

			for (int j = 0; j < Dim; j++)
				pp[Dim * i + j] = p[ii + j];
		}
	}

	void tps3::init_m(float* m, const float* src, int n, const int* ctl_idx, int nctl)
	{
		// Note that this is not directly applicable to fit(..., idx) because it requires an indirection for both i,j. Here we do the indirection for j only.
		int nrow = 4 + n;
		int ncol = 4 + nctl;

		// TODO: flip the i, j loops to get rid of the strided writes
		memset(m, 0, sizeof(float) * nrow * ncol);
		for (int i = 0; i < n; i++) {
			p3f xi = p3f_set(src + Dim * i);

			m[i] = 1.0f;
			m[1 * nrow + i] = p3f_get<0>(xi);
			m[2 * nrow + i] = p3f_get<1>(xi);
			m[3 * nrow + i] = p3f_get<2>(xi);

			for (int j = 0; j < nctl; j++) {
				p3f xj = p3f_set(src + Dim * ctl_idx[j]);
				float u = p3f_dist(xi, xi);
				m[nrow * (4 + j) + i] = u * u * u;
			}
		}

		for (int j = 0; j < nctl; j++) {
			p3f xj = p3f_set(src + Dim * ctl_idx[j]);
			m[nrow * (4 + j) + n] = 1.0f;
			m[nrow * (4 + j) + n + 1] = p3f_get<0>(xj);
			m[nrow * (4 + j) + n + 2] = p3f_get<1>(xj);
			m[nrow * (4 + j) + n + 3] = p3f_get<2>(xj);
		}
	}
};