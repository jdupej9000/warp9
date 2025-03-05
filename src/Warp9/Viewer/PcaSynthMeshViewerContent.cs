using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using Warp9.Model;

namespace Warp9.Viewer
{
    public class PcaSynthMeshViewerContent : ColormapMeshViewerContentBase
    {
        public PcaSynthMeshViewerContent(Project proj, long dcaEntityKey, string name) :
            base(name)
        {
            project = proj;
            entityKey = dcaEntityKey;

            if (!proj.Entries.TryGetValue(entityKey, out ProjectEntry? entry) ||
                entry is null ||
                entry.Kind != ProjectEntryKind.MeshPca)
            {
                throw new InvalidOperationException();
            }

            pcaEntry = entry;

            sidebar = new PcaSynthMeshSideBar();
        }

        Project project;
        ProjectEntry pcaEntry;
        long entityKey;
        PcaSynthMeshSideBar sidebar;

        public override Page? GetSidebar()
        {
            return sidebar;
        }
    }
}
