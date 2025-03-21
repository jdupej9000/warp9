using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
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
using Warp9.Utils;

namespace Warp9.Controls
{
    /// <summary>
    /// Interaction logic for ScatterPlotControl.xaml
    /// </summary>
    public partial class ScatterPlotControl : UserControl
    {
        public static readonly DependencyProperty PlotBackgroundProperty = DependencyProperty.Register(
           "PlotBackground", typeof(Brush), typeof(ScatterPlotControl), new FrameworkPropertyMetadata(
               defaultValue: new SolidColorBrush(),
               flags: FrameworkPropertyMetadataOptions.AffectsRender));


        public static readonly DependencyProperty PlotBorderProperty = DependencyProperty.Register(
            "PlotBorder", typeof(Brush), typeof(ScatterPlotControl), new FrameworkPropertyMetadata(
                defaultValue: new SolidColorBrush(),
                flags: FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty PlotForegroundProperty = DependencyProperty.Register(
            "PlotForeground", typeof(Brush), typeof(ScatterPlotControl), new FrameworkPropertyMetadata(
                defaultValue: new SolidColorBrush(),
                flags: FrameworkPropertyMetadataOptions.AffectsRender));


        public ScatterPlotControl()
        {
            InitializeComponent();
        }

        StreamGeometry? scatterGeometry = null;
        Vector2[]? scatterPoints = null;
        Vector2 RangeX { get; set; } = new Vector2(-1, 1);
        Vector2 RangeY { get; set; } = new Vector2(-1, 1);

        public Brush PlotBackground { get; set; } = new SolidColorBrush();
        public Brush PlotBorder { get; set; } = new SolidColorBrush();
        public Brush PlotForeground { get; set; } = new SolidColorBrush();

        public void SetData(ReadOnlySpan<float> x, ReadOnlySpan<float> y)
        {
            MakeRange(x, y);
            scatterPoints = MakeScatterPlot(x, y);
            InvalidateVisual();
        }

        protected override void OnRender(DrawingContext ctx)
        {
            //base.OnRender(ctx);
            //Brush fill = PlotBackground ?? Background;
            //Brush borderBrush = PlotBorder ?? BorderBrush;
            // Brush dots = PlotForeground ?? new SolidColorBrush(Color.FromRgb(255, 255, 255));

            Brush fill = new SolidColorBrush(Color.FromRgb(0, 0, 0));
            Brush borderBrush = new SolidColorBrush(Color.FromRgb(128, 128, 128));
            Brush dots = new SolidColorBrush(Color.FromRgb(255, 255, 255));

            Pen borderPen = new Pen(borderBrush, 1);
            Pen dotsPen = new Pen(dots, 1);


            ctx.DrawRectangle(fill, borderPen,
                new Rect(0, 0, ActualWidth, ActualHeight));

            if (scatterGeometry is not null)
                ctx.DrawGeometry(borderBrush, borderPen, scatterGeometry);

            if (scatterPoints is not null)
            {
                foreach (Vector2 pt in scatterPoints)
                    ctx.DrawRectangle(dots, dotsPen, new Rect(pt.X - 0.5f, pt.Y - 0.5f, 1, 1));
            }
        }

        private void Control_MouseMove(object sender, MouseEventArgs e)
        {
           
        }

        private void Control_MouseDown(object sender, MouseButtonEventArgs e)
        {
        }

        private void Control_MouseLeave(object sender, MouseEventArgs e)
        {
        }


        private void MakeRange(ReadOnlySpan<float> x, ReadOnlySpan<float> y)
        {
            RangeX = MiscUtils.Range(x);
            RangeY = MiscUtils.Range(y);
        }


        private Vector2[] MakeScatterPlot(ReadOnlySpan<float> x, ReadOnlySpan<float> y)
        {
            Vector2 rangex = RangeX;
            Vector2 rangey = RangeY;

            Vector2 p0 = new Vector2(rangex.X, rangey.X);
            Vector2 pp = new Vector2((float)gridMain.ActualWidth, (float)gridMain.ActualHeight) /
                new Vector2(rangex.Y - rangex.X, rangey.Y - rangey.X);

            int n = x.Length;
            Vector2[] ret = new Vector2[n];

            for (int i = 0; i < n; i++)
                ret[i] = (new Vector2(x[i], y[i]) - p0) * pp;

            return ret;
        }
    }
}
