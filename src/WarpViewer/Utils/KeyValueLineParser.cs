using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Warp9.Utils
{
    public ref struct KeyValueLineParser
    {
        public KeyValueLineParser(ReadOnlySpan<char> line)
        {
            Line = line;
            ptr = 0;
        }

        public ReadOnlySpan<char> Line;

        private int ptr;

        public bool TryGetNextToken(out ReadOnlySpan<char> key, out ReadOnlySpan<char> value)
        {
            int p0 = ptr;
            int n = Line.Length;

            key = default;
            value = default;

            while (p0 < n && IsWhite(Line[p0]))
                p0++;

            if (p0 >= n)
            {
                ptr = n;
                return false;
            }

            int p1 = p0;
            while (p1 < n && char.IsAsciiLetterOrDigit(Line[p1]))
                p1++;

            key = Line.Slice(p0, p1 - p0);
            if (Line[p1] != '=')
            {
                ptr = p1;
                return true;
            }

            p1++; // skip '='

            if (Line[p1] == '\"')
            {
                p1++;
                int p2 = p1;

                while (p2 < n && Line[p2] != '\"')
                    p2++;

                ptr = p2 + 1;
                value = Line.Slice(p1, p2 - p1);
                return true;
            }
            else
            {
                int p2 = p1;
                while (p2 < n && !IsWhite(Line[p2]))
                    p2++;

                ptr = p2 + 1;
                value = Line.Slice(p1, p2 - p1);
                return true;
            }

            // NOTREACHED
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsWhite(char c)
        {
            return c == ' ' || c == '\t';
        }

    }
}
