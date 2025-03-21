using System;
using System.Collections.Generic;
using System.Globalization;
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

namespace Warp9.Controls
{
    /// <summary>
    /// Interaction logic for ScatterPlotControl.xaml
    /// </summary>
    public partial class ScatterPlotControl : UserControl
    {
        public ScatterPlotControl()
        {
            InitializeComponent();
        }

        protected override void OnRender(DrawingContext ctx)
        {
            base.OnRender(ctx);
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
    }
}
