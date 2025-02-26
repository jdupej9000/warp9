using System;
using System.Collections;
using System.Collections.Generic;
using Warp9.Data;


namespace Warp9.Model
{
    public record MatrixColumnViewProvider(string Name);

    public class MatrixRowViewProvider(Matrix Matrix, int FirstColIndex, int NumCols, int RowIndex)
    {
        public int Index => RowIndex;
        public object? this[int index]
        {
            get
            {
                if (index >= NumCols)
                    return "!RNG";

                if (Matrix is Matrix<float> matf)
                {
                    return matf.GetColumn(FirstColIndex + index)[RowIndex];
                }
                else if (Matrix is Matrix<int> mati)
                {
                    return mati.GetColumn(FirstColIndex + index)[RowIndex];
                }

                return "!TYPE";
            }
            set
            { 
            }
        }
    }

    public class MatrixRowViewEnumerator(Matrix Matrix, int FirstColIndex, int NumCols) : IEnumerator, IEnumerator<MatrixRowViewProvider>
    {
        private int index = 0;

        public object Current => CurrentRow();
        MatrixRowViewProvider IEnumerator<MatrixRowViewProvider>.Current => CurrentRow();

        public bool MoveNext()
        {
            if (index >= Matrix.Rows)
                return false;

            index++;
            return true;
        }

        public void Reset()
        {
            index = 0;
        }

        public void Dispose()
        {
        }

        private MatrixRowViewProvider CurrentRow()
        {
            return new MatrixRowViewProvider(Matrix, FirstColIndex, NumCols, index);
        }
    }

    public class MatrixViewProvider : IList<MatrixRowViewProvider>, IList
    {
        public MatrixViewProvider(Matrix mat, string name = "", string columnNamePattern="{0}", int firstCol = 0, int numCols = -1)
        {
            Matrix = mat;
            Name = name;
            ColumnNamePattern = columnNamePattern;
            FirstColumnIndex = firstCol;
            NumCols = numCols;
        }

        public Matrix Matrix { get; init; }
        public string Name { get; init; }
        public string ColumnNamePattern { get; init; }
        public int FirstColumnIndex { get; init; }
        public int NumCols { get; init; }
        public int Count => Matrix.Rows;
        public bool IsReadOnly => false;
        public bool IsFixedSize => false;
        public bool IsSynchronized => false;
        public object SyncRoot => throw new NotImplementedException();

        public IEnumerable<MatrixColumnViewProvider> Columns => EnumerateColumns();

        object? IList.this[int index]
        {
            get => new MatrixRowViewProvider(Matrix, FirstColumnIndex, NumCols, index);
            set => throw new InvalidOperationException();
        }

        public MatrixRowViewProvider this[int index]
        {
            get => new MatrixRowViewProvider(Matrix, FirstColumnIndex, NumCols, index);
            set => throw new InvalidOperationException();
        }

        public IEnumerator<MatrixRowViewProvider> EnumerateRows()
        {
            return new MatrixRowViewEnumerator(Matrix, FirstColumnIndex, NumCols);
        }

        private IEnumerable<MatrixColumnViewProvider> EnumerateColumns()
        {
            int i0 = FirstColumnIndex;
            
            int i1 = NumCols;
            if (i1 < 0) 
                i1 = Matrix.Columns - i0;

            for (int i = i0; i < i1; i++)
                yield return new MatrixColumnViewProvider(string.Format(ColumnNamePattern, i));
        }

        public override string ToString()
        {
            return Name;
        }

        public int IndexOf(MatrixRowViewProvider item)
        {
            return item.Index;
        }

        public void Insert(int index, MatrixRowViewProvider item)
        {
            throw new InvalidOperationException();
        }

        public void RemoveAt(int index)
        {
            throw new InvalidOperationException();
        }

        public void Add(MatrixRowViewProvider item)
        {
            throw new InvalidOperationException();
        }

        public void Clear()
        {
            throw new InvalidOperationException();
        }

        public bool Contains(MatrixRowViewProvider item)
        {
            return true;
        }

        public void CopyTo(MatrixRowViewProvider[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public bool Remove(MatrixRowViewProvider item)
        {
            return false;
        }

        public IEnumerator<MatrixRowViewProvider> GetEnumerator()
        {
            return EnumerateRows();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public int Add(object? value)
        {
            throw new NotImplementedException();
        }

        public bool Contains(object? value)
        {
            return true;
        }

        public int IndexOf(object? value)
        {
            if (value is MatrixRowViewProvider mrvp)
                return mrvp.Index;

            return -1;
        }

        public void Insert(int index, object? value)
        {
            throw new InvalidOperationException();
        }

        public void Remove(object? value)
        {
            throw new InvalidOperationException();
        }

        public void CopyTo(Array array, int index)
        {
            throw new InvalidOperationException();
        }
    }
}
