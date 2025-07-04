using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Warp9.Data;
using Warp9.Jobs;
using Warp9.Native;
using Warp9.Processing;

namespace Warp9.JobItems
{
    public class DcaRejectionJobItem : ProjectJobItem
    {
        public DcaRejectionJobItem(int index, long specTableKey, string meshCol, int baseMeshIndex, string dcaItem, DcaConfiguration cfg, string resultItem, string resultWhitelist) :
            base(index, "Correspondence rejection", Jobs.JobItemFlags.FailuesAreFatal | Jobs.JobItemFlags.RunsAlone)
        {
            SpecimenTableKey = specTableKey;
            BaseMeshIndex = baseMeshIndex;
            CorrMeshesItem = dcaItem;
            MeshColumn = meshCol;
            DcaConfig = cfg;
            ResultItem = resultItem;
            ResultWhitelist = resultWhitelist;
        }

        public int BaseMeshIndex { get; init; }
        public long SpecimenTableKey { get; init; }
        public string CorrMeshesItem { get; init; }
        public string MeshColumn { get; init; }
        public DcaConfiguration DcaConfig { get; init; }
        public string ResultItem { get; init; }
        public string ResultWhitelist { get; init; }

        protected override bool RunInternal(IJob job, ProjectJobContext ctx)
        {
            if (!ctx.TryGetSpecTableMeshRegistered(SpecimenTableKey, MeshColumn, BaseMeshIndex, null, out Mesh? baseMesh) ||
               baseMesh is null)
            {
                ctx.WriteLog(ItemIndex, MessageKind.Error, "Cannot load base mesh.");
                return false;
            }

            if (!ctx.Workspace.TryGet(CorrMeshesItem, out List<PointCloud>? corrPcls) || corrPcls is null)
            {
                ctx.WriteLog(ItemIndex, MessageKind.Error, "Cannot load current correspondence point clouds.");
                return false;
            }

            Mesh dcaBaseMesh = Mesh.FromPointCloud(corrPcls[BaseMeshIndex], baseMesh);

            DcaVertexRejection rejection = DcaVertexRejection.Create(dcaBaseMesh, corrPcls,
                DcaConfig.RejectExpandedLowThreshold, DcaConfig.RejectExpandedHighThreshold);
            ctx.Workspace.Set(ResultItem, rejection);

            bool[] vertexWhitelist = rejection.ToVertexWhitelist((int)MathF.Ceiling(DcaConfig.RejectCountPercent * corrPcls.Count));
            ctx.Workspace.Set(ResultWhitelist, vertexWhitelist);

            if (DcaConfig.RejectImputation == DcaImputationKind.Tps)
            {
                int numImpute = dcaBaseMesh.VertexCount - vertexWhitelist.Count();
                ctx.WriteLog(ItemIndex, MessageKind.Information, $"Imputing {numImpute} vertices in each surface using {DcaConfig.RejectImputation}.");

                for (int i = 0; i < corrPcls.Count; i++)
                {
                    PointCloud? imputed = MeshImputation.ImputePositions(dcaBaseMesh, corrPcls[i], vertexWhitelist, 30);
                    if (imputed is not null)
                        corrPcls[i] = imputed;
                }
            }

            return true;
        }
    }
}
