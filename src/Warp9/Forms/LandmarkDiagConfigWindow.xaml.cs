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

namespace Warp9.Forms
{
    /// <summary>
    /// Interaction logic for LandmarkDiagConfigWindow.xaml
    /// </summary>
    public partial class LandmarkDiagConfigWindow : Window
    {
        public LandmarkDiagConfigWindow()
        {
            InitializeComponent();

            configuration = new LandmarkDiagConfiguration();
            project = Project.CreateEmpty();
        }

        Project project;
        LandmarkDiagConfiguration configuration;
        SpecimenTableInfo? specTable;

        public IEnumerable<SpecimenTableInfo> SpecimenTables =>
            ModelUtils.EnumerateEntitiesWithTables(project);

        public IEnumerable<SpecimenTableColumnInfo> AllowedLandmarkColumns =>
           ModelUtils.EnumerateAllTableColumns(project)
               .Where((x) => x.SpecTableId == (specTable?.SpecTableId ?? -1) &&
                               x.Column.ColumnType == SpecimenTableColumnType.PointCloud);

        public IEnumerable<SpecimenTableColumnInfo> AllowedMeshColumns =>
          ModelUtils.EnumerateAllTableColumns(project)
              .Where((x) => x.SpecTableId == (specTable?.SpecTableId ?? -1) &&
                              x.Column.ColumnType == SpecimenTableColumnType.Mesh);

        public void Attach(Project proj, LandmarkDiagConfiguration cfg)
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
            cmbLandmarkColumn.Items.Clear();
            foreach (var col in AllowedLandmarkColumns)
                cmbLandmarkColumn.Items.Add(col);

            cmbMeshColumn.Items.Clear();
            foreach (var col in AllowedMeshColumns)
                cmbMeshColumn.Items.Add(col);

            if (cmbLandmarkColumn.Items.Count > 0)
                cmbLandmarkColumn.SelectedIndex = 0;

            if (cmbMeshColumn.Items.Count > 0)
                cmbMeshColumn.SelectedIndex = 0;
        }

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            if (cmbSpecTable.SelectedValue is SpecimenTableInfo sti)
            {
                configuration.SpecimenTableKey = sti.SpecTableId;
            }
            else
            {
                MessageBox.Show("There is no specimen table selected.");
                return;
            }

            if (cmbLandmarkColumn.SelectedValue is SpecimenTableColumnInfo stcilm)
            {
                configuration.LandmarkColumn = stcilm.ColumnName;
            }
            else
            {
                MessageBox.Show("There is no source data column selected.");
            }

            if (cmbMeshColumn.SelectedValue is SpecimenTableColumnInfo stcimsh)
            {
                configuration.MeshColumn = stcimsh.ColumnName;
            }
            else
            {
                MessageBox.Show("There is no source data column selected.");
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
