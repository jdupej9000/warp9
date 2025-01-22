using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Warp9.Model;


namespace Warp9.Forms
{
    public record SpecimenTableColumnTextInfo(string name, string type)
    {
        public string Name { get; init; } = name;
        public string Type { get; init; } = type;
    }

    /// <summary>
    /// Interaction logic for SpecimenSelectorWindow.xaml
    /// </summary>
    public partial class SpecimenSelectorWindow : Window
    {
        public SpecimenSelectorWindow(SpecimenTableSelection sts)
        {
            table = sts;
            InitializeComponent();
        }

        SpecimenTableSelection table;

        private void ShowEntry()
        {
            dataMain.Columns.Clear();
            dataMain.ItemsSource = table;

            DataGridCheckBoxColumn colSel = new DataGridCheckBoxColumn
            {
                Header = new SpecimenTableColumnTextInfo("Selected", ""),
                CanUserReorder = false,
                IsReadOnly = false,
                IsThreeState = false,
                Binding = new Binding("IsSelected")
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

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            ShowEntry();
        }
    }
}
