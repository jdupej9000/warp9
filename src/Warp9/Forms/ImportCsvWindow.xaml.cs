﻿using System;
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
using Warp9.Navigation;
using Warp9.Utils;

namespace Warp9.Forms
{
    /// <summary>
    /// Interaction logic for ImportCsvWindow.xaml
    /// </summary>
    public partial class ImportCsvWindow : Window
    {
        public ImportCsvWindow()
        {
            InitializeComponent();
        }

        CsvImporter? Importer;

        public void AttachImporter(CsvImporter importer)
        {
            Importer = importer;
            DataContext = importer;
            importer.PropertyChanged += Importer_PropertyChanged;
        }

        private void Importer_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            dataCsv.Columns.Clear();

            if (Importer is null) return;

            string[]? data = Importer.ParsedData.FirstOrDefault();
            if (data is null) return;

            for (int i = 0; i < data.Length; i++)
            {
                DataGridTextColumn col = new DataGridTextColumn
                {
                    Header = string.Format("Column {0}", i+1),
                    Binding = new Binding("[" + i.ToString() + "]"),
                    IsReadOnly = true,
                    CanUserReorder = false,
                    CanUserSort = false
                };

                dataCsv.Columns.Add(col);
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (Importer is not null)
                Importer.PropertyChanged -= Importer_PropertyChanged;
        }
    }
}
