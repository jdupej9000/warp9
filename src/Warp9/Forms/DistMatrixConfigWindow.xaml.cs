using System;
using System.Collections.Generic;
using System.Text;
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
    /// Interaction logic for DistMatrixConfigWindow.xaml
    /// </summary>
    public partial class DistMatrixConfigWindow : Window
    {
        public DistMatrixConfigWindow()
        {
            InitializeComponent();

            project = Project.CreateEmpty();
            Config = new DiffMatrixConfiguration();
        }
        

        Project project;
        SpecimenTableInfo? specTable;
        
        DiffMatrixConfiguration Config { get; set; }

        public IEnumerable<SpecimenTableInfo> SpecimenTables =>
          ModelUtils.EnumerateEntitiesWithTables(project);

        public IEnumerable<SpecimenTableColumnInfo> AllowedMeshColumns =>
            ModelUtils.EnumerateAllTableColumns(project)
                .Where((x) => x.SpecTableId == (specTable?.SpecTableId ?? -1) &&
                                x.Column.ColumnType == SpecimenTableColumnType.Mesh ||
                                x.Column.ColumnType == SpecimenTableColumnType.PointCloud);

        public IEnumerable<SpecimenTableColumnInfo> AllowedSizeColumns =>
            ModelUtils.EnumerateAllTableColumns(project)
                .Where((x) => x.SpecTableId == (specTable?.SpecTableId ?? -1) &&
                                x.Column.ColumnType == SpecimenTableColumnType.Real);

        public void Attach(Project proj, DiffMatrixConfiguration cfg)
        {
            project = proj;
            Config = cfg;
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
            foreach (var col in AllowedMeshColumns)
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
                Config.ParentEntityKey = sti.SpecTableId;
            }
            else
            {
                MessageBox.Show("There is no specimen table selected.");
                return;
            }

            if (cmbSourceColumn.SelectedValue is SpecimenTableColumnInfo stcilm)
            {
                Config.ParentColumnName = stcilm.ColumnName;
            }
            else
            {
                MessageBox.Show("There is no source data column selected.");
            }

            if (cmbSizeColumn.SelectedValue is SpecimenTableColumnInfo stcisize)
            {
                Config.ParentSizeColumn = stcisize.ColumnName;
                Config.RestoreSize = true;
            }
            else
            {
                Config.RestoreSize = false;
            }

            List<int> methods = new List<int>();
            if (chkMethodProcrustesRaw.IsChecked == true) methods.Add((int)MeshDistanceKind.ProcrustesRaw);
            if (chkMethodProcrustesOpa.IsChecked == true) methods.Add((int)MeshDistanceKind.Procrustes);

            Config.Methods = methods.ToArray();

            DialogResult = true;
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void cmbSpecTable_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0 && 
                e.AddedItems[0] is SpecimenTableInfo sti)
            {
                specTable = sti;
                UpdateColumnSelectors();
            }
        }
    }
}

