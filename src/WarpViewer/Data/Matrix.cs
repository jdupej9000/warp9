using System;
using System.Runtime.InteropServices;

namespace Warp9.Data
{
    public class Matrix
    {
        private Matrix(float[] d)
        {
            data = d;
            Rows = d.Length;
            Columns = 1;
        }

        public Matrix(int cols, int rows)
        {
            Columns = cols;
            Rows = rows;
            data = new float[cols * rows];
        }

        private float[] data;

        public int Columns { get; init; }
        public int Rows { get; init; }
        public float[] Data => data;

        public Span<byte> GetRawData()
        {
            return MemoryMarshal.Cast<float, byte>(data.AsSpan());
        }

        public Span<float> GetColumn(int col)
        {
            if (col < 0 || col >= Columns)
                throw new IndexOutOfRangeException();

            return data.AsSpan().Slice(col * Rows, Rows);
        }

        public static Matrix FromVector(float[] d)
        {
            return new Matrix(d);
        }
    }
}