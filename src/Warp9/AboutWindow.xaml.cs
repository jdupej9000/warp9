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
            // Force WarpCore.dll to be loaded.
            WarpCore.GetInfoString(WarpCoreInfoIndex.VERSION);

            Dispatcher.Invoke(() =>
            {
                listDebug.Items.Add(new WarpCoreDebugItem(
                    ".NET version", Environment.Version.ToString()));
                listDebug.Items.Add(new WarpCoreDebugItem(
                    "OS version", Environment.OSVersion.ToString()));
                listDebug.Items.Add(new WarpCoreDebugItem(
                    "Machine name", Environment.MachineName));

                foreach (WarpCoreInfoIndex idx in Enum.GetValues<WarpCoreInfoIndex>())
                {
                  
                    listDebug.Items.Add(new WarpCoreDebugItem(
                        idx.ToString(), WarpCore.GetInfoString(idx)));
                }
            });
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                Task.Run(() => PopulateDebugItems());
            }
            catch (DllNotFoundException)
            {
                MessageBox.Show("WarpCore is down.");
            }
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }
    }
}