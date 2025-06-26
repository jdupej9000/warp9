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
using Warp9.Data;

namespace Warp9.Viewer
{
    /// <summary>
    /// Interaction logic for DcaDiagnosticsSideBar.xaml
    /// </summary>
    public partial class DcaDiagnosticsSideBar : Page, IViewerPage
    {
        public DcaDiagnosticsSideBar(DcaDiagnosticsViewerContent content)
        {
            InitializeComponent();
            Content = content;
            DataContext = content;
        }

        DcaDiagnosticsViewerContent Content { get; init; }

        public void SetHist(float[] values, Lut lut, float x0, float x1)
        {
            histField.SetAll(values, lut, x0, x1);
        }

        public void SetLut(Lut lut)
        {
            histField.Lut = lut;
        }

        public void SetRange(float x0, float x1)
        {
            histField.SetRange(x0, x1);
        }
        private void histField_ScaleHover(object sender, float? e)
        {
            Content.MeshScaleHover(e);
        }
    }
}
