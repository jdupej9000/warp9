using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using Warp9.Controls;
using Warp9.Model;

namespace Warp9.Viewer
{
    public class DcaViewerContent : IViewerContent
    {
        public DcaViewerContent(Project proj, long dcaEntityKey)
        {
            project = proj;
            entityKey = dcaEntityKey;

            if (!proj.Entries.TryGetValue(entityKey, out ProjectEntry? entry) ||
                entry.Kind != ProjectEntryKind.MeshCorrespondence)
                throw new InvalidOperationException();

            dcaEntry = entry;
        }

        Project project;
        ProjectEntry dcaEntry;
        long entityKey;

        RenderItemMesh meshRend = new RenderItemMesh();

        public void AttachRenderer(WpfInteropRenderer renderer)
        {
            renderer.ClearRenderItems();
            renderer.AddRenderItem(meshRend);
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
