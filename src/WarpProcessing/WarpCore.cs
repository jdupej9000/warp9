using System.Runtime.InteropServices;
using System.Text;

namespace Warp9
{
    public static class WarpCore
    {
        public const int WCINFO_VERSION = 0;
        public const int WCINFO_MKL_VERSION = 1;
        public const int WCINFO_MKL_ISA = 2;


        [DllImport("WarpCore")]
        public static extern int wcore_get_info(int index, StringBuilder buffer, int bufferSize);
    }
}
