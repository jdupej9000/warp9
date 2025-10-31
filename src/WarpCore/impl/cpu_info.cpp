#include "cpu_info.h"
#include <intrin.h>
#include <memory.h>

namespace warpcore::impl
{
	static WCORE_OPTPATH g_optpath, g_platform_optpath;
	static char g_cpuname[48];

	constexpr static bool is_bit(int v, int i)
	{
		return v & (1 << i);
	}

	void init_cpuinfo(void)
	{
		int features = 0;

		int regs[4];
		__cpuidex(regs, 7, 0);
		if (is_bit(regs[1], 5))
			features |= (int)WCORE_OPTPATH::AVX2;

		if (is_bit(regs[1], 16)) // TODO: check more than avx512.f
			features |= (int)WCORE_OPTPATH::AVX512;

		if (is_bit(regs[3], 15)) // TODO: check more than avx512.f
			features |= (int)WCORE_OPTPATH::HYBRID;

		g_platform_optpath = (WCORE_OPTPATH)features;
		g_optpath = (WCORE_OPTPATH)features;

		// Extract brand string
		__cpuid(regs, 0x80000002);
		memcpy(g_cpuname, regs, sizeof(regs));
		__cpuid(regs, 0x80000003);
		memcpy(g_cpuname + 16, regs, sizeof(regs));
		__cpuid(regs, 0x80000004);
		memcpy(g_cpuname + 32, regs, sizeof(regs));
	}

	WCORE_OPTPATH get_optpath(void)
	{
		return g_optpath;
	}

	bool has_feature(WCORE_OPTPATH f)
	{
		return ((int)g_optpath & (int)f) == (int)f;
	}

	WCORE_OPTPATH restrict_optpath(WCORE_OPTPATH path)
	{
		g_optpath = (WCORE_OPTPATH)((int)path & (int)g_platform_optpath);

		return g_optpath;
	}

	const char* get_cpuname(void)
	{
		return g_cpuname;
	}
};