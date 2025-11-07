using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Warp9.Data;
using Warp9.Jobs;
using Warp9.Model;
using Warp9.Processing;

namespace Warp9.JobItems
{
    public class LandmarkDiagJobItem : ProjectJobItem
    {
        public LandmarkDiagJobItem(int index, long specTableKey, string landmarkColumn, string meshColumn) :
            base(index, "Landmark diagnostics", JobItemFlags.FailuesAreFatal | JobItemFlags.RunsAlone)
        {
            SpecimenTableKey = specTableKey;
            LandmarkColumn = landmarkColumn;
            MeshColumn = meshColumn;
        }

        public long SpecimenTableKey { get; init; }
        public string LandmarkColumn { get; init; }
        public string MeshColumn {get; init;}

        protected override bool RunInternal(IJob job, ProjectJobContext ctx)
        {
            SpecimenTableColumn<ProjectReferenceLink>? colLandmarks = ModelUtils.TryGetSpecimenTableColumn<ProjectReferenceLink>(
                  ctx.Project, SpecimenTableKey, LandmarkColumn);

            SpecimenTableColumn<ProjectReferenceLink>? colMeshes = ModelUtils.TryGetSpecimenTableColumn<ProjectReferenceLink>(
                ctx.Project, SpecimenTableKey, MeshColumn);

            if (colLandmarks is null || colMeshes is null)
            {
                ctx.WriteLog(ItemIndex, MessageKind.Error,
                    "Cannot find mesh or landmark column.");
                return false;
            }

            int n = colLandmarks.NumRows;
            for (int i = 0; i < n; i++)
            {
                PointCloud? lms = ModelUtils.LoadSpecimenTableRef<PointCloud>(ctx.Project, colLandmarks, i);
                Mesh? mesh = ModelUtils.LoadSpecimenTableRef<Mesh>(ctx.Project, colMeshes, i);

                if (lms is not null && mesh is not null)
                {
                    Aabb? aabb = MeshUtils.FindBoundingBox(mesh, MeshSegmentSemantic.Position);
                    float meshSize = aabb is not null ? aabb.Value.MaxSide : 1;

                    float[] hover = LandmarkUtils.CalculateLandmarkOffsets(lms, mesh);

                    StringBuilder sb = new StringBuilder();
                    sb.AppendFormat("{0,4}: ");

                    for (int j = 0; j < hover.Length; j++)
                    {
                        float rel = hover[j] / meshSize;
                        if (rel < 0.001)
                            sb.Append("     ");
                        else
                            sb.AppendFormat("{0:.000} ", rel);
                    }

                    ctx.WriteLog(ItemIndex, MessageKind.Information, sb.ToString());
                }
                else
                {
                    ctx.WriteLog(ItemIndex, MessageKind.Warning, "could not load");
                }
            }

            return true;
        }
    }
}
