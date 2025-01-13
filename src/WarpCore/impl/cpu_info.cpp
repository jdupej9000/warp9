#include "cpu_info.h"
#include <intrin.h>
#include <memory.h>

namespace warpcore::impl
{
	static OPT_PATH g_optpath;
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
		g_optpath = has_avx512 ? OPT_PATH::AVX512 : OPT_PATH::AVX2;

		// Extract brand string
		__cpuid(regs, 0x80000002);
		memcpy(g_cpuname, regs, sizeof(regs));
		__cpuid(regs, 0x80000003);
		memcpy(g_cpuname + 16, regs, sizeof(regs));
		__cpuid(regs, 0x80000004);
		memcpy(g_cpuname + 32, regs, sizeof(regs));
	}

	OPT_PATH get_optpath(void)
	{
		return g_optpath;
	}

	const char* get_cpuname(void)
	{
		return g_cpuname;
	}
};