using Microsoft.Windows.Themes;
using System.IO;
using System.Windows;
using Warp9.Themes;

namespace Warp9
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {   
            ThemesController.SetTheme((ThemeType)Options.Instance.ThemeIndex);

            App.Current.DispatcherUnhandledException += Current_DispatcherUnhandledException;
        }

        private void Current_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            string reportPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "warp9-last-error.txt");
            File.WriteAllText(reportPath, e.ToString());
        }
    }
}
