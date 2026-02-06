using System;
using System.Buffers;
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

            try
            {
                List<PointCloud> dcaCorrPcls = ModelUtils.LoadModelsAsPclsWithSize(ctx.Project, Config.ParentEntityKey, Config.ParentColumnName,
                    Config.RestoreSize ? Config.ParentSizeColumn : null);

                int nv = dcaCorrPcls[0]!.VertexCount;
                int ns = dcaCorrPcls.Count;

                bool[] allow = ModelUtils.MakeAllowList(proj, nv, Config.RejectionMode != PcaRejectionMode.None,
                    Config.RejectionMode == PcaRejectionMode.CustomThreshold ?
                        Config.RejectionThreshold : 0.01f * dcaEntry.Payload.MeshCorrExtra!.DcaConfig.RejectCountPercent,
                    dcaEntry.Payload.MeshCorrExtra.VertexRejectionRatesKey);

                ctx.WriteLog(ItemIndex, MessageKind.Information, "Fitting PCA.");
                Pca pca = FitPca(dcaCorrPcls, allow);

                ctx.WriteLog(ItemIndex, MessageKind.Information, "Transforming source data into PCs.");
                MatrixCollection mc = TransformToScores(dcaCorrPcls, pca);

                StoreToProject(proj, mc, dcaEntry.Payload.MeshCorrExtra.BaseMeshCorrKey);
            }
            catch (ModelException e)
            {
                ctx.WriteLog(ItemIndex, MessageKind.Error, e.Message);
                return false;
            }

            return true;
        }

        Pca FitPca(List<PointCloud> dcaCorrPcls, bool[] allow)
        {
            Pca? pca = Pca.Fit(dcaCorrPcls!, allow, Config.NormalizeScale);
            if (pca is null)
                throw new ModelException("Failed to fit PCA.");

            return pca;
        }

        MatrixCollection TransformToScores(List<PointCloud> dcaCorrPcls, Pca pca)
        {
            int ns = dcaCorrPcls.Count;
            int npcs = Math.Min(50, ns - 1);

            int npcsall = pca.NumPcs;
            float[] scores = ArrayPool<float>.Shared.Rent(npcsall);
            Matrix<float> scoresMat = new Matrix<float>(npcs, ns);
            Parallel.For(0, ns, (i) =>
            {
                if (dcaCorrPcls[i] is null || !pca.TryGetScores(dcaCorrPcls[i]!, scores.AsSpan()))
                    throw new ModelException($"Cannot transform specimen {i}.");

                for (int j = 0; j < npcs; j++)
                    scoresMat[i, j] = scores[j];
            });

            ArrayPool<float>.Shared.Return(scores);

            MatrixCollection mcPca = pca.ToMatrixCollection();
            mcPca[Pca.KeyScores] = scoresMat;

            return mcPca;
        }

        void StoreToProject(Project proj, MatrixCollection mcPca, long baseMeshCorrKey)
        {
            long pcaDataKey = proj.AddReferenceDirect(ProjectReferenceFormat.W9Matrix, mcPca);

            ProjectEntry entry = proj.AddNewEntry(ProjectEntryKind.MeshPca);
            entry.Name = ResultEntryName;
            entry.Deps.Add(Config.ParentEntityKey);
            entry.Payload.PcaExtra = new PcaExtraInfo()
            {
                Info = Config,
                DataKey = pcaDataKey,
                TemplateKey = baseMeshCorrKey
            };
        }
    }
}
