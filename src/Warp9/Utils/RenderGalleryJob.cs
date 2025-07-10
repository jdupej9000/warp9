using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Warp9.JobItems;
using Warp9.Jobs;
using Warp9.Model;
using Warp9.Viewer;

namespace Warp9.Utils
{
    public static class RenderGalleryJob
    {
        public static IEnumerable<ProjectJobItem> Create(IReadOnlyList<SnapshotInfo> snapshots, GalleryRenderSettings settings)
        {
            yield return new RenderGalleryJobItem(0, snapshots, settings);
        }
    }
}
