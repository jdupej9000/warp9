using System.IO;
using System.Reflection;
using System.Text;
using System.Windows;
using Warp9.Native;
using Warp9.Themes;

namespace Warp9
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        static DateTime StartTime = DateTime.Now;

        private void Application_Startup(object sender, StartupEventArgs e)
        {   
            ThemesController.SetTheme((ThemeType)Options.Instance.ThemeIndex);

            App.Current.DispatcherUnhandledException += Current_DispatcherUnhandledException;
        }

        private void Current_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("+--------------------+");
            sb.AppendLine("| warp9 Error Report |");
            sb.AppendLine("+--------------------+");
            sb.AppendLine();

            sb.AppendLine("Time Started : " + StartTime.ToString());
            sb.AppendLine("Time Crashed : " + DateTime.Now.ToString());
            sb.AppendLine(".NET version : " + Environment.Version.ToString());
            sb.AppendLine("OS version   : " + Environment.OSVersion.ToString());

            sb.AppendLine();
            sb.AppendLine("WarpCore info");
            sb.AppendLine("---------------------------");
            try
            {
                const int MaxDataLen = 1024;
                StringBuilder sbwcore = new StringBuilder(MaxDataLen);
                foreach (WarpCoreInfoIndex idx in Enum.GetValues(typeof(WarpCoreInfoIndex)))
                {
                    int len = WarpCore.wcore_get_info((int)idx, sbwcore, MaxDataLen);
                    string sidx = idx.ToString();
                    sb.AppendLine(sidx.PadRight(25) + ": " + sbwcore.ToString());
                }
            }
            catch (DllNotFoundException)
            {
                sb.AppendLine("WarpCore could not be loaded.");
            }
            catch (Exception ee)
            {
                sb.AppendLine("Failed to query system information: " + ee.Message);
                sb.AppendLine(ee.StackTrace);
                sb.AppendLine();
            }

            sb.AppendLine();
            sb.AppendLine("Unhandled exception details");
            sb.AppendLine("---------------------------");
            sb.AppendLine(e.Exception.Message);
            sb.AppendLine();
            sb.AppendLine(e.Exception.StackTrace ?? "(no stack trace)");
            sb.AppendLine();

            sb.AppendLine("Loaded modules");
            sb.AppendLine("--------------");
            foreach (Module m in Assembly.GetExecutingAssembly().GetLoadedModules())
            {
                sb.AppendLine($"* '{m.Name}' in '{m.Assembly.FullName ?? ""}'");
            }
            sb.AppendLine();


            string baseDir = AppDomain.CurrentDomain.BaseDirectory;            
            sb.AppendLine("Application folder listing");
            sb.AppendLine("--------------------------");
            foreach (string l in ListFiles(baseDir))
                sb.AppendLine("* " + l);

            sb.AppendLine();
            sb.AppendLine("Report ends here.");

            string reportPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "warp9-last-error.txt");
            File.WriteAllText(reportPath, sb.ToString());

            MessageBox.Show($"Error report has been saved to: {reportPath}.");
        }

        private static IEnumerable<string> ListFiles(string root)
        {
            string[] files = Directory.GetFiles(root, "*", SearchOption.AllDirectories);
            foreach (string file in files)
            {
                string relPath = Path.GetRelativePath(root, file);
                long length = new FileInfo(file).Length;

                yield return $"{relPath} : {length} Bytes";
            }
        }
    }
}
