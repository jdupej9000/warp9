using System;
using System.Collections.Generic;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using Warp9.Model;
using Warp9.ProjectExplorer;

namespace Warp9.Navigation
{
    public record SpecimenTableColumnInfo(string name, string type)
    {
        public string Name { get; init; } = name;
        public string Type { get; init; } = type;
    }

    public partial class SpecimenTablePage : Page, IWarp9View
    {
        public SpecimenTablePage()
        {
            InitializeComponent();
        }

        Warp9ViewModel? viewModel;

        public void AttachViewModel(Warp9ViewModel vm)
        {
            viewModel = vm;
        }

        public void DetachViewModel()
        {
            viewModel = null;
        }

        public void ShowEntry(long idx)
        {
            dataMain.Columns.Clear();

            if (viewModel is null || !viewModel.Project.Entries.TryGetValue(idx, out ProjectEntry? entry))
                throw new InvalidOperationException();

            SpecimenTable table = entry.Payload.Table ?? throw new InvalidOperationException();
            dataMain.ItemsSource = table.GetRows().ToList();

            foreach (var kvp in table.Columns)
            {

                switch (kvp.Value.ColumnType)
                {
                    case SpecimenTableColumnType.Integer:
                    case SpecimenTableColumnType.Real:
                    case SpecimenTableColumnType.String:
                        {
                            DataGridTextColumn col = new DataGridTextColumn();
                            col.Header = new SpecimenTableColumnInfo(kvp.Key, kvp.Value.ColumnType.ToString());
                            col.Binding = new Binding("[" + kvp.Key + "]");
                            dataMain.Columns.Add(col);
                        }
                        break;

                    case SpecimenTableColumnType.Factor:
                        {
                            DataGridComboBoxColumn col = new DataGridComboBoxColumn();
                            col.Header = new SpecimenTableColumnInfo(kvp.Key, kvp.Value.ColumnType.ToString());
                            col.SelectedItemBinding = new Binding("[" + kvp.Key + "]");
                            col.ItemsSource = kvp.Value.Names;
                            dataMain.Columns.Add(col);
                        }
                        break;

                    case SpecimenTableColumnType.Boolean:
                        {
                            DataGridCheckBoxColumn col = new DataGridCheckBoxColumn();
                            col.Header = new SpecimenTableColumnInfo(kvp.Key, kvp.Value.ColumnType.ToString());
                            col.Binding = new Binding("[" + kvp.Key + "]");
                            dataMain.Columns.Add(col);
                        }
                        break;
                    case SpecimenTableColumnType.Image:
                        break;
                    case SpecimenTableColumnType.Mesh:
                        break;
                    case SpecimenTableColumnType.PointCloud:
                        break;
                    default:
                        throw new NotSupportedException();
                }

               
            }
        }

        private void dataMain_BeginningEdit(object sender, DataGridBeginningEditEventArgs e)
        {
            // This is a workaround for incorrectly drawn combo box backgrounds in the data grid.
            // see https://github.com/AngryCarrot789/WPFDarkTheme/issues/28

            /*if (isAnyRowEditing())
            {
                e.Cancel = true;
            }*/
            
            if (e.Column is DataGridComboBoxColumn cbCol)
            {
                Style style = new Style(typeof(ComboBox), 
                    (Style)TryFindResource("DataGridComboBoxColumnEditingElementStyle"));

                if (style is not null)
                    cbCol.EditingElementStyle = style;
            }
            else if (e.Column is DataGridTextColumn txCol)
            {
                Style style = new Style(typeof(TextBox),
                    (Style)TryFindResource("DataGridTextColumnEditingElementStyle"));

                if (style is not null)
                    txCol.EditingElementStyle = style;
            }
            else if (e.Column is DataGridCheckBoxColumn chCol)
            {
                Style style = new Style(typeof(CheckBox),
                    (Style)TryFindResource("DataGridCheckBoxColumnEditingElementStyle"));

                if (style is not null)
                    chCol.EditingElementStyle = style;
            }
        }

        private void btnSpecAdd_Click(object sender, RoutedEventArgs e)
        {

        }

        private void btnSpecDelete_Click(object sender, RoutedEventArgs e)
        {

        }

        private void btnSpecEditCol_Click(object sender, RoutedEventArgs e)
        {

        }

        private void btnSpecImport_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
