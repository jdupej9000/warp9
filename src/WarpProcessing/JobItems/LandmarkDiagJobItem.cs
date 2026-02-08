using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Security.Cryptography;
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

            PointCloud?[] pcls = ModelUtils.LoadSpecimenTableRefs<PointCloud>(ctx.Project, colLandmarks).ToArray();
            if (pcls.Any((t) => t is null))
            {
                ctx.WriteLog(ItemIndex, MessageKind.Error, "Cannot load landmarks in one or more specimens.");
                return false;
            }

            ctx.WriteLog(ItemIndex, MessageKind.Information, "HOVERING LANDMARKS");
            ctx.WriteLog(ItemIndex, MessageKind.Information, "Shows surface-to-landmark distances relative to model bounding box size.");

            const float HoverThresh = 0.003f;
            const float GpaOutlierThresh = 0.1f;

            int n = colLandmarks.NumRows;
            for (int i = 0; i < n; i++)
            {
                PointCloud? lms = pcls[i];
                Mesh? mesh = ModelUtils.LoadSpecimenTableRef<Mesh>(ctx.Project, colMeshes, i);

                if (lms is not null && mesh is not null)
                {
                    Aabb? aabb = MeshUtils.FindBoundingBox(mesh, MeshSegmentSemantic.Position);
                    float meshSize = aabb is not null ? aabb.Value.MaxSide : 1;
                    float[] hover = LandmarkUtils.CalculateLandmarkOffsets(lms, mesh);

                    if (hover.Any((t) => t > meshSize * HoverThresh))
                    {
                        StringBuilder sb = new StringBuilder();
                        sb.AppendFormat("{0,4}: ", i);

                        for (int j = 0; j < hover.Length; j++)
                        {
                            float rel = hover[j] / meshSize;
                            if (rel < HoverThresh)
                                sb.Append("     ");
                            else
                                sb.AppendFormat("{0:.000} ", rel);
                        }

                        ctx.WriteLog(ItemIndex, MessageKind.Information, sb.ToString());
                    }                  
                }
                else
                {
                    ctx.WriteLog(ItemIndex, MessageKind.Warning, "could not load");
                }
            }

            ctx.WriteLog(ItemIndex, MessageKind.Information, "");
            ctx.WriteLog(ItemIndex, MessageKind.Information, "GPA OUTLIERS");
            ctx.WriteLog(ItemIndex, MessageKind.Information, "Landmark distances to GPA mean, relative to GPA mean size, maxima over specimen. Large values may indicate incorrect landmark ordering.");
            GpaConfiguration gpaCfg = new GpaConfiguration();
            Gpa gpa = Gpa.Fit(pcls!, null, gpaCfg);
            Aabb lmBox = MeshUtils.FindBoundingBox(gpa.Mean, MeshSegmentSemantic.Position).Value;
            for (int i = 0; i < n; i++)
            {
                float drel = LandmarkUtils.MaxHomoLandmarkDistance(gpa.Mean, gpa.GetTransformed(i)) / lmBox.MaxSide;
                if (drel > GpaOutlierThresh)
                {
                    ctx.WriteLog(ItemIndex, MessageKind.Information,
                        string.Format("{0,4}: {1:F3}", i, drel));
                }
            }


            ctx.WriteLog(ItemIndex, MessageKind.Information, "");
            ctx.WriteLog(ItemIndex, MessageKind.Information, "LANDMARK DISPERSIONS");
            ctx.WriteLog(ItemIndex, MessageKind.Information, "Large values indicate noisy, poorly repeatable landmarks.");
            float[] dispPost = LandmarkUtils.CalculateDispersion(gpa.Mean, gpa.EnumerateTransformed());

            ctx.WriteLog(ItemIndex, MessageKind.Information, 
                string.Join(", ", dispPost.Select((t) => t.ToString("F3"))));

            return true;
        }
    }
}
