using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Threading.Tasks;
using Warp9.Data;
using Warp9.Jobs;
using Warp9.Model;
using Warp9.Native;
using Warp9.Processing;
using Warp9.Utils;

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
            if (!proj.Entries.TryGetValue(Config.ParentEntityKey, out ProjectEntry? dcaEntry) ||
                dcaEntry is null ||
                dcaEntry.Kind != ProjectEntryKind.MeshCorrespondence ||
                dcaEntry.Payload.MeshCorrExtra is null ||
                dcaEntry.Payload.Table is null)
            {
                return false;
            }

            SpecimenTableColumn<ProjectReferenceLink>? corrColumn = ModelUtils.TryGetSpecimenTableColumn<ProjectReferenceLink>(
                ctx.Project, Config.ParentEntityKey, Config.ParentColumnName);
            if (corrColumn is null)
                return false;

            List<PointCloud?> dcaCorrPcls = ModelUtils.LoadSpecimenTableRefs<PointCloud>(proj, corrColumn).ToList();
            if (dcaCorrPcls.Any((t) => t is null))
                return false;

            int nv = dcaCorrPcls[0]!.VertexCount;
            int ns = dcaCorrPcls.Count;

            if (Config.RestoreSize)
            {
                if (Config.ParentSizeColumn is null)
                {
                    ctx.WriteLog(ItemIndex, MessageKind.Error,
                        "Restore size is enabled, but no size column is selected.");
                    return false;
                }

                SpecimenTableColumn<double>? csColumn = ModelUtils.TryGetSpecimenTableColumn<double>(
                    ctx.Project, Config.ParentEntityKey, Config.ParentSizeColumn);

                if (csColumn is not null)
                {
                    IReadOnlyList<double> cs = csColumn.GetData<double>();

                    for (int i = 0; i < ns; i++)
                        dcaCorrPcls[i] = MeshScaling.ScalePosition(dcaCorrPcls[i]!, (float)cs[i]).ToPointCloud();
                }
            }

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
                    0.01f * dcaEntry.Payload.MeshCorrExtra!.DcaConfig.RejectCountPercent;

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

            
            int npcs = 50;
            ctx.WriteLog(ItemIndex, MessageKind.Information, $"Transforming source data ({dcaCorrPcls.Count} datapoints), keeping {npcs} PCs.");
            
            int npcsall = pca.NumPcs;
            float[] scores = new float[npcsall];
            Matrix<float> scoresMat = new Matrix<float>(npcs, ns);
            for (int i = 0; i < ns; i++)
            {
                if (dcaCorrPcls[i] is null || 
                    !pca.TryGetScores(dcaCorrPcls[i]!, scores.AsSpan()))
                {
                    ctx.WriteLog(ItemIndex, MessageKind.Error, $"Cannot transform specimen {i}.");
                    return false;
                }

                for (int j = 0; j < npcs; j++)
                    scoresMat[i, j] = scores[j];
            }

            MatrixCollection mcPca = pca.ToMatrixCollection();
            mcPca[Pca.KeyScores] = scoresMat;
            long pcaDataKey = proj.AddReferenceDirect(ProjectReferenceFormat.W9Matrix, mcPca);

            ProjectEntry entry = proj.AddNewEntry(ProjectEntryKind.MeshPca);
            entry.Name = ResultEntryName;
            entry.Deps.Add(Config.ParentEntityKey);
            entry.Payload.PcaExtra = new PcaExtraInfo()
            {
                Info = Config,
                DataKey = pcaDataKey,
                TemplateKey = dcaEntry.Payload.MeshCorrExtra.BaseMeshCorrKey
            };

            ctx.WriteLog(ItemIndex, MessageKind.Information,
               string.Format("The entry '{0}' has been added to the project.", ResultEntryName));

            return true;
        }

     
    }
}
