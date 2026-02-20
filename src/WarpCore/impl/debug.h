#pragma once

#include <vector>
#include <chrono>

#define PROFILE_START std::vector<long long> __prof{}; std::chrono::high_resolution_clock::time_point __t0, __t1;
#define PROFILE(fun) __t0=std::chrono::high_resolution_clock::now(); fun; __t1=std::chrono::high_resolution_clock::now(); __prof.push_back(std::chrono::duration_cast<std::chrono::nanoseconds>(__t1-__t0).count());
#define PROFILE_END debug_write_profile(__prof)

namespace warpcore::impl
{
	void debug_matrix(const char* prefix, int index, const float* data, int rows, int cols);
	void debug_pcl(const char* prefix, int index, int iterartion, const float* data, int num_vert, bool soa);
	void debug_write_profile(const std::vector<long long>& t);
}