using SharpDX.Direct3D11;
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
using Warp9;

namespace Warp9.Controls
{
    /// <summary>
    /// Interaction logic for HistogramAndLutControl.xaml
    /// </summary>
    public partial class HistogramAndLutControl : UserControl
    {
        public HistogramAndLutControl()
        {
            InitializeComponent();
        }

        float x0 = 0, x1 = 1;
        Lut lut = Lut.Create(256, Lut.FastColors);
        float[] scalarField = Array.Empty<float>();
        int[] hist = Array.Empty<int>();
        int histMax = 1;

        public float X0
        {
            get { return x0; }
            set { x0 = value; Updated(); }
        }

        public float X1
        {
            get { return x1; }
            set { x1 = value; Updated(); }
        }

        public Lut Lut
        {
            get { return lut; }
            set { lut = value; Updated(); }
        }

        public float[] ScalarField
        {
            get { return scalarField; }
            set { scalarField = value; Updated(); }
        }

        public void SetAll(float[] values, Lut lut, float x0, float x1)
        {
            scalarField = values;
            this.lut = lut;
            this.x0 = x0;
            this.x1 = x1;
            Updated();
        }

        protected override void OnRender(DrawingContext ctx)
        {
            base.OnRender(ctx);

            Pen pen = new Pen(new SolidColorBrush(Color.FromRgb(255, 255, 255)), 1);

            int w = Math.Min((int)gridMain.ActualWidth, hist.Length);
            double h = ActualHeight;
            for (int i = 0; i < w; i++)
            {
                ctx.DrawLine(pen,
                    new Point(i, h),
                    new Point(i, h - h * (double)hist[i] / histMax));
            }
        }

        private void Updated()
        {
            int numBins = (int)gridMain.ActualWidth;
            if (hist.Length != numBins)
            {
                hist = new int[numBins];
            }
            else
            {
                for (int i = 0; i < numBins; i++)
                    hist[i] = 0;
            }

            MiscUtils.Histogram(scalarField.AsSpan(), numBins, x0, x1, hist.AsSpan(), out _, out _);
            histMax = Math.Max(1, hist.Max());

            InvalidateVisual();
        }
    }
}
