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
using System.Windows.Shapes;
using Warp9.Model;

namespace Warp9.Forms
{
    /// <summary>
    /// Interaction logic for ColumnEditWindow.xaml
    /// </summary>
    public partial class ColumnEditWindow : Window
    {
        public ColumnEditWindow()
        {
            InitializeComponent();
            DataContext = this;
            cmbType.ItemsSource = Enum.GetValues<SpecimenTableColumnType>();
        }

        public string ColumnName { get; set; } = string.Empty;
        public SpecimenTableColumnType ColumnType { get; set; }
        public string ColumnLevelsString { get; set; }
        public string[] ColumnLevels
        {
            get { return ColumnLevelsString.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries); }
            set { ColumnLevelsString = string.Join(",", value); }
        }
        

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
