using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

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
            int accum = 0, pos = 0, retpos = 0;
            for (int rep = 0; rep < repeat; rep++)
            {
                for (int i = 0; i < data.Length; i++)
                {
                    if (data[i])
                        accum |= 1 << pos;

                    pos++;

                    if (pos == 32)
                    {
                        mask[retpos++] = accum;
                        accum = 0;
                        pos = 0;
                    }
                }
            }

            if (pos != 0)
                mask[retpos] = accum;
        }
    }
}
