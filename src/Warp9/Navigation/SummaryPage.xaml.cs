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
    /// <summary>
    /// Interaction logic for SummaryPage.xaml
    /// </summary>
    public partial class SummaryPage : Page, IWarp9View
    {
        public SummaryPage()
        {
            InitializeComponent();
        }

        Warp9ViewModel? viewModel;

        public FlowDocument Document
        {
            get { return docMain.Document; }
            set { docMain.Document = value; }
        }

        public void AttachViewModel(Warp9ViewModel vm)
        {
            viewModel = vm;
        }

        public void DetachViewModel()
        {
            viewModel = null;
        }
    }
}
