#pragma once

#include "../config.h"

namespace warpcore::impl
{
    int find_line_end(const char* data, int size);
    int find_first(const char* data, int size, char ch);
    int skip_float(const char* data, int size);
    int skip_int(const char* data, int size);
    int skip_seq(const char* data, int size, char skip);
};