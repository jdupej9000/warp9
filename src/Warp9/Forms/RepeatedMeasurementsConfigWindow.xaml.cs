using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Warp9.Model;
using Warp9.Processing;
using Warp9.Themes;

namespace Warp9.Forms
{
    public class RepeatedMeasurementsOperationRadioConverter : RadioBoolToIntConverter<RepeatedMeasurementsOperation>
    {
    };

    /// <summary>
    /// Interaction logic for RepeatedMeasurementsConfigWindow.xaml
    /// </summary>
    public partial class RepeatedMeasurementsConfigWindow : Window
    {
        public RepeatedMeasurementsConfigWindow()
        {
            InitializeComponent();
        }

        public SpecimenTable Table { get; set; }
        public SpecimenTableSeriesSelection Series { get; set; }
        public RepeatedMeasurementsOperation Operation { get; set; } = RepeatedMeasurementsOperation.TwoPointDifference;

        public int SeriesColumnIndex { get; set; }
        public int OrderColumnIndex { get; set; }

        public ObservableCollection<SpecimenTableColumnTextInfo> SearchableColumns { get; } = new ObservableCollection<SpecimenTableColumnTextInfo>();
        public ObservableCollection<string> OrderValueLevels { get; set; }

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
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            InitSearchableColumns();
        }
              
        private void OrderColumn_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            OrderValueLevels.Clear();
            foreach(string val in SpecimenTableUtils.FindUniqueValuesAsString(Table, SearchableColumns[OrderColumnIndex].Name))
                OrderValueLevels.Add(val);
        }
    }
}
