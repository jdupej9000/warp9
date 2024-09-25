#include "misc.h"
#include <sstream>
#include <cstring>
#include <mkl.h>
#include <string.h>

using namespace std;

extern "C" int wcore_get_info(int index, char* buffer, int bufferSize)
{
    stringstream ss{};

    switch (index) {
    case WCINFO_VERSION:
        ss << "warpcore 0.1";
        break;

    case WCINFO_COMPILER:
#if defined(_MSC_FULL_VER)
        ss << "MSVC++ " << ((_MSC_FULL_VER / 10000000) % 100) << "." << 
            ((_MSC_FULL_VER / 100000) % 100) << "."
            << (_MSC_FULL_VER % 100000);
#else
        ss << "unknown";
#endif
        break;

    case WCINFO_OPT_PATH:
        ss << "avx2";
        break;

    case WCINFO_MKL_VERSION:
    case WCINFO_MKL_ISA: {
        MKLVersion mkl_version;
        ::mkl_get_version(&mkl_version);

        if(index == WCINFO_MKL_VERSION) {
        ss << "oneMKL " << mkl_version.MajorVersion << "." << 
            mkl_version.MinorVersion << "." <<
            mkl_version.UpdateVersion;
        } else if(index == WCINFO_MKL_ISA) {
            ss << mkl_version.Processor;
        }
        }
        break;
    
    default:
        ss << "Invalid index.";
        break;
    }

    size_t sslen = ss.tellp();
    
    if(bufferSize > 0)
        strncpy_s(buffer, bufferSize, ss.str().c_str(), std::min(sslen, (size_t)bufferSize));
    
    return (int)sslen;
}