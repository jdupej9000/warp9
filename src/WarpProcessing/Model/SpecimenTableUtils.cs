using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Warp9.Model
{
    public enum SpecimenTableValuePredicate
    {
        Equals = 0,
        NotEqual,
        GreaterThan,
        GreaterThanOrEqual,
        LessThan,
        LessThanOrEqual,
        In,
        NotIn
    }

    public enum RepeatedMeasurementsOperation
    {
        TwoPointDifference,
        LinearRegression
    }

    public static class SpecimenTableUtils
    {
        public static bool TestPredicate(SpecimenTableRow row, string column, SpecimenTableValuePredicate predicate, string? value = null, string[]? values = null)
        {
            int opres = 0;
            switch (predicate)
            {
                case SpecimenTableValuePredicate.Equals:
                    ArgumentNullException.ThrowIfNull(value);                    
                    return row.IsInSet(column, value);

                case SpecimenTableValuePredicate.NotEqual:
                    ArgumentNullException.ThrowIfNull(value);
                    return !row.IsInSet(column, value);

                case SpecimenTableValuePredicate.GreaterThan:
                    ArgumentNullException.ThrowIfNull(value);
                    opres = row.CompareTo(column, value);
                    return opres == 1;

                case SpecimenTableValuePredicate.GreaterThanOrEqual:
                    ArgumentNullException.ThrowIfNull(value);
                    opres = row.CompareTo(column, value);
                    return opres == 1 || opres == 0;

                case SpecimenTableValuePredicate.LessThan:
                    ArgumentNullException.ThrowIfNull(value);
                    opres = row.CompareTo(column, value);
                    return opres == -1;

                case SpecimenTableValuePredicate.LessThanOrEqual:
                    ArgumentNullException.ThrowIfNull(value);
                    opres = row.CompareTo(column, value);
                    return opres == -1 || opres == 0;

                case SpecimenTableValuePredicate.In:
                    ArgumentNullException.ThrowIfNull(values);
                    return row.IsInSet(column, values);

                case SpecimenTableValuePredicate.NotIn:
                    ArgumentNullException.ThrowIfNull(values);
                    return !row.IsInSet(column, values);

                default:
                    return false;
            }
        }

        public static IEnumerable<SpecimenTableRow> SelectRows(IEnumerable<SpecimenTableRow> rows, string column, SpecimenTableValuePredicate predicate, string values)
        {
            string[] valueParts = values.Split(',');
            foreach (SpecimenTableRow row in rows)
            {
                if(TestPredicate(row, column, predicate, values, valueParts))
                    yield return row;
            }
        }

        public static IReadOnlyList<string> FindUniqueValuesAsString(SpecimenTable table, string column)
        {
            if (!table.Columns.TryGetValue(column, out SpecimenTableColumn? values) || values is null)
                return Array.Empty<string>();

            if (values.ColumnType == SpecimenTableColumnType.Factor)
            {
                return values.Names ?? Array.Empty<string>();
            }
            else if (values.ColumnType == SpecimenTableColumnType.Boolean)
            {
                return new string[] { "false", "true" };
            }
            else if (values.ColumnType == SpecimenTableColumnType.String)
            {
                return values.GetData<string>()
                    .ToHashSet()
                    .ToArray();
            }
            else if (values.ColumnType == SpecimenTableColumnType.Integer)
            {
                return values.GetData<long>()
                    .Select((t) => t.ToString(CultureInfo.InvariantCulture))
                    .ToHashSet()
                    .ToArray();
            }

            return Array.Empty<string>();
        }

        public static IEnumerable<(int, int)> FindPairs(SpecimenTable table, string seriesIdColumn, string seriesOrderColumn, string orderFirstValue, string orderSecondValue)
        {
            IReadOnlyList<string> levels = FindUniqueValuesAsString(table, seriesIdColumn);
            foreach (string lvl in levels)
            {
                int a = -1, b = -1;
                foreach (SpecimenTableRow rows in SelectRows(table, seriesIdColumn, SpecimenTableValuePredicate.Equals, lvl))
                {
                    if (rows.IsInSet(seriesOrderColumn, orderFirstValue))
                        a = rows.RowIndex;

                    if (rows.IsInSet(seriesOrderColumn, orderSecondValue))
                        b = rows.RowIndex;

                    if(a != -1 && b != -1)
                        yield return (a, b);
                }
            }
        }

        public static IEnumerable<int[]> FindSeries(SpecimenTable table, string seriesIdColumn, int minDataPoints = 1)
        {
            IReadOnlyList<string> levels = FindUniqueValuesAsString(table, seriesIdColumn);
            foreach (string lvl in levels)
            {
                int[] ret = SelectRows(table, seriesIdColumn, SpecimenTableValuePredicate.Equals, lvl)
                    .Select(t => t.RowIndex)
                    .ToArray();
               
                if(ret.Length >= minDataPoints)
                    yield return ret;
            }
        }
    }
}
