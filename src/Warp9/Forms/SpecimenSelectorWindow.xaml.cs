using System;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using Warp9.Model;


namespace Warp9.Forms
{
    public record SpecimenTableColumnTextInfo(string name, string type)
    {
        public string Name { get; init; } = name;
        public string Type { get; init; } = type;

        public override string ToString()
        {
            return string.Format("{0} ({1})", Name, Type);
        }
    }

  
    /// <summary>
    /// Interaction logic for SpecimenSelectorWindow.xaml
    /// </summary>
    public partial class SpecimenSelectorWindow : Window
    {
        public SpecimenSelectorWindow(SpecimenTableSelection sts)
        {
            table = sts;
            InitSearchableColumns(sts);
            InitializeComponent();
            DataContext = this;
        }

        SpecimenTableSelection table;
        public ObservableCollection<SpecimenTableColumnTextInfo> SearchableColumns { get; } = new ObservableCollection<SpecimenTableColumnTextInfo>();
        public ObservableCollection<string> Operators { get; } = new ObservableCollection<string>{
            "Equals", "Not equal to", 
            "Greater than", "Greater than or equal to",
            "Less than", "Less than or equal to",
            "In", "Not in"
        };

        private void InitSearchableColumns(SpecimenTableSelection sts)
        {
            foreach (var col in sts.TableColumns)
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

        private void ShowEntry()
        {
            dataMain.Columns.Clear();
            dataMain.ItemsSource = table;

            FrameworkElementFactory checkboxFactory = new FrameworkElementFactory(typeof(CheckBox));
            checkboxFactory.SetValue(HorizontalAlignmentProperty, HorizontalAlignment.Center);
            checkboxFactory.SetValue(VerticalAlignmentProperty, VerticalAlignment.Center);
            checkboxFactory.SetBinding(ToggleButton.IsCheckedProperty, new Binding("IsSelected") 
            { 
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged 
            });

            DataGridTemplateColumn colSel = new DataGridTemplateColumn
            {
                Header = new SpecimenTableColumnTextInfo("Selected", ""),
                CanUserReorder = false,
                IsReadOnly = false,
                CellTemplate = new DataTemplate { VisualTree = checkboxFactory },
            };

            dataMain.Columns.Add(colSel);

            foreach (var kvp in table.TableColumns)
            {
                switch (kvp.Value.ColumnType)
                {
                    case SpecimenTableColumnType.Integer:
                    case SpecimenTableColumnType.Real:
                    case SpecimenTableColumnType.String:
                        {
                            DataGridTextColumn col = new DataGridTextColumn
                            {
                                Header = new SpecimenTableColumnTextInfo(kvp.Key, kvp.Value.ColumnType.ToString()),
                                Binding = new Binding("ParentRow[" + kvp.Key + "]"),
                                IsReadOnly = true
                            };
                            dataMain.Columns.Add(col);
                        }
                        break;

                    case SpecimenTableColumnType.Factor:
                        {
                            DataGridComboBoxColumn col = new DataGridComboBoxColumn
                            {
                                Header = new SpecimenTableColumnTextInfo(kvp.Key, kvp.Value.ColumnType.ToString()),
                                SelectedItemBinding = new Binding("ParentRow[" + kvp.Key + "]"),
                                ItemsSource = kvp.Value.Names,
                                IsReadOnly = true
                            };
                            dataMain.Columns.Add(col);
                        }
                        break;

                    case SpecimenTableColumnType.Boolean:
                        {
                            DataGridCheckBoxColumn col = new DataGridCheckBoxColumn
                            {
                                Header = new SpecimenTableColumnTextInfo(kvp.Key, kvp.Value.ColumnType.ToString()),
                                Binding = new Binding("ParentRow[" + kvp.Key + "]"),
                                IsReadOnly = true
                            };
                            dataMain.Columns.Add(col);
                        }
                        break;
                }
            }
        }

        private IEnumerable<SpecimenTableRow> GetFiltered()
        {
            IEnumerable<SpecimenTableRow> rows = table.Table;

            if ((chkTest0.IsChecked ?? true) == true && cmbCol0.SelectedValue is SpecimenTableColumnTextInfo col0)
                rows = SpecimenTableUtils.SelectRows(rows, col0.Name, (SpecimenTableValuePredicate)cmbOperator0.SelectedIndex, txtValue0.Text);

            if ((chkTest1.IsChecked ?? true) == true && cmbCol1.SelectedValue is SpecimenTableColumnTextInfo col1)
                rows = SpecimenTableUtils.SelectRows(rows, col1.Name, (SpecimenTableValuePredicate)cmbOperator1.SelectedIndex, txtValue1.Text);

            return rows;
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            for (int i = 0; i < table.Selected.Length; i++)
                table.Selected[i] = false;

            dataMain.ItemsSource = null;
            dataMain.ItemsSource = table;
        }

        private void Select_Click(object sender, RoutedEventArgs e)
        {
            foreach (SpecimenTableRow row in GetFiltered())
                table.Selected[row.RowIndex] = true;

            dataMain.ItemsSource = null;
            dataMain.ItemsSource = table;
        }

        private void Unselect_Click(object sender, RoutedEventArgs e)
        {
            foreach (SpecimenTableRow row in GetFiltered())
                table.Selected[row.RowIndex] = false;
            
            dataMain.ItemsSource = null;
            dataMain.ItemsSource = table;
        }

        private void Invert_Click(object sender, RoutedEventArgs e)
        {
            foreach (SpecimenTableRow row in GetFiltered())
                table.Selected[row.RowIndex] ^= true;

            dataMain.ItemsSource = null;
            dataMain.ItemsSource = table;
        }

        static bool oldFilterEnable0 = true, oldFilterEnable1 = false;
        static int oldColIndex0 = 0, oldColIndex1 = 0;
        static int oldOpIndex0 = 0, oldOpIndex1 = 0;
        static string oldFilterValue0 = "", oldFilterValue1 = "";

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            oldFilterEnable0 = chkTest0.IsChecked.GetValueOrDefault();
            oldColIndex0 = cmbCol0.SelectedIndex;
            oldOpIndex0 = cmbOperator0.SelectedIndex;
            oldFilterValue0 = txtValue0.Text;

            oldFilterEnable1 = chkTest1.IsChecked.GetValueOrDefault();
            oldColIndex1 = cmbCol1.SelectedIndex;
            oldOpIndex1 = cmbOperator1.SelectedIndex;
            oldFilterValue1 = txtValue1.Text;

            DialogResult = true;
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            ShowEntry();

            try
            {
                chkTest0.IsChecked = oldFilterEnable0;
                cmbCol0.SelectedIndex = oldColIndex0;
                cmbOperator0.SelectedIndex = oldOpIndex0;
                txtValue0.Text = oldFilterValue0;

                chkTest1.IsChecked = oldFilterEnable1;
                cmbCol1.SelectedIndex = oldColIndex1;
                cmbOperator1.SelectedIndex = oldOpIndex1;
                txtValue1.Text = oldFilterValue1;
            }
            catch
            {
            }
        }
    }
}
