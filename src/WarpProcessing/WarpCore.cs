using System.Runtime.InteropServices;
using System.Text;

namespace Warp9
{
    public enum WarpCoreInfoIndex : int
    {
        WCINFO_VERSION = 0,
        WCINFO_MKL_VERSION = 1,
        WCINFO_MKL_ISA = 2
    };

    public static class WarpCore
    {
        [DllImport("WarpCore.dll", CharSet = CharSet.Ansi)]
        public static extern int wcore_get_info(int index, StringBuilder buffer, int bufferSize);
    }
}
