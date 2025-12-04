using System;

namespace Warp9.Utils
{
    public static class BitMask
    {
        public static int GetArraySize(int numBits)
        {
            return (numBits + 31) / 32;
        }

        public static int[] MakeBitMask(ReadOnlySpan<bool> data, int repeat = 1)
        {
            int len = GetArraySize(data.Length * repeat);
            int[] ret = new int[len];

            MakeBitMask(ret.AsSpan(), data, repeat);

            return ret;
        }

        public static void MakeBitMask(Span<int> mask, ReadOnlySpan<bool> data, int repeat = 1)
        {
            if (repeat < 1 || repeat > 32)
                throw new ArgumentOutOfRangeException();

            long accum = 0;
            long one = (1L << repeat) - 1;
            int n = data.Length;
            int cached = 0;
            int ptr = 0;

            for (int i = 0; i < n; i++)
            {
                if(data[i])
                    accum |= one << cached;

                cached += repeat;

                if (cached >= 32)
                {
                    mask[ptr] = (int)(accum & 0xffffffff);
                    accum >>= 32;
                    cached -= 32;
                    ptr++;
                }
            }

            if(cached != 0)
                mask[ptr] = (int)(accum & 0xffffffff);
        }
    }
}
