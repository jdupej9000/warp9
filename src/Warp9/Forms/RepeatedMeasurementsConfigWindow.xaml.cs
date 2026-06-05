using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Warp9.Model;
using Warp9.Processing;
using Warp9.Themes;
using Warp9.Viewer;

namespace Warp9.Forms
{
    public class RepeatedMeasurementsOperationRadioConverter : RadioBoolToIntConverter<RepeatedMeasurementsOperation>
    {
    };

    /// <summary>
    /// Interaction logic for RepeatedMeasurementsConfigWindow.xaml
    /// </summary>
    public partial class RepeatedMeasurementsConfigWindow : Window
    {
        public RepeatedMeasurementsConfigWindow(RepeatedMeasurementsCfg cfg)
        {
            InitializeComponent();
            DataContext = cfg;
            config = cfg;
        }

        RepeatedMeasurementsCfg config;

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            config.UpdateSelection();
            DialogResult = true;
        }
    }
}
