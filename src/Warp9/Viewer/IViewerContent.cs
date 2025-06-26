using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using Warp9.Controls;
using Warp9.Data;
using Warp9.Scene;

namespace Warp9.Viewer
{
    public interface IViewerContent
    {
        public string Name { get; }
        public void AttachRenderer(WpfInteropRenderer renderer);
        public void DetachRenderer();
        public Page? GetSidebar();
        public void ViewportResized(Size size);
        public void ViewChanged(CameraInfo ci);
        public void MeshScaleHover(float? e);

        public ViewerScene Scene { get; }

        public event EventHandler ViewUpdated; // TODO: rename
    }
}
