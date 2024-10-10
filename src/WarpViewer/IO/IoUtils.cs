using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Warp9.IO
{
    public static class IoUtils
    {

        public static int Skip(string s, int pos, char ch = ' ')
        {
            while (pos < s.Length)
            {
                if (s[pos] != ch) return pos;
                pos++;
            }

            return pos;
        }

        public static int SkipInt(string s, int pos)
        {
            while (pos < s.Length)
            {
                if (s[pos] < '0' || s[pos] > '9') return pos;
                pos++;
            }

            return pos;
        }

        public static int SkipNonInt(string s, int pos)
        {
            while (pos < s.Length)
            {
                if (s[pos] >= '0' && s[pos] <= '9') return pos;
                pos++;
            }

            return pos;
        }

        public static int SkipAllBut(string s, int pos, char ch)
        {
            while (pos < s.Length)
            {
                if (s[pos] == ch) return pos;
                pos++;
            }

            return pos;
        }

        public static int ParseSeparatedFloats(string line, int offset, char sep, Span<float> vec)
        {
            int len = line.Length;

            int num = 0;
            int pos = offset;
            while (pos < len)
            {
                pos = Skip(line, pos, sep);
                if (pos == len) 
                    break;

                int pos2 = SkipAllBut(line, pos, sep);
                if (!float.TryParse(line.AsSpan(pos, pos2 - pos), CultureInfo.InvariantCulture, out vec[num]))
                    break;

                num++;
                pos = pos2;

                if (num >= vec.Length || pos == len)
                    break;
            }

            return num;
        }
    }
}
