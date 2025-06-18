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
    public record ScatterPlotPosInfo(Vector2 Pos);

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
        Vector2 Range0 => new Vector2(RangeX.X, RangeY.X);
        Vector2 Range1 => new Vector2(RangeX.Y, RangeY.Y);

        bool dragging = false;

        public Brush PlotBackground
        {
            get { return (Brush)GetValue(PlotBackgroundProperty); }
            set { SetValue(PlotBackgroundProperty, value); }
        }

        public Brush PlotBorder
        {
            get { return (Brush)GetValue(PlotBorderProperty); }
            set { SetValue(PlotBorderProperty, value); }
        }
        public Brush PlotForeground
        {
            get { return (Brush)GetValue(PlotForegroundProperty); }
            set { SetValue(PlotForegroundProperty, value); }
        }

        public event EventHandler<ScatterPlotPosInfo>? PlotPosChanged;

        public void SetData(ReadOnlySpan<float> x, ReadOnlySpan<float> y)
        {
            MakeRange(x, y);
            scatterPoints = MakeScatterPlot(x, y);
            InvalidateVisual();
            NotifyPosChange(0.5f * new Vector2((float)ActualWidth, (float)ActualHeight));
        }

        protected override void OnRender(DrawingContext ctx)
        {
            //base.OnRender(ctx);
            Brush fill = PlotBackground;
            Brush borderBrush = PlotBorder;
            Brush dots = PlotForeground;

            //Brush fill = new SolidColorBrush(Color.FromRgb(0, 0, 0));
            //Brush borderBrush = new SolidColorBrush(Color.FromRgb(128, 128, 128));
            //Brush dots = new SolidColorBrush(Color.FromRgb(255, 255, 255));

            Pen borderPen = new Pen(borderBrush, 1);
            Pen dotsPen = new Pen(dots, 1);

            ctx.DrawRoundedRectangle(fill, borderPen,
                new Rect(0, 0, ActualWidth, ActualHeight),
                4, 4);
            //ctx.DrawRectangle(fill, borderPen,
            //    new Rect(0, 0, ActualWidth, ActualHeight-1));

            if (scatterGeometry is not null)
                ctx.DrawGeometry(borderBrush, borderPen, scatterGeometry);

            if (RangeX.X < 0 && RangeX.Y > 0)
            {
                float px = (float)(-RangeX.X / (RangeX.Y - RangeX.X) * ActualWidth);
                ctx.DrawLine(borderPen, new Point(px, 0), new Point(px, ActualHeight)); 
            }

            if (RangeY.X < 0 && RangeY.Y > 0)
            {
                float py = (float)(-RangeY.X / (RangeY.Y - RangeY.X) * ActualHeight);
                ctx.DrawLine(borderPen, new Point(0, py), new Point(ActualWidth, py));
            }

            if (scatterPoints is not null)
            {
                foreach (Vector2 pt in scatterPoints)
                    ctx.DrawRectangle(dots, dotsPen, new Rect(pt.X - 0.5f, pt.Y - 0.5f, 1, 1));
            }
        }

        private void NotifyPosChange(Vector2 posScreen)
        {
            Vector2 xy = Range0 + posScreen / new Vector2((float)ActualWidth, (float)ActualHeight) * (Range1 - Range0);
            PlotPosChanged?.Invoke(this, new ScatterPlotPosInfo(xy));
        }


        private void Control_MouseMove(object sender, MouseEventArgs e)
        {
            if (dragging)
            {
                Point pos = e.MouseDevice.GetPosition(this);
                NotifyPosChange(new Vector2((float)pos.X, (float)pos.Y));
            }
        }

        private void Control_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if(e.LeftButton == MouseButtonState.Pressed)
                dragging = true;
        }

        private void Control_MouseLeave(object sender, MouseEventArgs e)
        {
            dragging = false;
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
