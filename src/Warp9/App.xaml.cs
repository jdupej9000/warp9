using Microsoft.Windows.Themes;
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
        }
    }
}
