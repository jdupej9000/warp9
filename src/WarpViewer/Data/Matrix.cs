using System;
using System.Runtime.InteropServices;

namespace Warp9.Data
{
    public class Matrix
    {
        public Matrix(int cols, int rows)
        {
            Columns = cols;
            Rows = rows;
            data = new float[cols * rows];
        }

        private float[] data;

        public int Columns { get; init; }
        public int Rows { get; init; }

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
    }
}