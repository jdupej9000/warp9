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
        StreamGeometry? geomHist = null;
        Brush? brushHist = null;
        int histMax = 1;

        public event EventHandler<float?> ScaleHover;

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

            if(geomHist is not null && brushHist is not null)
                ctx.DrawGeometry(brushHist, null, geomHist);
        }

        private void Control_MouseMove(object sender, MouseEventArgs e)
        {
            float t = (float)(x0 + e.GetPosition(this).X / ActualWidth * (x1 - x0));
            ScaleHover?.Invoke(this, t);
        }

        private void Control_MouseLeave(object sender, MouseEventArgs e)
        {
            ScaleHover?.Invoke(this, null);
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

            const int NumGradientStops = 64;
            GradientStopCollection stops = new GradientStopCollection();
            for (int i = 0; i < NumGradientStops; i++)
            {
                float t = (float)i / (float)(NumGradientStops - 1);
                System.Drawing.Color col = lut.Sample(t);
                stops.Add(new GradientStop(Color.FromRgb(col.R, col.G, col.B), t));
            }

            brushHist = new LinearGradientBrush(stops, 0);

            geomHist = new StreamGeometry();
            int w = Math.Min((int)gridMain.ActualWidth, hist.Length);
            double h = ActualHeight;

            using (StreamGeometryContext sgc = geomHist.Open())
            {
                sgc.BeginFigure(new Point(0, h), true, true);

                Point[] pts = new Point[w + 1];
                for (int i = 0; i < w; i++)
                {
                    double barHeight = h * (double)hist[i] / histMax;
                    pts[i] = new Point(i, h - h * (double)hist[i] / histMax);
                }
                pts[w] = new Point(w, h);

                sgc.PolyLineTo(pts, false, true);
            }

            InvalidateVisual();
        }
    }
}
