using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Warp9.Model;
using Warp9.Viewer;

namespace Warp9.Scene
{
    public class GridSceneElement : ISceneElement
    {
        public bool Visible { get; set; } = true;
        public RenderItemVersion Version { get; } = new RenderItemVersion();

        public void ConfigureRenderItem(RenderItemDelta delta, Project proj, RenderItemBase rib)
        {
            if (rib is not RenderItemGrid ri)
                return;

            ri.Visible = Visible;
        }
    }
}
