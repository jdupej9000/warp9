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
using System.Windows.Navigation;
using System.Windows.Shapes;
using Warp9.ProjectExplorer;

namespace Warp9.Navigation
{
 
    public partial class ProjectMainPage : Page, IWarp9View
    {
        public ProjectMainPage()
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
                viewModel.Project.Settings.Comment = txtEdit.Text;
        }
    }
}
