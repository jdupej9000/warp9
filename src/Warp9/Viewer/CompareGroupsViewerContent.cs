using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using Warp9.Controls;

namespace Warp9.Viewer
{
    public class CompareGroupsViewerContent : IViewerContent
    {
        public string Name => "Compare groups";

        RenderItemMesh meshRend = new RenderItemMesh();
        RenderItemGrid gridRend = new RenderItemGrid();

        public event PropertyChangedEventHandler? PropertyChanged;
        public event EventHandler ViewUpdated;

        public void AttachRenderer(WpfInteropRenderer renderer)
        {
            throw new NotImplementedException();
        }

        public Page? GetSidebar()
        {
            return null;
        }

        public void ViewportResized(Size size)
        {
        }
    }
}
