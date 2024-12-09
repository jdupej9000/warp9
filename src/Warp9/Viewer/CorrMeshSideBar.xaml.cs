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

namespace Warp9.Viewer
{
    /// <summary>
    /// Interaction logic for CorrMeshSideBar.xaml
    /// </summary>
    public partial class CorrMeshSideBar : Page
    {
        public CorrMeshSideBar(CorrMeshViewerContent content)
        {
            InitializeComponent();
            Content = content;
            DataContext = content;
        }

        CorrMeshViewerContent Content { get; init; }

        private void btnSpecimenInc_Click(object sender, RoutedEventArgs e)
        {
            if(int.TryParse(txtSpecimen.Text, out int specimen))
                txtSpecimen.Text = (specimen + 1).ToString();
        }

        private void btnSpecimenDec_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(txtSpecimen.Text, out int specimen))
                txtSpecimen.Text = (specimen - 1).ToString();
        }
    }
}
