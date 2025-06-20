using System;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices;

namespace Warp9.Data
{
    public abstract class Matrix : ITable
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
        public bool HasColumnNames => false;

        public Type ColumnType(int idx) => ElementType;
        public string? ColumnName(int idx) => null;

        public abstract object? GetAt(int col, int row);
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
            get { return data[r + c * Rows]; }
            set { data[r + c * Rows] = value; }
        }

        public override Span<byte> GetRawData()
        {
            return MemoryMarshal.Cast<T, byte>(data.AsSpan());
        }

        public override object? GetAt(int col, int row)
        {
            if (col < 0 || row < 0 || col >= Columns || row >= Rows)
                return null;

            return data[row + col * Columns];
        }

        public Span<T> GetColumn(int col)
        {
            if (col < 0 || col >= Columns)
                throw new IndexOutOfRangeException();

            return data.AsSpan().Slice(col * Rows, Rows);
        }
    }
}