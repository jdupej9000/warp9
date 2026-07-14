using System;
using System.Collections.Generic;
using System.Text;

/* RenderItems are added to Renderers to perform rendering of objects. Renderer
 * calls ProjectToTask before rendering, so that a RenderTask can be created or
 * updated. RenderTask holds the buffers, shaders, textures and performs the 
 * actual draw calls with those objects. When the configuration in a RenderItem
 * changes, it will be reflected in the RenderTask on next rendering. The updates
 * are versioned to prevent superfluous buffer updates etc. RenderItem can be
 * transferred between Renderers or owned by multiple Renderers simultaneously.
 * Renderers maintain their own sets of RenderTasks as needed.
*/

namespace Warp9.Viewer
{
    public class RenderItemBase
    {
        public long Version { get; private set; } = 0;        

        public void Commit()
        {
            Version++;
        }

        public bool ProjectToTask(RenderTask task)
        {
            bool mustUpdate = task.TryUpdate(Version);
            if (mustUpdate) UpdateTask(task);

            return mustUpdate;
        }

        protected virtual void UpdateTask(RenderTask task)
        {
        }
}
}
