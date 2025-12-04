#pragma once

#include "../p3f.h"

namespace warpcore::impl
{
	class tps3
	{
	public:
		tps3(int n);
		tps3(tps3&& other);
		~tps3(void);

	private:
		float* m_data;
		int m_n;

	public:
		int num_pts(void) const noexcept { return m_n; }
		float* ptr_p(void) noexcept { return m_data; }
		float* ptr_w(void) noexcept { return m_data + 3 * m_n; }
		float* ptr_a(void) noexcept { return m_data + 6 * m_n; }

		p3f transform(p3f x, bool noaffine=false) noexcept;
		void transform(float* y, const float* x, int n, const void* allow, bool noaffine = false, bool neg = false) noexcept;

		void fit(const float* src, const float* dest);
		void fit(int src_len, const float* src, const float* dest, const int* idx);
	};
};