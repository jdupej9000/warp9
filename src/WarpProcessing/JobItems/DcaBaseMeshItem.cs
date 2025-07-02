using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Warp9.Data;
using Warp9.Jobs;
using Warp9.Model;
using Warp9.Native;
using Warp9.Processing;

namespace Warp9.JobItems
{
    public class DcaBaseMeshItem : ProjectJobItem
    {
        public DcaBaseMeshItem(int index, long specTableKey, string meshColumn, int baseMeshIndex, string? gpaItem, string baseMeshKey, bool optimizeMesh) :
            base(index, "Base mesh generation", JobItemFlags.FailuesAreFatal | JobItemFlags.RunsAlone)
        {
            SpecimenTableKey = specTableKey;
            MeshColumn = meshColumn;
            BaseMeshIndex = baseMeshIndex;
            OptimizeMesh = optimizeMesh;
            BaseMeshKey = baseMeshKey;
            GpaItem = gpaItem; 
            LogItem = null;
        }

        public long SpecimenTableKey { get; init; }
        public string MeshColumn { get; init; }
        public int BaseMeshIndex { get; init; }
        public string? GpaItem { get; init; }
        public string BaseMeshKey { get; init; }
        public string? LogItem { get; init; }
        public bool OptimizeMesh { get; init; }

        protected override bool RunInternal(IJob job, ProjectJobContext ctx)
        {
            SpecimenTableColumn<ProjectReferenceLink>? column = ModelUtils.TryGetSpecimenTableColumn<ProjectReferenceLink>(
               ctx.Project, SpecimenTableKey, MeshColumn);
            if (column is null)
            {
                ctx.WriteLog(ItemIndex, MessageKind.Error,
                    string.Format("Cannot find column '{0}' in entity '{1}'.", MeshColumn, SpecimenTableKey));
                return false;
            }

            if (!ctx.TryGetSpecTableMeshRegistered(SpecimenTableKey, MeshColumn, BaseMeshIndex, GpaItem, out Mesh? baseMesh) || baseMesh is null)
                return false;

            if (OptimizeMesh)
                baseMesh = MeshFairing.Optimize(baseMesh, 1).ToMesh();

            ctx.Workspace.Set(BaseMeshKey, baseMesh);

            return true;
        }
    }
}
