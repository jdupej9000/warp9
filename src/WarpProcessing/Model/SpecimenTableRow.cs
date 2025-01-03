using System;

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
        internal int RowIndex => rowIndex;

        public object? this[string column]
        {
            get
            {
                if (column == "!index") return rowIndex.ToString();

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

        public object GetSafeTypedValue(string column)
        {
            if (column == "!index") 
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
