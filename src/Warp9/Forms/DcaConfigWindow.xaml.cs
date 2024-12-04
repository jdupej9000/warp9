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

    public class DcaRigidPreregKindRadioConverter : RadioBoolToIntConverter<DcaRigidPreregKind>
    {
    };

    public class DcaNonrigidRegistrationKindRadioConverter : RadioBoolToIntConverter<DcaNonrigidRegistrationKind>
    {
    };

    public class DcaSurfaceProjectionKindRadioConverter : RadioBoolToIntConverter<DcaSurfaceProjectionKind>
    {
    };

    public class DcaRigidPostRegistrationKindRadioConverter : RadioBoolToIntConverter<DcaRigidPostRegistrationKind>
    {
    };

    /// <summary>
    /// Interaction logic for DcaConfigWindow.xaml
    /// </summary>
    public partial class DcaConfigWindow : Window
    {
        public DcaConfigWindow()
        {
            InitializeComponent();

            configuration = new DcaConfiguration();
            project = Project.CreateEmpty();
        }

        Project project;
        DcaConfiguration configuration;
        SpecimenTableInfo? specTable;

        public DcaConfiguration Config => configuration;
        public Project Project => project;

        public IEnumerable<SpecimenTableInfo> SpecimenTables =>
           ModelUtils.EnumerateSpecimenTables(project);


        public IEnumerable<SpecimenTableColumnInfo> AllowedMeshColumns =>
            ModelUtils.EnumerateAllSpecimenTableColumns(project)
                .Where((x) => x.SpecTableId == (specTable?.SpecTableId ?? -1) && 
                                x.Column.ColumnType == SpecimenTableColumnType.Mesh);

        public IEnumerable<SpecimenTableColumnInfo> AllowedLandmarksColumns =>
            ModelUtils.EnumerateAllSpecimenTableColumns(project)
                .Where((x) => x.SpecTableId == (specTable?.SpecTableId ?? -1) &&
                                x.Column.ColumnType == SpecimenTableColumnType.PointCloud);

        public void Attach(Project proj, DcaConfiguration cfg)
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
            cmbMeshes.Items.Clear();
            foreach (var col in AllowedMeshColumns)
                cmbMeshes.Items.Add(col);

            cmbLandmarks.Items.Clear();
            foreach (var col in AllowedLandmarksColumns)
                cmbLandmarks.Items.Add(col);

            cmbLandmarks.Items.Add("No landmarks");

            cmbBase.Items.Clear();
            for (int i = 0; i < specTable?.SpecimenTable.Count; i++)
                cmbBase.Items.Add(i);

            if (cmbMeshes.Items.Count > 0)
                cmbMeshes.SelectedIndex = 0;

            if (cmbLandmarks.Items.Count > 0)
                cmbLandmarks.SelectedIndex = 0;

            if (cmbBase.Items.Count > 0)
                cmbBase.SelectedIndex = 0;
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

            if (cmbLandmarks.SelectedValue is SpecimenTableColumnInfo stcilm)
            {
                configuration.LandmarkColumnName = stcilm.ColumnName;
            }

            if (cmbMeshes.SelectedValue is SpecimenTableColumnInfo stcimesh)
            {
                configuration.MeshColumnName = stcimesh.ColumnName;
            }
            else
            {
                MessageBox.Show("There is no mesh column selected from the specimen table.");
                return;
            }

            configuration.BaseMeshIndex = cmbBase.SelectedIndex;

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
