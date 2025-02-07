using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Warp9.Utils
{
    public ref struct StringLineEnumerator
    {
        public StringLineEnumerator(ReadOnlySpan<char> text)
        {
            buffer = text;
        }

        ReadOnlySpan<char> buffer;

        public ReadOnlySpan<char> Current { get; private set; }

        public StringLineEnumerator GetEnumerator()
        {
            return this;
        }

        public bool MoveNext()
        {
            if (buffer.Length == 0) 
                return false;

            int index = buffer.IndexOfAny('\r', '\n');
            if (index == -1) 
            {
                Current = buffer;
                buffer = ReadOnlySpan<char>.Empty;                
                return true;
            }

            if (index < buffer.Length - 1 && buffer[index] == '\r' && buffer[index + 1] == '\n')
            {
                Current = buffer.Slice(0, index);
                buffer = buffer.Slice(index + 2);
                return true;
            }

            Current = buffer.Slice(0, index);
            buffer = buffer.Slice(index + 1);
            return true;
        }
    }
}
