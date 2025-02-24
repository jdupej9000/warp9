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
using Warp9.Processing;
using Warp9.Themes;

namespace Warp9.Forms
{
    public class PcaRejectionModeRadioConverter : RadioBoolToIntConverter<PcaRejectionMode>
    {
    };

    /// <summary>
    /// Interaction logic for PcaConfigWindow.xaml
    /// </summary>
    public partial class PcaConfigWindow : Window
    {
        public PcaConfigWindow()
        {
            InitializeComponent();

            configuration = new PcaConfiguration();
            project = Project.CreateEmpty();
        }

        Project project;
        PcaConfiguration configuration;
        SpecimenTableInfo? specTable;

        public IEnumerable<SpecimenTableInfo> SpecimenTables =>
          ModelUtils.EnumerateEntitiesWithTables(project);

        public IEnumerable<SpecimenTableColumnInfo> AllowedSourceColumns =>
            ModelUtils.EnumerateAllTableColumns(project)
                .Where((x) => x.SpecTableId == (specTable?.SpecTableId ?? -1) &&
                                x.Column.ColumnType == SpecimenTableColumnType.PointCloud);

        public IEnumerable<SpecimenTableColumnInfo> AllowedSizeColumns =>
            ModelUtils.EnumerateAllTableColumns(project)
                .Where((x) => x.SpecTableId == (specTable?.SpecTableId ?? -1) &&
                                x.Column.ColumnType == SpecimenTableColumnType.Real);

        public void Attach(Project proj, PcaConfiguration cfg)
        {
            project = proj;
            configuration = cfg;
            DataContext = cfg;

            cmbSpecTable.Items.Clear();
            foreach (var st in SpecimenTables)
                cmbSpecTable.Items.Add(st);

            if (cmbSpecTable.Items.Count > 0)
                cmbSpecTable.SelectedIndex = 0;
        }


        private void UpdateColumnSelectors()
        {
            cmbSourceColumn.Items.Clear();
            foreach (var col in AllowedSourceColumns)
                cmbSourceColumn.Items.Add(col);

            cmbSizeColumn.Items.Clear();
            cmbSizeColumn.Items.Add("Leave as is");
            foreach (var col in AllowedSizeColumns)
                cmbSizeColumn.Items.Add(col);

            if (cmbSourceColumn.Items.Count > 0)
                cmbSourceColumn.SelectedIndex = 0;

            if (cmbSizeColumn.Items.Count > 0)
                cmbSizeColumn.SelectedIndex = 0;
        }


        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            if (cmbSpecTable.SelectedValue is SpecimenTableInfo sti)
            {
                configuration.ParentEntityKey = sti.SpecTableId;
            }
            else
            {
                MessageBox.Show("There is no specimen table selected.");
                return;
            }

            if (cmbSourceColumn.SelectedValue is SpecimenTableColumnInfo stcilm)
            {
                configuration.ParentColumnName = stcilm.ColumnName;
            }
            else
            {
                MessageBox.Show("There is no source data column selected.");
            }

            if (cmbSizeColumn.SelectedValue is SpecimenTableColumnInfo stcisize)
            {
                configuration.ParentSizeColumn = stcisize.ColumnName;
                configuration.RestoreSize = true;
            }
            else
            {
                configuration.RestoreSize = false;
            }

            DialogResult = true;
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void cmbSpecTable_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0 && e.AddedItems[0] is SpecimenTableInfo sti)
            {
                specTable = sti;
                UpdateColumnSelectors();
            }
        }
    }
}
