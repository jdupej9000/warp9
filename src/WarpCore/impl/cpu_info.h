#pragma once

namespace warpcore::impl
{
	enum class OPT_PATH : int
	{
		AVX2 = 0,
		AVX512 = 1
	};

	void init_cpuinfo(void);
	OPT_PATH get_optpath(void);
	const char* get_cpuname(void);
}