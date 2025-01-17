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
using System.Security.Policy;
using System.Globalization;

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

        float x0 = 0, x1 = 1, cursorPos = -1;
        bool cursorVisible = false;
        Lut lut = Lut.Create(256, Lut.FastColors);
        float[] scalarField = Array.Empty<float>();
        int[] hist = Array.Empty<int>();
        StreamGeometry? geomHist = null;
        Brush? brushHist = null;
        int histMax = 1;
        const double AxisMargin = 16;
        bool exitNoDisappear = false;

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

        public float CursorPos => cursorPos;
        public bool IsCursorVisible => cursorVisible;

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

        public void SetRange(float x0, float x1)
        {
            this.x0 = x0;
            this.x1 = x1;
            Updated();
        }

        protected override void OnRender(DrawingContext ctx)
        {
            base.OnRender(ctx);

            Brush fill = Themes.ThemesController.GetBrush("ThemeColor.Control.EditBackground");
            Brush borderBrush = Themes.ThemesController.GetBrush("ThemeColor.Control.BorderLight");
            Pen borderPen = new Pen(borderBrush, 1);

            ctx.DrawRectangle(fill, borderPen,
                new Rect(0, 0, ActualWidth, ActualHeight - AxisMargin));

            if (geomHist is not null && brushHist is not null)
                ctx.DrawGeometry(brushHist, borderPen, geomHist);

            double w = ActualWidth;
            double axisY = ActualHeight - AxisMargin + 1;
            Brush axisBrush = Themes.ThemesController.GetBrush("ABrush.Foreground.Disabled");
            Pen axisPen = new Pen(axisBrush, 1);
            
            if (Math.Min(x0, x1) < 0 && Math.Max(x0, x1) > 0)
            {
                double zeroAt = -X0 / (X1 - X0) * w;
                ctx.DrawLine(axisPen, new Point(zeroAt, axisY), new Point(zeroAt, axisY + 3));

                FormattedText txt = new FormattedText("0", CultureInfo.CurrentCulture,
                   FlowDirection.LeftToRight,
                   new Typeface(FontFamily, FontStyles.Normal, FontWeights.Regular, FontStretches.Normal),
                   9, axisBrush, 1);

                ctx.DrawText(txt, new Point(zeroAt - txt.Width * 0.5, axisY + 4));
            }

            if (cursorVisible)
            {
                double x = (cursorPos - X0) / (X1 - X0) * w;
                ctx.DrawLine(axisPen, new Point(x, 0), new Point(x, axisY + 3));

                FormattedText txt = new FormattedText(cursorPos.ToString("F3"), CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    new Typeface(FontFamily, FontStyles.Normal, FontWeights.Regular, FontStretches.Normal),
                    9, axisBrush, 1);

                ctx.DrawText(txt, new Point(x - txt.Width * 0.5, axisY + 4));
            }
        }

        private void Control_MouseMove(object sender, MouseEventArgs e)
        {
            float t = (float)(x0 + e.GetPosition(this).X / ActualWidth * (x1 - x0));
            cursorPos = t;
            cursorVisible = true;
            ScaleHover?.Invoke(this, t);
            InvalidateVisual();
        }

        private void Control_MouseDown(object sender, MouseButtonEventArgs e)
        {
            exitNoDisappear = !exitNoDisappear;
        }

        private void Control_MouseLeave(object sender, MouseEventArgs e)
        {
            if (!exitNoDisappear)
            {
                cursorVisible = false;
                ScaleHover?.Invoke(this, null);
                InvalidateVisual();
            }
        }

        private void Updated()
        {
            int numBins = (int)gridMain.ActualWidth;

            if (scalarField.Length > 0 && numBins > 0)
            {
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
                double h = ActualHeight - AxisMargin;

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
            }

            InvalidateVisual();
        }
    }
}
