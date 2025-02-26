using System;
using System.Runtime.InteropServices;

namespace Warp9.Data
{
    public abstract class Matrix
    {
        protected Matrix(int cols, int rows, Type type)
        {
            Columns = cols;
            Rows = rows;
            ElementType = type;
        }

        public int Columns { get; init; }
        public int Rows { get; init; }
        public Type ElementType { get; init; }

        public abstract Span<byte> GetRawData();
    }

    public class Matrix<T> : Matrix 
        where T:unmanaged
    {
        public Matrix(T[] d) :
            base(1, d.Length, typeof(T))
        {
            data = d;          
        }

        public Matrix(int cols, int rows)
            : base(cols, rows, typeof(T))
        {
            data = new T[cols * rows];
        }

        public Matrix(T[] data, int cols, int rows)
          : base(cols, rows, typeof(T))
        {
            if (data.Length < cols * rows)
                throw new ArgumentException();

            this.data = data;
        }

        private T[] data;

        public T[] Data => data;

        public T this[int r, int c]
        {
            get { return data[r + c * Columns]; }
            set { data[r + c * Columns] = value; }
        }

        public override Span<byte> GetRawData()
        {
            return MemoryMarshal.Cast<T, byte>(data.AsSpan());
        }

        public Span<T> GetColumn(int col)
        {
            if (col < 0 || col >= Columns)
                throw new IndexOutOfRangeException();

            return data.AsSpan().Slice(col * Rows, Rows);
        }
    }
}