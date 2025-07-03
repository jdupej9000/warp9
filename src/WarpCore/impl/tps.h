#pragma once

#include "../p3f.h"

namespace warpcore::impl
{
	class tps3d
	{
	public:
		tps3d(int n);
		~tps3d(void);

	private:
		float* m_data;
		int m_n;

	public:
		int num_pts(void) const noexcept { return m_n; }
		float* ptr_p(void) noexcept { return m_data; }
		float* ptr_w(void) noexcept { return m_data + 3 * m_n; }
		float* ptr_a(void) noexcept { return m_data + 6 * m_n; }

		p3f transform(p3f x, bool noaffine=false) noexcept;
		void transform_soa(float* x, int n, const void* allow, bool noaffine = false, bool neg = false) noexcept;
	};

	void tps_fit3d_soa(tps3d* tps, const float* src, const float* dest);
};