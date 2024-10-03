using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Warp9.Model
{
    public class SpecimenTableRow
    {
        public SpecimenTableRow(SpecimenTable t, int i)
        {
            table = t;
            rowIndex = i;
        }

        SpecimenTable table;
        int rowIndex;

        public object? this[string column]
        {
            get
            {
                return table.Columns[column].GetAt(rowIndex);
            }
            set
            {
                table.Columns[column].SetAt(rowIndex, value);
            }
        }
    };

    public class SpecimenTable
    {
        [JsonPropertyName("cols")]
        public Dictionary<string, SpecimenTableColumn> Columns { get; set; } = new Dictionary<string, SpecimenTableColumn>();

        public SpecimenTableColumn<T> AddColumn<T>(string name, SpecimenTableColumnType type, string[]? names = null)
        {
            // TODO: validate against allowed types
            SpecimenTableColumn<T> col = new SpecimenTableColumn<T>(type, names);
            Columns.Add(name, col);
            return col;
        }

        public static SpecimenTable Collect(bool allowIrregular, params SpecimenTable[] src)
        {
            Dictionary<string, SpecimenTableColumn> cols = new Dictionary<string, SpecimenTableColumn>();

            int nrows = 0;
            foreach (SpecimenTable tab in src)
            {
                foreach (var kvp in tab.Columns)
                {
                    cols[kvp.Key] = kvp.Value;

                    if (nrows == 0)
                    {
                        nrows = kvp.Value.NumRows;
                    }
                    else if (nrows != kvp.Value.NumRows && !allowIrregular)
                    {
                        throw new InvalidOperationException();
                    }
                }
            }

            SpecimenTable ret = new SpecimenTable();
            ret.Columns = cols;

            return ret;
        }

        public IEnumerable<SpecimenTableRow> GetRows()
        {
            int numRows = Columns.Values.First().NumRows;
            for (int i = 0; i < numRows; i++)
                yield return new SpecimenTableRow(this, i);
        }
    }
}
