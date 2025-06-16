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
    /// Interaction logic for PcaSynthMeshSideBar.xaml
    /// </summary>
    public partial class PcaSynthMeshSideBar : Page
    {
        public PcaSynthMeshSideBar(PcaSynthMeshViewerContent content)
        {
            InitializeComponent();
            Content = content;
            DataContext = content;
        }

        PcaSynthMeshViewerContent Content { get; init; }

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

        public void UpdateScatterplot(ReadOnlySpan<float> x, ReadOnlySpan<float> y)
        {
            scatPca.SetData(x, y);
        }

        private void histField_ScaleHover(object sender, float? e)
        {
            Content.MeshScaleHover(e);
        }

        private void scatPca_PlotPosChanged(object sender, Controls.ScatterPlotPosInfo e)
        {
            Content.ScatterPlotPosChanged(e);
        }
    }
}
