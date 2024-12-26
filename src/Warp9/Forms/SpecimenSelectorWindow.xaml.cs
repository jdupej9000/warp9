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
using System.Windows.Shapes;
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
        public SpecimenSelectorWindow()
        {
            InitializeComponent();
        }

        long entryIndex = -1;
        Project? project;

        SpecimenTable Table
        {
            get
            {
                if (entryIndex < 0 || project is null || !project.Entries.TryGetValue(entryIndex, out ProjectEntry? entry))
                    throw new InvalidOperationException();

                return entry.Payload.Table ?? throw new InvalidOperationException();
            }
        }

        public void ShowEntry(long idx)
        {
            dataMain.Columns.Clear();

            entryIndex = idx;
            SpecimenTable table = Table;
            dataMain.ItemsSource = table;

            DataGridTextColumn colId = new DataGridTextColumn
            {
                Header = new SpecimenTableColumnTextInfo("ID", ""),
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
                                Header = new SpecimenTableColumnTextInfo(kvp.Key, kvp.Value.ColumnType.ToString()),
                                Binding = new Binding("[" + kvp.Key + "]")
                            };
                            dataMain.Columns.Add(col);
                        }
                        break;

                    case SpecimenTableColumnType.Factor:
                        {
                            DataGridComboBoxColumn col = new DataGridComboBoxColumn
                            {
                                Header = new SpecimenTableColumnTextInfo(kvp.Key, kvp.Value.ColumnType.ToString()),
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
                                Header = new SpecimenTableColumnTextInfo(kvp.Key, kvp.Value.ColumnType.ToString()),
                                Binding = new Binding("[" + kvp.Key + "]")
                            };
                            dataMain.Columns.Add(col);
                        }
                        break;
                }
            }
        }

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
        }
    }
}
