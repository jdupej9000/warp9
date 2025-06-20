using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Warp9.Data
{
    public interface ITable
    {
        public bool HasColumnNames { get; }
        public int Columns { get; }
        public int Rows { get; }
        public Type ColumnType(int idx);
        public string? ColumnName(int idx);
        public object? GetAt(int col, int row);
    }
}
