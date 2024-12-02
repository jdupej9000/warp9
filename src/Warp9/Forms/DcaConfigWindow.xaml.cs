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

        public DcaConfiguration Config => configuration;
        public Project Project => project;
        public IEnumerable<SpecimenTableColumnInfo> AllowedMeshColumns =>
            ModelUtils.EnumerateAllSpecimenTableColumns(project)
                .Where((x) => x.Column.ColumnType == SpecimenTableColumnType.Mesh);

        public IEnumerable<SpecimenTableColumnInfo> AllowedLandmarksColumns =>
           ModelUtils.EnumerateAllSpecimenTableColumns(project)
               .Where((x) => x.Column.ColumnType == SpecimenTableColumnType.PointCloud);

        public void Attach(Project proj, DcaConfiguration cfg)
        {
            project = proj;
            configuration = cfg;
            DataContext = cfg;

            cmbMeshes.Items.Clear();
            foreach(var col in AllowedMeshColumns)
                cmbMeshes.Items.Add(col);

            cmbLandmarks.Items.Clear();
            foreach (var col in AllowedLandmarksColumns)
                cmbLandmarks.Items.Add(col);

            if(cmbMeshes.Items.Count > 0)
                cmbMeshes.SelectedIndex = 0;

            if (cmbLandmarks.Items.Count > 0)
                cmbLandmarks.SelectedIndex = 0;
        }

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
