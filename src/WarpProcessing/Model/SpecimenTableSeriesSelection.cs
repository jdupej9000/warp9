using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Policy;
using System.Text;
using Warp9.Data;

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

    public enum RepeatedMeasurementsOperation
    {
        TwoPointDifference = 0,
        LinearRegression
    }

    public class SpecimenTableSeriesSelection
    {
        public SpecimenTableSeriesSelection(SpecimenTable tab)
        {
            specTable = tab;
        }

        private readonly SpecimenTable specTable;
        private List<SpecimenTableSeries> series = new List<SpecimenTableSeries>();

        public string IdColumn { get; private set; } = string.Empty;
        public string? OrderColumn { get; private set; }
        public string? FirstValue { get; private set; }
        public string? SecondValue { get; private set; }
        public RepeatedMeasurementsOperation Operation { get; set; } = RepeatedMeasurementsOperation.TwoPointDifference;

        public IReadOnlyList<SpecimenTableSeries> Series => series;
        public SpecimenTable Table => specTable;

        public void InitToPairs(string seriesIdColumn, string seriesOrderColumn, string orderFirstValue, string orderSecondValue)
        {
            series.Clear();
            foreach ((int a, int b) in SpecimenTableUtils.FindPairs(specTable, seriesIdColumn, seriesOrderColumn, orderFirstValue, orderSecondValue))
                AddSeries(a, b);

            IdColumn = seriesIdColumn;
            OrderColumn = seriesOrderColumn;
            FirstValue = orderFirstValue;
            SecondValue = orderSecondValue;
        }

        public void InitToSeries(string seriesIdColumn, int minDataPoints = 1)
        {
            series.Clear();
            foreach (int[] idx in SpecimenTableUtils.FindSeries(specTable, seriesIdColumn))
                AddSeries(idx);

            IdColumn = seriesIdColumn;            
        }

        public void Clear()
        {
            series.Clear();
        }

        private void AddSeries(params int[] indices)
        {
            series.Add(new SpecimenTableSeries(specTable, indices));
        }
    }
}
