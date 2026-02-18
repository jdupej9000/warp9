using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
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
        long entryIndex = -1;
        bool fullResolveTable = false;

        SpecimenTable Table
        {
            get 
            {
                if (entryIndex < 0 || 
                    viewModel is null || 
                    !viewModel.Project.Entries.TryGetValue(entryIndex, out ProjectEntry? entry))
                    throw new InvalidOperationException();

                if(entry.Payload.Table is null)
                    throw new InvalidOperationException();

                if (fullResolveTable)
                    return ModelUtils.MakeFullSpecimenTable(viewModel.Project, entryIndex) ?? throw new InvalidOperationException();

                return entry.Payload.Table;
            }
        }

        public void AttachViewModel(Warp9ViewModel vm)
        {
            viewModel = vm;
        }

        public void DetachViewModel()
        {
            viewModel = null;
        }

        public void ShowEntry(long idx, bool fullResolve = false)
        {
            dataMain.Columns.Clear();

            entryIndex = idx;
            fullResolveTable = fullResolve;
            SpecimenTable table = Table;
            dataMain.ItemsSource = table;

            DataGridTextColumn colId = new DataGridTextColumn
            {
                Header = new SpecimenTableColumnInfo("ID", ""),
                Binding = new Binding("[!index]"),
                CanUserReorder = false,
                IsReadOnly = true
            };
            dataMain.Columns.Add(colId);

            foreach (var kvp in table.Columns)
            {
                switch (kvp.Value.ColumnType)
                {
                    case SpecimenTableColumnType.Integer:
                    case SpecimenTableColumnType.Real:
                    case SpecimenTableColumnType.String:
                        {
                            DataGridTextColumn col = new DataGridTextColumn
                            {
                                Header = new SpecimenTableColumnInfo(kvp.Key, kvp.Value.ColumnType.ToString()),
                                Binding = new Binding("[" + kvp.Key + "]")
                            };
                            dataMain.Columns.Add(col);
                        }
                        break;

                    case SpecimenTableColumnType.Factor:
                        {
                            DataGridComboBoxColumn col = new DataGridComboBoxColumn
                            {
                                Header = new SpecimenTableColumnInfo(kvp.Key, kvp.Value.ColumnType.ToString()),
                                SelectedItemBinding = new Binding("[" + kvp.Key + "]"),
                                ItemsSource = kvp.Value.Names
                            };
                            dataMain.Columns.Add(col);
                        }
                        break;

                    case SpecimenTableColumnType.Boolean:
                        {
                            DataGridCheckBoxColumn col = new DataGridCheckBoxColumn
                            {
                                Header = new SpecimenTableColumnInfo(kvp.Key, kvp.Value.ColumnType.ToString()),
                                Binding = new Binding("[" + kvp.Key + "]")
                            };
                            dataMain.Columns.Add(col);
                        }
                        break;
                    case SpecimenTableColumnType.Image:
                    case SpecimenTableColumnType.Mesh:
                    case SpecimenTableColumnType.PointCloud:
                    case SpecimenTableColumnType.Matrix:
                        {
                            DataGridTextColumn col = new DataGridTextColumn
                            {
                                Header = new SpecimenTableColumnInfo(kvp.Key, kvp.Value.ColumnType.ToString()),
                                Binding = new Binding("[" + kvp.Key + "]"),
                                IsReadOnly = true
                            };
                            dataMain.Columns.Add(col);
                        }
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
            SpecimenTable table = Table;
            table.Add(new SpecimenTableRow(table, table.Columns.Count));
        }

        private void btnSpecDelete_Click(object sender, RoutedEventArgs e)
        {
            int selected = dataMain.SelectedIndex;
            if (selected != -1)
                Table.RemoveAt(selected);
        }

        private void btnSpecEditCol_Click(object sender, RoutedEventArgs e)
        {

        }

        private void btnSpecImport_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
