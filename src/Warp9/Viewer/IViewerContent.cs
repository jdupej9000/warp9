using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using Warp9.Controls;

namespace Warp9.Viewer
{
    public interface IViewerContent
    {
        public void AttachRenderer(WpfInteropRenderer renderer);
        public Page? GetSidebar();
        public void ViewportResized(Size size);
    }
}
