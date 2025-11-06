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

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            for (int i = 0; i < table.Selected.Length; i++)
                table.Selected[i] = false;
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
