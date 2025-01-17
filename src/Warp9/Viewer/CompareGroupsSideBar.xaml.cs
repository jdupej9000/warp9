using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
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
using Warp9.Data;

namespace Warp9.Viewer
{
    /// <summary>
    /// Interaction logic for CompareGroupsSideBar.xaml
    /// </summary>
    public partial class CompareGroupsSideBar : Page
    {
        public CompareGroupsSideBar(CompareGroupsViewerContent content)
        {
            InitializeComponent();
            Content = content;
            DataContext = content;
        }

        CompareGroupsViewerContent Content { get; init; }

        private void GroupA_Click(object sender, RoutedEventArgs e)
        {
            Content.InvokeGroupSelectionDialog(0);
        }

        private void GroupB_Click(object sender, RoutedEventArgs e)
        {
            Content.InvokeGroupSelectionDialog(1);
        }

        private void GroupSwap_Click(object sender, RoutedEventArgs e)
        {
            Content.SwapGroups();
        }

        public void SetHist(float[] values, Lut lut, float x0, float x1)
        {
            histField.SetAll(values, lut, x0, x1);
        }

        public void SetRange(float x0, float x1)
        {
            histField.SetRange(x0, x1);
        }

        public void SetLut(Lut lut)
        {
            histField.Lut = lut;
        }

        private void histField_ScaleHover(object sender, float? e)
        {
            Content.MeshScaleHover(e);
        }
    }
}
