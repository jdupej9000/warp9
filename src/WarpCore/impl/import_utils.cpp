#include "import_utils.h"

namespace warpcore::impl
{
    int find_line_end(const char* data, int size)
    {
        return find_first(data, size, '\n');
    }

    int find_first(const char* data, int size, char ch)
    {
        int i = 0;
        while(i < size) {
            if(data[i] == ch) return i;
            i++;
        }
        return -1;
    }

    int skip_float(const char* data, int size)
    {
        int i = 0;

        while(i < size) {
            const char ch = data[i];
            if(!(ch >= '0' && ch <= '9') && ch != '.' && ch !='-')
                return i;

            i++;
        }

        return 0;
    }

    int skip_int(const char* data, int size)
    {
        int i = 0;

        while(i < size) {
            const char ch = data[i];
            if(!(ch >= '0' && ch <= '9') && ch != '-')
                return i;

            i++;
        }

        return 0;
    }

    int skip_seq(const char* data, int size, char skip)
    {
        int i = 0;

        while(i < size) {
            const char ch = data[i];
            if(ch != skip)
                return i;

            i++;
        }

        return 0;
    }
};