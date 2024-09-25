using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Warp9.Data;

namespace Warp9.Model
{
    public enum SpecimenTableColumnType
    {
        Integer = 0,
        Real = 1,
        String = 2,
        Factor = 3,
        Boolean = 4,

        Image = 10,

        Mesh = 20,
        PointCloud = 21,

        Invalid = -1
    }

    public abstract class SpecimenTableColumn
    {
        public SpecimenTableColumnType ColumnType { get; internal set; } = SpecimenTableColumnType.Invalid;
        public abstract int NumRows { get; }
        public string[]? Names { get; set; } = null;

        public abstract IEnumerable<T> GetData<T>();
    }

    public class SpecimenTableColumn<T> : SpecimenTableColumn
    {
        public SpecimenTableColumn(SpecimenTableColumnType colType, string[]? names=null)
        {
            ColumnType = colType;
            Names = names;
        }

        List<T> data = new List<T>();

        public override int NumRows => data.Count;
        public List<T> Data => data;

        public override IEnumerable<TReq> GetData<TReq>()
        {
            if (data is List<TReq> typedData)
                return typedData;

            throw new InvalidOperationException();
        }
    }
}
