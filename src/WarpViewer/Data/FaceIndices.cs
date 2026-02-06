using System;
using System.Runtime.InteropServices;

namespace Warp9.Data
{
    [StructLayout(LayoutKind.Sequential)]
    public struct FaceIndices
    {
        public FaceIndices(int i0, int i1, int i2)
        {
            I0 = i0;
            I1 = i1;
            I2 = i2;
        }

        public int I0;
        public int I1;
        public int I2;

        public int this[int idx]
        {
            get 
            {
                return idx switch
                {
                    0 => I0,
                    1 => I1,
                    2 => I2,
                    _ => throw new IndexOutOfRangeException()
                };
            }
            set
            {
                switch(idx)
                {
                    case 0: I0 = value; break;
                    case 1: I1 = value; break;
                    case 2: I2 = value; break;
                    default: throw new IndexOutOfRangeException();
                }
            }
        }

        public readonly bool IsDegenerate()
        {
            return I0 == I1 || I0 == I2 || I1 == I2;
        }
    }
}
