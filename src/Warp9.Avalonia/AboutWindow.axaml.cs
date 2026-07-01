using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System;
using Warp9.Native;
using Avalonia.Interactivity;
using System.Text;

namespace Warp9.Avalonia;

public partial class AboutWindow : Window
{
    public AboutWindow()
    {
        InitializeComponent();
    }

    private void Window_Loaded(object? sender, RoutedEventArgs e)
    {
        PopulateDebugItems();
    }

    private void PopulateDebugItems()
    {
        // Force WarpCore.dll to be loaded.
        WarpCore.GetInfoString(WarpCoreInfoIndex.VERSION);

        Dispatcher.Invoke(() =>
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("[ Environment ]");
            sb.AppendLine(".NET version     : " + Environment.Version.ToString());
            sb.AppendLine("OS version       : " + Environment.OSVersion.ToString());
            sb.AppendLine("Machine name     : " + Environment.MachineName);
            sb.AppendLine("Avalonia version : " + typeof(AvaloniaObject).Assembly.GetName().Version.ToString());

            sb.AppendLine();
            sb.AppendLine("[ Warp9 ]");
            foreach (WarpCoreInfoIndex idx in Enum.GetValues<WarpCoreInfoIndex>())
            {
                sb.AppendLine(idx.ToString() + " : " + WarpCore.GetInfoString(idx));
            }

            txtInfo.Text = sb.ToString();
        });
    }
}