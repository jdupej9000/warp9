using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Warp9.Data;
using Warp9.Jobs;
using Warp9.Model;
using Warp9.Processing;
using Warp9.Utils;

namespace Warp9.JobItems
{
    public class DiffMatrixJobItem : ProjectJobItem
    {
        public DiffMatrixJobItem(int index, string resultEntryName, DiffMatrixConfiguration cfg) :
            base(index, "Distance Matrix", JobItemFlags.RunsAlone | JobItemFlags.FailuesAreFatal)
        {
            ResultEntryName = resultEntryName;
            Config = cfg;
        }

        public string ResultEntryName { get; init; }
        public DiffMatrixConfiguration Config { get; init; }

        protected override bool RunInternal(IJob job, ProjectJobContext ctx)
        {
            Project proj = ctx.Project;
            if (!proj.Entries.TryGetValue(Config.ParentEntityKey, out ProjectEntry? dcaEntry) ||
                dcaEntry is null ||
                dcaEntry.Kind != ProjectEntryKind.MeshCorrespondence ||
                dcaEntry.Payload.MeshCorrExtra is null ||
                dcaEntry.Payload.Table is null)
            {
                return false;
            }

            try
            {
                List<PointCloud> dcaCorrPcls = ModelUtils.LoadModelsAsPclsWithSize(ctx.Project, Config.ParentEntityKey, Config.ParentColumnName,
                    Config.RestoreSize ? Config.ParentSizeColumn : null);

                int nv = dcaCorrPcls[0]!.VertexCount;
                int ns = dcaCorrPcls.Count;

                if(Config.RejectionMode != PcaRejectionMode.None)
                    throw new ModelException("Vertex rejection is not supported yet.");

                bool[] allow = ModelUtils.MakeAllowList(proj, nv, Config.RejectionMode != PcaRejectionMode.None,
                    Config.RejectionMode == PcaRejectionMode.CustomThreshold ?
                        Config.RejectionThreshold : 0.01f * dcaEntry.Payload.MeshCorrExtra!.DcaConfig.RejectCountPercent,
                    dcaEntry.Payload.MeshCorrExtra.VertexRejectionRatesKey);

                MatrixCollection mc = MakeDiffMatrix(dcaCorrPcls, allow);
                StoreToProject(proj, mc);
            }
            catch (ModelException e)
            {
                ctx.WriteLog(ItemIndex, MessageKind.Error, e.Message);
                return false;
            }

            return true;
        }

        private MatrixCollection MakeDiffMatrix(List<PointCloud> pcls, bool[] allow)
        {
            MatrixCollection mc = MeshDistance.Compute(pcls, null,
                Config.Methods.Select((t) => (MeshDistanceKind)t).ToArray());
            return mc;
        }

        private void StoreToProject(Project proj, MatrixCollection mc)
        {
            long mcKey = proj.AddReferenceDirect(ProjectReferenceFormat.W9Matrix, mc);

            ProjectEntry entry = proj.AddNewEntry(ProjectEntryKind.DiffMatrix);
            entry.Name = ResultEntryName;
            entry.Deps.Add(Config.ParentEntityKey);
            entry.Payload.DiffMatrixExtra = new DiffMatrixExtraInfo()
            {
                Config = Config,
                DataKey = mcKey
            };
        }

    }
}
