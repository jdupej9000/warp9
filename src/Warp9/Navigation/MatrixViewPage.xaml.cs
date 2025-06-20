using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Warp9.Model;
using Warp9.ProjectExplorer;

namespace Warp9.Navigation
{
    /// <summary>
    /// Interaction logic for MatrixViewPage.xaml
    /// </summary>
    public partial class MatrixViewPage : Page, IWarp9View
    {
        public MatrixViewPage()
        {
            InitializeComponent();
        }

        Warp9ViewModel? viewModel;

        public void AttachViewModel(Warp9ViewModel vm)
        {
            viewModel = vm;
        }

        public void DetachViewModel()
        {
            viewModel = null;
        }

        public void SetMatrices(params MatrixViewProvider[] providers)
        {
            tabPages.Items.Clear();
            for (int i = 0; i < providers.Length; i++)
            {
                tabPages.Items.Add(new TabItem 
                { 
                    Header = providers[i].Name,
                    Tag = providers[i] 
                });
            }

            if (providers.Length > 0)
                ShowMatrix(providers[0]);
        }

        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (tabPages.SelectedItem is TabItem ti &&
                ti.Tag is MatrixViewProvider mvp)
            {
                ShowMatrix(mvp);
            }
        }

        private void btnExport_Click(object sender, RoutedEventArgs e)
        {
            if (tabPages.SelectedItem is TabItem ti &&
                ti.Tag is MatrixViewProvider mvp)
            {
                SaveFileDialog dlg = new SaveFileDialog();
                dlg.Filter = "Comma-separated values (*.csv)|*.csv";

                DialogResult res = dlg.ShowDialog();
                if (res == DialogResult.OK)
                {
                    throw new NotImplementedException();
                }
            }
        }

        private void ShowMatrix(MatrixViewProvider mvp)
        {
            dataMain.Columns.Clear();
          
            int colIndex = mvp.FirstColumnIndex;
            foreach (MatrixColumnViewProvider mcvp in mvp.Columns)
            {
                DataGridTextColumn dgcol = new DataGridTextColumn
                {
                    Header = mcvp,
                    CanUserReorder = false,
                    IsReadOnly = true,
                    Binding = new System.Windows.Data.Binding($"[{colIndex}]")
                };
                dataMain.Columns.Add(dgcol);
                colIndex++;
            }

            dataMain.ItemsSource = mvp;
        }
    }
}
