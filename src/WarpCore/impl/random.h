#pragma once

#include "../config.h"

namespace warpcore::impl
{

	uint64_t rand_murmur(uint64_t seed);
	uint64_t rand_splitmix(uint64_t& state);
	uint64_t rand_wyhash(uint64_t& state);
	uint64_t rand_lehmer64(uint64_t& state);
	uint32_t rand_xorshift32(uint64_t& state);

};