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
        }
        

        Project project;
        SpecimenTableInfo? specTable;

        public IEnumerable<SpecimenTableInfo> SpecimenTables =>
          ModelUtils.EnumerateSpecimenTables(project);

        public IEnumerable<SpecimenTableColumnInfo> AllowedMeshColumns =>
            ModelUtils.EnumerateAllTableColumns(project)
                .Where((x) => x.SpecTableId == (specTable?.SpecTableId ?? -1) &&
                                x.Column.ColumnType == SpecimenTableColumnType.Mesh ||
                                x.Column.ColumnType == SpecimenTableColumnType.PointCloud);

        public void Attach(Project proj, object cfg)
        {
            project = proj;

            cmbSpecTable.Items.Clear();
            foreach (var st in SpecimenTables)
                cmbSpecTable.Items.Add(st);

            if (cmbSpecTable.Items.Count > 0)
                cmbSpecTable.SelectedIndex = 0;
        }

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void cmbSpecTable_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
        }

    }
}

