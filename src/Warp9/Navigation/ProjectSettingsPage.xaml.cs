using System.Windows.Controls;
using Warp9.Model;
using Warp9.ProjectExplorer;

namespace Warp9.Navigation
{
    /// <summary>
    /// Interaction logic for ProjectSettingsPage.xaml
    /// </summary>
    public partial class ProjectSettingsPage : Page, IWarp9View
    {
        public ProjectSettingsPage()
        {
            InitializeComponent();
        }

        Warp9ViewModel? viewModel;

        public void AttachViewModel(Warp9ViewModel vm)
        {
            viewModel = vm;
            cmbExtRefPolicy.SelectedIndex = (int)viewModel.Project.Settings.ExternalReferencePolicy;
      
        }

        public void DetachViewModel()
        {
            viewModel = null;
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (viewModel is not null)
            {
                viewModel.Project.Settings.ExternalReferencePolicy = (ProjectExternalReferencePolicy)cmbExtRefPolicy.SelectedIndex;
            }
        }
    }
}
