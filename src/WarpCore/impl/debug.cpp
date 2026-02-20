#include "utils.h"
#include "vec_math.h"
#include <algorithm>
#include <stdio.h>
#include <inttypes.h>
#include "debug.h"

using namespace std;

namespace warpcore::impl
{
	void debug_matrix(const char* prefix, int index, const float* data, int rows, int cols)
	{
		constexpr size_t BUFF_SIZE = 512;
		char path[BUFF_SIZE];
		sprintf_s(path, BUFF_SIZE, "%s-%i.csv", prefix, index);

		FILE* f;
		fopen_s(&f, path, "w");

		if (f == nullptr)
			return;

		for (int j = 0; j < rows; j++) {
			for (int i = 0; i < cols; i++) {
				fprintf_s(f, "%f", data[j + i * rows]);

				if (i == cols - 1)
					fprintf_s(f, "\n");
				else
					fprintf_s(f, ",");
			}
		}

		fclose(f);
	}

	void debug_pcl(const char* prefix, int index, int iterartion, const float* data, int num_vert, bool soa)
	{
		constexpr size_t BUFF_SIZE = 512;
		char path[BUFF_SIZE];
		sprintf_s(path, BUFF_SIZE, "%s-%i-%i.xyz", prefix, index, iterartion);

		FILE* f;
		fopen_s(&f, path, "w");

		if (f == nullptr)
			return;

		if (soa) {
			for (int i = 0; i < num_vert; i++) {
				fprintf_s(f, "%f %f %f\n", data[i], data[num_vert + i], data[2 * num_vert + i]);
			}
		} else {
			for (int i = 0; i < num_vert; i++) {
				fprintf_s(f, "%f %f %f\n", data[3 * i], data[3 * i + 1], data[3 * i + 2]);
			}
		}

		fclose(f);
	}

	void debug_write_profile(const vector<long long>& t)
	{
		constexpr size_t BUFF_SIZE = 512;
		
		FILE* f;
		fopen_s(&f, "profile.csv", "a");

		for (size_t idx = 0; idx < t.size(); idx++) {

			if (idx != t.size() - 1) {
				fprintf_s(f, "%" PRId64 ",", t[idx]);
			} else {
				fprintf_s(f, "%" PRId64 "\n", t[idx]);
			}
		}
	
		fclose(f);
	}
}