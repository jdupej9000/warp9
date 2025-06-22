using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Warp9.Model
{
    public class ReferencedData<T> where T : class
    {
        public ReferencedData(long key)
        {
            Key = key;
            Value = null;
        }

        public ReferencedData(T val, long key = -1)
        {
            Key = -1;
            Value = val;
        }

        public long Key { get; set; } = -1;
        public T? Value { get; set; }

        public bool IsLoaded => Value is not null;
    }
}
