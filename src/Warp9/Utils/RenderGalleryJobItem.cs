using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Warp9.Forms;
using Warp9.JobItems;
using Warp9.Jobs;
using Warp9.Model;
using Warp9.Navigation;

namespace Warp9.Utils
{
    public class RenderGalleryJobItem : ProjectJobItem
    {
        public RenderGalleryJobItem(int index, IReadOnlyList<SnapshotInfo> items, GalleryRenderSettings settings) :
            base(index, "Render gallery items", Jobs.JobItemFlags.RunsAlone | Jobs.JobItemFlags.FailuesAreFatal)
        {
            Items = items;
            Settings = settings;
        }

        public IReadOnlyList<SnapshotInfo> Items { get; init; }
        public GalleryRenderSettings Settings { get; init; }

        protected override bool RunInternal(IJob job, ProjectJobContext ctx)
        {
            try
            {
                SnapshotRenderer.RenderSnaphots(ctx.Project, Items, Settings);
            }
            catch (Exception ex)
            {
                ctx.WriteLog(ItemIndex, MessageKind.Error, "Failed to render one or more of the selected snapshots: " + ex.Message);
                return false;
            }

            ctx.WriteLog(ItemIndex, MessageKind.Information, $"Done rendering {Items.Count} snapshots to '{Settings.Directory}'.");

            return true;
        }
    }
}
