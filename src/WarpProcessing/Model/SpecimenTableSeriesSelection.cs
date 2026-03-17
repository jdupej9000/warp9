using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Policy;
using System.Text;

namespace Warp9.Model
{
    public record SpecimenTableSeries
    {
        public SpecimenTableSeries(SpecimenTable table, IEnumerable<int> indices)
        {
            Table = table;
            Indices.AddRange(indices);
        }

        public SpecimenTable Table { get; init; }
        public List<int> Indices { get; init; } = new List<int>();

        public IEnumerable<SpecimenTableRow> EnumRows()
        {
            foreach (int i in Indices)
                yield return Table[i];
        }
    };
  

    public class SpecimenTableSeriesSelection
    {
        public SpecimenTableSeriesSelection(SpecimenTable tab)
        {
            specTable = tab;
        }

        private readonly SpecimenTable specTable;
        private List<SpecimenTableSeries> series = new List<SpecimenTableSeries>();

        public IReadOnlyList<SpecimenTableSeries> Series => series;

        private void AddSeries(params int[] indices)
        {
            series.Add(new SpecimenTableSeries(specTable, indices));
        }

        public static SpecimenTableSeriesSelection MakePairs(SpecimenTable table, string seriesIdColumn, string seriesOrderColumn, string orderFirstValue, string orderSecondValue)
        {
            SpecimenTableSeriesSelection ret = new SpecimenTableSeriesSelection(table);

            foreach ((int a, int b) in SpecimenTableUtils.FindPairs(table, seriesIdColumn, seriesOrderColumn, orderFirstValue, orderSecondValue))
                ret.AddSeries(a, b);

            return ret;
        }

        public static SpecimenTableSeriesSelection MakeSeries(SpecimenTable table, string seriesIdColumn, int minDataPoints = 1)
        {
            SpecimenTableSeriesSelection ret = new SpecimenTableSeriesSelection(table);

            foreach (int[] idx in SpecimenTableUtils.FindSeries(table, seriesIdColumn))
                ret.AddSeries(idx);

            return ret;
        }
    }
}
