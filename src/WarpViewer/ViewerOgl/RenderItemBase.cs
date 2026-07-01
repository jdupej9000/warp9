using System;
using System.Collections.Generic;
using System.Text;

namespace Warp9.ViewerOgl
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
