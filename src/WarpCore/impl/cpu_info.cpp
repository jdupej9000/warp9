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
		int regs[4];

		// Check support for AVX512
		__cpuidex(regs, 7, 0);
		bool has_avx512 = is_bit(regs[1], 16); // TODO: check more than avx512.f
		g_platform_optpath = g_optpath = 
			has_avx512 ? WCORE_OPTPATH::AVX512 : WCORE_OPTPATH::AVX2;

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

	WCORE_OPTPATH restrict_optpath(WCORE_OPTPATH path)
	{
		if (path > g_platform_optpath)
			g_optpath = g_platform_optpath;
		else
			g_optpath = path;

		return g_optpath;
	}

	const char* get_cpuname(void)
	{
		return g_cpuname;
	}
};