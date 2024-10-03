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
using Warp9.Model;
using Warp9.ProjectExplorer;

namespace Warp9.Navigation
{
    public record SpecimenTableColumnInfo(string name, string type)
    {
        public string Name { get; init; } = name;
        public string Type { get; init; } = type;
    }

    public partial class SpecimenTablePage : Page, IWarp9View
    {
        public SpecimenTablePage()
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

        public void ShowEntry(int idx)
        {
            dataMain.Columns.Clear();

            if (viewModel is null || !viewModel.Project.Entries.TryGetValue(idx, out ProjectEntry? entry))
                throw new InvalidOperationException();

            SpecimenTable table = entry.Payload.Table ?? throw new InvalidOperationException();
            dataMain.ItemsSource = table.GetRows();

            foreach (var kvp in table.Columns)
            {
                DataGridTextColumn col = new DataGridTextColumn();
                col.Header = new SpecimenTableColumnInfo(kvp.Key, kvp.Value.ColumnType.ToString());
                col.Binding = new Binding("[" + kvp.Key + "]");
                dataMain.Columns.Add(col);
            }
        }
    }
}
