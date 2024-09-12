#include "random.h"

#ifdef _MSC_VER
#include <intrin.h>
#endif 

namespace warpcore::impl
{

    uint64_t rand_murmur(uint64_t seed)
    {
        //https://lemire.me/blog/2023/10/17/randomness-in-programming-with-go-code/
        uint64_t h = seed;
        h ^= h >> 33;
        h *= 0xff51afd7ed558ccd;
        h ^= h >> 33;
        h *= 0xc4ceb9fe1a85ec53;
        h ^= h >> 33;
        return h;
    }

    uint64_t rand_splitmix(uint64_t& state)
    {
        //https://lemire.me/blog/2023/10/17/randomness-in-programming-with-go-code/
        state += 0x9E3779B97F4A7C15;
        uint64_t z = state;
        z = (z ^ (z >> 30));
        z *= (0xBF58476D1CE4E5B9);
        z = (z ^ (z >> 27));
        z *= (0x94D049BB133111EB);
        return z ^ (z >> 31);
    }

    uint64_t rand_wyhash(uint64_t& state)
    {
        //https://lemire.me/blog/2021/03/17/apples-m1-processor-and-the-full-128-bit-integer-product/

        state += 0x60bee2bee120fc15ull;
#ifdef _MSC_VER
        uint64_t hi, lo;
        lo = _mul128(state, 0xa3b195354a39b70dull, (long long*)&hi);
        uint64_t m1 = hi ^ lo;
        lo = _mul128(m1, 0x1b03738712fad5c9ull, (long long*)&hi);
        return hi ^ lo;
#else
        __uint128_t x = (__uint128_t)state * 0xa3b195354a39b70dull;
        uint64_t m1 = (x >> 64) ^ x;
        x = (__uint128_t)m1 * 0x1b03738712fad5c9ull; 
        return (x >> 64) ^ x;
#endif
    }

    uint64_t rand_lehmer64(uint64_t& state)
    {
        // https://lemire.me/blog/2019/03/19/the-fastest-conventional-random-number-generator-that-can-pass-big-crush/
       
#ifdef _MSC_VER
        uint64_t hi;
        state = _mul128(state, 0xda942042e4dd58b5ull, (long long*)&hi);
        return hi;
#else
        __uint128_t x = (__uint128_t)state * 0xda942042e4dd58b5ull;
        state = x;
        return x >> 64;
#endif
    }

    uint32_t rand_xorshift32(uint64_t& state)
    {
        // https://lemire.me/blog/2018/07/02/predicting-the-truncated-xorshift32-random-number-generator/
        uint64_t result = state * 0xd989bcacc137dcd5ull;
        state ^= state >> 11;
        state ^= state << 31;
        state ^= state >> 18;
        return (uint32_t)(result >> 32ull);
    }

};