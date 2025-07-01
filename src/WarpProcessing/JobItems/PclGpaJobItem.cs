using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Warp9.Data;
using Warp9.Jobs;
using Warp9.Processing;

namespace Warp9.JobItems
{
    public class PclGpaJobItem : ProjectJobItem
    {
        public PclGpaJobItem(int index, string meshListWorkspaceItem, string sizeItem, string resultItem) :
            base(index, "Mesh GPA", Jobs.JobItemFlags.FailuesAreFatal | Jobs.JobItemFlags.RunsAlone)
        {
            ResultItem = resultItem;
            SizeItem = sizeItem;
            MeshListItem = meshListWorkspaceItem;
        }

        public string ResultItem { get; init; }
        public string SizeItem { get; init; }
        public string MeshListItem { get; init; }

        protected override bool RunInternal(IJob job, ProjectJobContext ctx)
        {
            PointCloud[] pointClouds;
            List<Mesh>? meshes = null;

            if (ctx.Workspace.TryGet(MeshListItem, out List<PointCloud>? pcls) && pcls is not null)
            {
                pointClouds = pcls.ToArray();
            }
            else if (ctx.Workspace.TryGet(MeshListItem, out meshes) && meshes is not null)
            {
                pointClouds = meshes.ConvertAll((t) => (PointCloud)t).ToArray();
            }
            else
            {
                throw new InvalidOperationException(MeshListItem + " is not a list of point clouds nor a list of meshes or does not exist at all.");
            }

            int n = pointClouds.Length;
            ctx.WriteLog(ItemIndex, MessageKind.Information,
                    string.Format("Running GPA on {0} meshes with {1} vertices.", n, pointClouds[0].VertexCount));

            Gpa res = Gpa.Fit(pointClouds);
            ctx.WriteLog(ItemIndex, MessageKind.Information,
                    string.Format("Mesh GPA complete ({0}).", res.ToString()));

            if (ctx.Workspace.TryGet(SizeItem, out List<float>? corrCs) && corrCs is not null)
            {
                for (int i = 0; i < n; i++)
                    corrCs[i] *= res.GetTransform(i).cs;
            }
            else
            {
                for (int i = 0; i < n; i++)
                    ctx.Workspace.Set(SizeItem, i, res.GetTransform(i).cs);
            }

            if (meshes is null)
            {
                for (int i = 0; i < n; i++)
                    ctx.Workspace.Set(ResultItem, i, pointClouds[i]);
            }
            else
            {
                for (int i = 0; i < n; i++)
                    ctx.Workspace.Set(ResultItem, i, Mesh.FromPointCloud(pointClouds[i], meshes[i]));
            }

            return true;
        }
    }
}
