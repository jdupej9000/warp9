using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text;
using System.Windows.Documents;
using Warp9.Forms;
using Warp9.Model;

namespace Warp9.Viewer
{
    public class RepeatedMeasurementsCfg
    {
        public RepeatedMeasurementsCfg(SpecimenTableSeriesSelection s)
        {
            Series = s;
            InitSearchableColumns();
        }

        public SpecimenTableSeriesSelection Series { get; init; }
        public SpecimenTable Table => Series.Table;

        public int SeriesColumnIndex 
        { 
            get; 
            set 
            { 
                field = value; 
                UpdateSelection(); 
            } 
        } = 0;

        public int OrderColumnIndex
        {
            get;
            set
            {
                field = value;
                UpdateLevels();
            }
        } = 0;

        public int OrderFirstIndex 
        { 
            get;
            set
            {
                field = value;
                UpdateSelection();
            }
        } = 0;

        public int OrderSecondIndex
        {
            get;
            set
            {
                field = value;
                UpdateSelection();
            }
        } = 0;

        public ObservableCollection<SpecimenTableColumnTextInfo> SearchableColumns { get; } = new ObservableCollection<SpecimenTableColumnTextInfo>();
        public ObservableCollection<string> OrderValueLevels { get; set; } = new ObservableCollection<string>();

        private void InitSearchableColumns()
        {
            SearchableColumns.Clear();
            foreach (var col in Table.Columns)
            {
                if (col.Value.ColumnType == SpecimenTableColumnType.Integer ||
                    col.Value.ColumnType == SpecimenTableColumnType.Real ||
                    col.Value.ColumnType == SpecimenTableColumnType.String ||
                    col.Value.ColumnType == SpecimenTableColumnType.Factor ||
                    col.Value.ColumnType == SpecimenTableColumnType.Boolean)
                {
                    SearchableColumns.Add(new SpecimenTableColumnTextInfo(
                        col.Key, col.Value.ColumnType.ToString()));
                }
            }

            for (int i = 0; i < SearchableColumns.Count; i++)
            {
                if (SearchableColumns[i].Name == Series.IdColumn)
                    SeriesColumnIndex = i;

                if (Series.OrderColumn == SearchableColumns[i].Name)
                    OrderColumnIndex = i;
            }

            UpdateLevels();

            if (Series.FirstValue is not null)
                OrderFirstIndex = OrderValueLevels.IndexOf(Series.FirstValue);

            if (Series.SecondValue is not null)
                OrderSecondIndex = OrderValueLevels.IndexOf(Series.SecondValue);
        }

        private void UpdateLevels()
        {
            OrderValueLevels.Clear();
            foreach (string val in SpecimenTableUtils.FindUniqueValuesAsString(Table, SearchableColumns[OrderColumnIndex].Name))
                OrderValueLevels.Add(val);

            UpdateSelection();
        }

        public void UpdateSelection()
        {
            try
            {
                string seriesName = SearchableColumns[SeriesColumnIndex].Name;
                string orderName;
                string firstValue;
                string secondValue;

                switch (Series.Operation)
                {
                    case RepeatedMeasurementsOperation.TwoPointDifference:
                        orderName = SearchableColumns[OrderColumnIndex].Name;
                        firstValue = OrderValueLevels[OrderFirstIndex];
                        secondValue = OrderValueLevels[OrderSecondIndex];
                        Series.InitToPairs(seriesName, orderName, firstValue, secondValue);
                        break;

                    case RepeatedMeasurementsOperation.LinearRegression:
                        Series.InitToSeries(seriesName);
                        break;
                }
            }
            catch
            {
                Series.Clear();
            }
        }
    }
}
