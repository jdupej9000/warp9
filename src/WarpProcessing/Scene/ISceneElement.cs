using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Warp9.Model;
using Warp9.Viewer;

namespace Warp9.Scene
{
    public interface ISceneElement
    {
        public RenderItemVersion Version { get; }
        public void ConfigureRenderItem(RenderItemDelta delta, Project proj, RenderItemBase rib);
        public void PersistData(Project project);
    }
}
