using System.Collections.ObjectModel;
using System.Text;
using System.Windows;
using Warp9.Native;

namespace Warp9
{
    public struct WarpCoreDebugItem
    {
        public WarpCoreDebugItem(string id, string data)
        {
            Id = id;
            Data = data;
        }

        public string Id { get; set; }
        public string Data { get; set; }
    }

    public partial class AboutWindow : Window
    {
        public AboutWindow()
        {
            InitializeComponent();
        }

        protected ObservableCollection<WarpCoreDebugItem> debugItems = new ObservableCollection<WarpCoreDebugItem>();

        private void PopulateDebugItems()
        {
            const int MaxDataLen = 1024;
            StringBuilder sb = new StringBuilder(MaxDataLen);

            foreach (WarpCoreInfoIndex idx in Enum.GetValues(typeof(WarpCoreInfoIndex)))
            {
                int len = WarpCore.wcore_get_info((int)idx, sb, MaxDataLen);
                listDebug.Items.Add(new WarpCoreDebugItem(
                    idx.ToString(), sb.ToString()));
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                PopulateDebugItems();
            }
            catch (DllNotFoundException)
            {
                MessageBox.Show("WarpCore is down.");
            }
        }
    }
}