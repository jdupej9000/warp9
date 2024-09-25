using System.Runtime.InteropServices;
using System.Text;

namespace Warp9
{
    public enum WarpCoreInfoIndex : int
    {
        WCINFO_VERSION = 0,
        WCINFO_COMPILER = 1,
        WCINFO_OPT_PATH = 2,
        WCINFO_MKL_VERSION = 1000,
        WCINFO_MKL_ISA = 1001
    };

    public static class WarpCore
    {
        [DllImport("WarpCore.dll", CharSet = CharSet.Ansi)]
        public static extern int wcore_get_info(int index, StringBuilder buffer, int bufferSize);
    }
}
