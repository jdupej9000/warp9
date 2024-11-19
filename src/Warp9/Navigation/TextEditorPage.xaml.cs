using System.Windows.Controls;
using Warp9.ProjectExplorer;

namespace Warp9.Navigation
{

    public partial class TextEditorPage : Page, IWarp9View
    {
        public TextEditorPage()
        {
            InitializeComponent();
        }

        Warp9ViewModel? viewModel;

        public void AttachViewModel(Warp9ViewModel vm)
        {
            viewModel = vm;
            txtEdit.Text = viewModel.Project.Settings.Comment ?? string.Empty;
        }

        public void DetachViewModel()
        {
            viewModel = null;
            txtEdit.Text = string.Empty;
        }

        private void txtEdit_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (viewModel is not null)
            {
                viewModel.Project.Settings.Comment = txtEdit.Text;
            }
        }
    }
}
