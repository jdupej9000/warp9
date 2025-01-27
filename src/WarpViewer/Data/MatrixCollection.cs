using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Warp9.Data
{
    public class MatrixCollection : Dictionary<int, Matrix>
    {
        public bool HasMatrix<T>(int key) where T:unmanaged
        {
            return TryGetValue(key, out Matrix? m) && m is Matrix<T>;
        }

        public bool TryGetMatrix<T>(int key, out Matrix<T>? m) where T : unmanaged
        {
            if (TryGetValue(key, out Matrix? mm) &&
                mm is Matrix<T> mt)
            {
                m = mt;
                return true;
            }

            m = null;
            return false;
        }

        public static readonly MatrixCollection Empty = new MatrixCollection();
    }
}
