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
        public void ConfigureRenderItem(Project proj, RenderItemBase rib);
        public void UpdateDynamicBuffers(Project proj, RenderItemBase rib);
    }
}
