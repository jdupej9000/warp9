using System;
using System.Linq;

namespace Warp9.Model
{
    public class SpecimenTableRow
    {
        public SpecimenTableRow(SpecimenTable t, int i)
        {
            parent = t;
            rowIndex = i;
        }

        SpecimenTable parent;
        int rowIndex;

        internal SpecimenTable ParentTable => parent;
        public int RowIndex => rowIndex;

        public object? this[string column]
        {
            get
            {
                if (column == ModelConstants.IndexColumnName) return rowIndex.ToString();

                if (!parent.Columns.TryGetValue(column, out SpecimenTableColumn? col))
                    return null;

                object? val = col.GetAt(rowIndex);

                return col.ColumnType switch
                {
                    SpecimenTableColumnType.Integer or
                    SpecimenTableColumnType.Real or
                    SpecimenTableColumnType.String => val?.ToString() ?? "(null)",

                    SpecimenTableColumnType.Factor => val is null ? "" : col.Names![(int)val],

                    SpecimenTableColumnType.Boolean => (bool)(val ?? false),

                    SpecimenTableColumnType.Image or
                    SpecimenTableColumnType.Mesh or
                    SpecimenTableColumnType.PointCloud or
                    SpecimenTableColumnType.Matrix => ((ProjectReferenceLink)val!).ReferenceIndex,

                    _ => throw new NotImplementedException()
                };
            }
            set
            {
                if (!parent.Columns.TryGetValue(column, out SpecimenTableColumn? col))
                    return;

                parent.Columns[column].SetAt(rowIndex, ParseValue(col, value));
            }
        }

        public bool IsInSet(string column, params string[] set)
        {
            object value = GetSafeTypedValue(column);

            if (value is long i)
            {
                foreach (string s in set)
                {
                    if (long.TryParse(s, out long si64) && si64 == i)
                        return true;
                }
            }
            else if (value is double f)
            {
                foreach (string s in set)
                {
                    if (double.TryParse(s, out double sf64) && Math.Abs(sf64 - f) < 1e-6)
                        return true;
                }
            }
            else if (value is string t)
            {
                if (set.Contains(t))
                    return true;
            }
            else if (value is bool b)
            {
                foreach (string s in set)
                {
                    bool expected = (s.Length > 0 && (s[0] == 'T' || s[0] == 't'));
                    if (b == expected)
                        return true;
                }
            }

            return false;
        }

        public int CompareTo(string column, string s)
        {
            object value = GetSafeTypedValue(column);

            if (value is long i && long.TryParse(s, out long si64))
            {
                return i.CompareTo(si64);                
            }
            else if (value is double f && double.TryParse(s, out double sf64))
            {
                return f.CompareTo(sf64);
            }
            else if (value is string t)
            {
                return t.CompareTo(s);
            }
            else if (value is bool b)
            {               
                bool expected = (s.Length > 0 && (s[0] == 'T' || s[0] == 't'));
                return b.CompareTo(expected);                
            }

            return -1;
        }

        public object GetSafeTypedValue(string column)
        {
            if (column == ModelConstants.IndexColumnName) 
                return rowIndex.ToString();

            if (!parent.Columns.TryGetValue(column, out SpecimenTableColumn? col))
                return false;

            object? val = col.GetAt(rowIndex);
            return col.ColumnType switch
            {
                SpecimenTableColumnType.Integer => (val as long?) ?? -1,
                SpecimenTableColumnType.Real => (val as double?) ?? double.NaN,
                SpecimenTableColumnType.String => val?.ToString() ?? "(null)",
                SpecimenTableColumnType.Factor => val is null ? "" : col.Names![(int)val],
                SpecimenTableColumnType.Boolean => (bool)(val ?? false),
                _ => false
            };
        }

        private static object? ParseValue(SpecimenTableColumn col, object? val)
        {
            if (val is string v)
            {
                switch (col.ColumnType)
                {
                    case SpecimenTableColumnType.Integer:
                        if (long.TryParse(v, out long valLong))
                            return valLong;
                        break;

                    case SpecimenTableColumnType.Real:
                        if (double.TryParse(v, out double valDouble))
                            return valDouble;
                        break;

                    case SpecimenTableColumnType.String:
                        return v;

                    case SpecimenTableColumnType.Factor:
                        return Array.IndexOf(col.Names ?? Array.Empty<string>(), v);
                }
            }
            else if (val is bool b)
            {
                return b;
            }

            return null;
        }
    };
}
