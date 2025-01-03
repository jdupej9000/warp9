using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Linq.Dynamic.Core;
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
using System.Text.RegularExpressions;

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

        private void txtQuery_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                try
                {
                    ApplyQuery(txtQuery.Text, chkQueryClearFirst.IsChecked ?? false, chkQueryCheck.IsChecked ?? false);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message + Environment.NewLine + ex.StackTrace);
                }
            }
        }

        private void ApplyQuery(string query, bool clearFirst, bool newSelect)
        {
            string code = "row => " + Regex.Replace(query,
                @"\$([A-Za-z0-9]+)", @"row[""$1""]");

            ParsingConfig cfg = new ParsingConfig();
            cfg.ConvertObjectToSupportComparison = true;

            LambdaExpression lambda = DynamicExpressionParser.ParseLambda<SpecimenTableSelectionRow, bool>(
                typeof(Func<SpecimenTableSelectionRow, bool>), cfg, false, code);

            Func<SpecimenTableSelectionRow, bool> pred = (Func<SpecimenTableSelectionRow, bool>)lambda.Compile();

            if (clearFirst)
            {
                for (int i = 0; i < table.Count; i++)
                    table.Selected[i] = false;
            }

            
            // translate $Name to Row["Name"]
            foreach (SpecimenTableSelectionRow row in table.AsQueryable().Where(pred))
            {
               // MessageBox.Show(row.ToString());
                table.Selected[row.Index] = newSelect;
            }

            table.NotifyUpdated();
        }
    }
}
