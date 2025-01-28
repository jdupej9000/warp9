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
    public class DcaPcaJobItem : ProjectJobItem
    {
        public DcaPcaJobItem(int index, string resultEntryName, PcaConfiguration cfg) :
            base(index, "PCA", JobItemFlags.FailuesAreFatal | JobItemFlags.RunsAlone)
        {
            ResultEntryName = resultEntryName;
            Config = cfg;
        }

        public string ResultEntryName { get; init; }
        public PcaConfiguration Config { get; init; }

        protected override bool RunInternal(IJob job, ProjectJobContext ctx)
        {
            Project proj = ctx.Project;
            if (!proj.Entries.TryGetValue(Config.ParentDcaKey, out ProjectEntry? dcaEntry) ||
                dcaEntry is null ||
                dcaEntry.Kind != ProjectEntryKind.MeshCorrespondence ||
                dcaEntry.Payload.MeshCorrExtra is null ||
                dcaEntry.Payload.Table is null)
            {
                return false;
            }

            SpecimenTableColumn<ProjectReferenceLink>? corrColumn = ModelUtils.TryGetSpecimenTableColumn<ProjectReferenceLink>(
                ctx.Project, Config.ParentDcaKey, ModelConstants.CorrespondencePclColumnName);
            if (corrColumn is null)
                return false;

            List<PointCloud?> dcaCorrPcls = ModelUtils.LoadSpecimenTableRefs<PointCloud>(proj, corrColumn).ToList();
            if (dcaCorrPcls.Any((t) => t is null))
                return false;

            if (Config.RestoreSize)
            {
                SpecimenTableColumn<double>? csColumn = ModelUtils.TryGetSpecimenTableColumn<double>(
                    ctx.Project, Config.ParentDcaKey, ModelConstants.CentroidSizeColumnName);
                // TODO
            }

            int nv = dcaCorrPcls[0]!.VertexCount;

            bool[] allow = new bool[nv];
            if (Config.RejectionMode == PcaRejectionMode.None)
            {
                for (int i = 0; i < nv; i++)
                    allow[i] = true;
            }
            else
            {
                float thresh = Config.RejectionMode == PcaRejectionMode.CustomThreshold ?
                    Config.RejectionThreshold :
                    dcaEntry.Payload.MeshCorrExtra!.DcaConfig.RejectCountPercent;

                if (!proj.TryGetReference(dcaEntry.Payload.MeshCorrExtra.VertexRejectionRatesKey, out MatrixCollection? rejmc) ||
                    rejmc is null ||
                    !rejmc.TryGetMatrix(ModelConstants.VertexRejectionRatesKey, out Matrix<float>? rejectRates) ||
                    rejectRates is null)
                {
                    ctx.WriteLog(ItemIndex, MessageKind.Error,
                        "Vertex rejection rates are required but not present in the DCA entry.");
                    return false;
                }

                MiscUtils.ThresholdBelow(rejectRates.Data.AsSpan(), thresh, allow.AsSpan());
            }

            ctx.WriteLog(ItemIndex, MessageKind.Information, "Fitting PCA.");
            Pca? pca = Pca.Fit(dcaCorrPcls!, allow, Config.NormalizeScale);
            if (pca is null)
            {
                ctx.WriteLog(ItemIndex, MessageKind.Error, "Failed to fit PCA.");
                return false;
            }

            MatrixCollection mcPca = pca.ToMatrixCollection();
            long pcaDataKey = proj.AddReferenceDirect(ProjectReferenceFormat.W9Matrix, mcPca);

            ProjectEntry entry = proj.AddNewEntry(ProjectEntryKind.MeshPca);
            entry.Name = ResultEntryName;
            entry.Deps.Add(Config.ParentDcaKey);
            entry.Payload.PcaExtra = new PcaExtraInfo()
            {
                Info = Config,
                DataKey = pcaDataKey
            };

            ctx.WriteLog(ItemIndex, MessageKind.Information,
               string.Format("The entry '{0}' has been added to the project.", ResultEntryName));

            return true;
        }

     
    }
}
