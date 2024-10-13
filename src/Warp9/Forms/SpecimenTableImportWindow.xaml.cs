using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Warp9.Model;
using Warp9.Utils;

namespace Warp9.Forms
{
    public record StiwTypeComboItem(string name, ColumnImportType type)
    {
        public string Name { get; init; } = name;
        public ColumnImportType Type { get; init; } = type;
    };

    public class SpecimenTableImportAssgn : INotifyPropertyChanged
    {
        private string name = string.Empty;
        private string levelsRaw = string.Empty;
        private string colsRaw = "1";
        private int typeIndex = 0;

        public string Name 
        {
            get { return name; }
            set { name = value; Notify("Name"); }
        }

        public int TypeIndex
        {
            get { return typeIndex; }
            set { typeIndex = value; Notify("TypeIndex"); Notify("TypeRaw"); }
        }

        public string LevelsRaw
        {
            get { return levelsRaw; }
            set { levelsRaw = value; Notify("LevelsRaw"); }
        }

        public string ColumnRangeRaw
        {
            get { return colsRaw; }
            set { colsRaw = value; Notify("ColumnRangeRaw"); }
        }

        public string TypeRaw => ImportTypes[(ColumnImportType)typeIndex];
        
        public event PropertyChangedEventHandler? PropertyChanged;

        public void Notify(string pn)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(pn));
        }

        public SpecimenTableColumnImportOperation ToOperation()
        {
            List<int> cols = new List<int>();

            // TODO: move to method and optimize
            foreach (string seg in colsRaw.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
            {
                string[] parts = seg.Split('-');

                if (parts.Length == 1)
                {
                    if (int.TryParse(parts[0], out int n))
                        cols.Add(n - 1);
                }
                else if (parts.Length == 2)
                {
                    if (int.TryParse(parts[0], out int n0) &&
                        int.TryParse(parts[1], out int n1))
                    {
                        for (int i = n0; i <= n1; i++)
                            cols.Add(i - 1);
                    } 
                }
            }

            string[]? levels = null;
            if(levelsRaw is not null)
                levels = levelsRaw.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

            return new SpecimenTableColumnImportOperation(
               name, (ColumnImportType)typeIndex, cols.ToArray(), levels);
        }

        public static Dictionary<ColumnImportType, string> ImportTypes = new Dictionary<ColumnImportType, string>()
        {
            { ColumnImportType.Integer, "Integer" },
            { ColumnImportType.Real, "Real" },
            { ColumnImportType.String, "String" },
            { ColumnImportType.Factor, "Factor" },
            { ColumnImportType.Boolean, "Boolean" },
            { ColumnImportType.Image, "Image" },
            { ColumnImportType.Mesh, "Mesh" },
            { ColumnImportType.Landmarks, "Landmarks" },
            { ColumnImportType.Matrix, "Matrix" },
            { ColumnImportType.Landmarks2DAos, "Landmarks (xyxy)" },
            { ColumnImportType.Landmarks2DSoa, "Landmarks (xxyy)" },
            { ColumnImportType.Landmarks3DAos, "Landmarks (xyzxyz)" },
            { ColumnImportType.Landmarks3DSoa, "Landmarks (xxyyzz)" }
        };
    }

    /// <summary>
    /// Interaction logic for SpecimenTableImportWindow.xaml
    /// </summary>
    public partial class SpecimenTableImportWindow : Window
    {
        public SpecimenTableImportWindow()
        {
            InitializeComponent();
        }

        IUntypedTableProvider? Importer;
        ObservableCollection<SpecimenTableImportAssgn> ColumnAssignments = new ObservableCollection<SpecimenTableImportAssgn>();

        SpecimenTableImportAssgn? SelectedAssignmnent { get; set; } = null;

        public IEnumerable<SpecimenTableColumnImportOperation> ImportOperations => ColumnAssignments.Select((t) => t.ToOperation());

        public void AttachImporter(IUntypedTableProvider importer)
        {
            Importer = importer;
            DataContext = importer;
            UpdateColumns();
        }

        private void UpdateColumns()
        {
            dataCsv.Columns.Clear();

            if (Importer is null) return;

            string[]? data = Importer.ParsedData.FirstOrDefault();
            if (data is null) return;

            for (int i = 0; i < data.Length; i++)
            {
                DataGridTextColumn col = new DataGridTextColumn
                {
                    Header = string.Format("Column {0}", i + 1),
                    Binding = new Binding("[" + i.ToString() + "]"),
                    IsReadOnly = true,
                    CanUserReorder = false,
                    CanUserSort = false
                };

                dataCsv.Columns.Add(col);
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            foreach (var kvp in SpecimenTableImportAssgn.ImportTypes)
                cmbType.Items.Add(new StiwTypeComboItem(kvp.Value, kvp.Key));

            ColumnAssignments.Add(new SpecimenTableImportAssgn() { Name = "xxx", TypeIndex = 2, ColumnRangeRaw="1"});
            lstCols.ItemsSource = ColumnAssignments;
            
        }

        private void btnAddCol_Click(object sender, RoutedEventArgs e)
        {
            SpecimenTableImportAssgn newAssgn = new SpecimenTableImportAssgn();
            ColumnAssignments.Add(newAssgn);
        }

        private void btnRemoveCol_Click(object sender, RoutedEventArgs e)
        {
            if(SelectedAssignmnent is not null)
                ColumnAssignments.Remove(SelectedAssignmnent);
        }

        private void lstCols_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lstCols.SelectedItem is SpecimenTableImportAssgn stia)
                SelectedAssignmnent = stia;
            else
                SelectedAssignmnent = null;

            gridEditAssgn.DataContext = SelectedAssignmnent;
        }

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
