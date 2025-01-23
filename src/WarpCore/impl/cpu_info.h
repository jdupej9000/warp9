#pragma once

#include "../defs.h"

namespace warpcore::impl
{
	void init_cpuinfo(void);
	WCORE_OPTPATH get_optpath(void);
	WCORE_OPTPATH restrict_optpath(WCORE_OPTPATH path);
	const char* get_cpuname(void);
}