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
using Warp9.Utils;

namespace Warp9.Forms
{
    public enum ColumnImportType
    {
        Integer,
        Real,
        String,
        Factor,
        Boolean,
        Image,
        Mesh,
        Landmarks,
        Matrix,
        Landmarks2DAos,
        Landmarks2DSoa,
        Landmarks3DAos,
        Landmarks3DSoa
    }

    public record StiwTypeComboItem(string name, ColumnImportType type)
    {
        public string Name { get; init; } = name;
        public ColumnImportType Type { get; init; } = type;
    };

    /// <summary>
    /// Interaction logic for SpecimenTableImportWindow.xaml
    /// </summary>
    public partial class SpecimenTableImportWindow : Window
    {
        public SpecimenTableImportWindow()
        {
            InitializeComponent();
        }

        IUntypedTableProvider? Importer;

        public void AttachImporter(IUntypedTableProvider importer)
        {
            Importer = importer;
            DataContext = importer;
            UpdateColumns();
        }

        private void UpdateColumns()
        {
            dataCsv.Columns.Clear();

            if (Importer is null) return;

            string[]? data = Importer.ParsedData.FirstOrDefault();
            if (data is null) return;

            for (int i = 0; i < data.Length; i++)
            {
                DataGridTextColumn col = new DataGridTextColumn
                {
                    Header = string.Format("Column {0}", i + 1),
                    Binding = new Binding("[" + i.ToString() + "]"),
                    IsReadOnly = true,
                    CanUserReorder = false,
                    CanUserSort = false
                };

                dataCsv.Columns.Add(col);
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            cmbType.Items.Add(new StiwTypeComboItem("Integer", ColumnImportType.Integer));
            cmbType.Items.Add(new StiwTypeComboItem("Real", ColumnImportType.Real));
            cmbType.Items.Add(new StiwTypeComboItem("String", ColumnImportType.String));
            cmbType.Items.Add(new StiwTypeComboItem("Factor", ColumnImportType.Factor));
            cmbType.Items.Add(new StiwTypeComboItem("Boolean", ColumnImportType.Boolean));
            cmbType.Items.Add(new StiwTypeComboItem("Image file", ColumnImportType.Image));
            cmbType.Items.Add(new StiwTypeComboItem("Mesh file", ColumnImportType.Mesh));
            cmbType.Items.Add(new StiwTypeComboItem("Landmarks file", ColumnImportType.Landmarks));
            cmbType.Items.Add(new StiwTypeComboItem("Matrix file", ColumnImportType.Matrix));
            cmbType.Items.Add(new StiwTypeComboItem("Direct landmarks 2D (xyxy)", ColumnImportType.Landmarks2DAos));
            cmbType.Items.Add(new StiwTypeComboItem("Direct landmarks 2D (xxyy)", ColumnImportType.Landmarks2DSoa));
            cmbType.Items.Add(new StiwTypeComboItem("Direct landmarks 3D (xyzxyz)", ColumnImportType.Landmarks3DAos));
            cmbType.Items.Add(new StiwTypeComboItem("Direct landmarks 3D (xxyyzz)", ColumnImportType.Landmarks3DSoa));
        }
    }
}
