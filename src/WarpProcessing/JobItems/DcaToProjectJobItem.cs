using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Warp9.Data;
using Warp9.Jobs;
using Warp9.Model;
using Warp9.Processing;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace Warp9.JobItems
{
    public class DcaToProjectJobItem : ProjectJobItem
    {
        public DcaToProjectJobItem(int index, long specTableKey, string? gpaItem, string corrPclsItem, string? corrLmsItem, string corrSizeItem, string corrRejection, string corrWhitelist, string resultEntryName, DcaConfiguration cfg) :
            base(index, "Updating project", JobItemFlags.FailuesAreFatal | JobItemFlags.RunsAlone)
        {
            SpecimenTableKey = specTableKey;
            CorrespondencePclItem = corrPclsItem;
            CorrespondenceLandmarksItem = corrLmsItem;
            ResultEntryName = resultEntryName;
            CorrespondenceSizeItem = corrSizeItem;
            GpaItem = gpaItem;
            DcaConfig = cfg;
            RejectionItem = corrRejection;
            VertexWhitelistItem = corrWhitelist;
        }

        public long SpecimenTableKey { get; init; }
        public string CorrespondencePclItem { get; init; }
        public string CorrespondenceSizeItem { get; init; }
        public string? CorrespondenceLandmarksItem { get; init; }
        public string ResultEntryName { get; init; }
        public string? GpaItem { get; init; }
        public string RejectionItem { get; init; }
        public string VertexWhitelistItem { get; init; }
        public DcaConfiguration DcaConfig { get; init; }

        protected override bool RunInternal(IJob job, ProjectJobContext ctx)
        {
            Project proj = ctx.Project;
            SpecimenTable specTab = new SpecimenTable();

            PointCloud pclBase;

            int n = 0;
            if (ctx.Workspace.TryGet(CorrespondencePclItem, out List<PointCloud>? corrPcls) && corrPcls is not null)
            {
                n = corrPcls.Count;
                SpecimenTableColumn<ProjectReferenceLink> colCorrPcl = specTab.AddColumn<ProjectReferenceLink>("corrPcl", SpecimenTableColumnType.PointCloud);
                for (int i = 0; i < n; i++)
                {
                    long referenceKey = proj.AddReferenceDirect(ProjectReferenceFormat.W9Pcl, corrPcls[i]);
                    colCorrPcl.Add(new ProjectReferenceLink(referenceKey));
                }

                pclBase = corrPcls[DcaConfig.BaseMeshIndex];
            }
            else if (ctx.Workspace.TryGet(CorrespondencePclItem, out List<Mesh>? corrMeshes) && corrMeshes is not null)
            {
                n = corrMeshes.Count;
                SpecimenTableColumn<ProjectReferenceLink> colCorrPcl = specTab.AddColumn<ProjectReferenceLink>("corrPcl", SpecimenTableColumnType.PointCloud);
                for (int i = 0; i < n; i++)
                {
                    long referenceKey = proj.AddReferenceDirect(ProjectReferenceFormat.W9Pcl, corrMeshes[i].ToPointCloud());
                    colCorrPcl.Add(new ProjectReferenceLink(referenceKey));
                }

                pclBase = corrMeshes[DcaConfig.BaseMeshIndex];
            }
            else
            {
                throw new InvalidOperationException("Cannot find the correspondence pcls in the job workspace.");
            }

            if (CorrespondenceLandmarksItem is not null)
            {
                if (!ctx.Workspace.TryGet(CorrespondenceLandmarksItem, out List<PointCloud>? corrLms) || corrLms is null)
                    throw new InvalidOperationException("Cannot find the correspondence landmarks in the job workspace.");

                SpecimenTableColumn<ProjectReferenceLink> colCorrLms = specTab.AddColumn<ProjectReferenceLink>("corrLms", SpecimenTableColumnType.PointCloud);
                for (int i = 0; i < n; i++)
                {
                    long referenceKey = proj.AddReferenceDirect(ProjectReferenceFormat.W9Pcl, corrLms[i]);
                    colCorrLms.Add(new ProjectReferenceLink(referenceKey));
                }
            }

            if (ctx.Workspace.TryGet(CorrespondenceSizeItem, out List<float>? cs) && cs is not null)
            {
                SpecimenTableColumn<double> colCs = specTab.AddColumn<double>("cs", SpecimenTableColumnType.Real);
                for (int i = 0; i < n; i++)
                    colCs.Add((double)cs[i]);
            }

            long rejectionRatesKey = 0;
            if (ctx.Workspace.TryGet(RejectionItem, out DcaVertexRejection? rej) && rej is not null)
            {
                SpecimenTableColumn<double> colRej = specTab.AddColumn<double>("rejected", SpecimenTableColumnType.Real);
                for (int i = 0; i < n; i++)
                    colRej.Add((double)rej.MeshRejections[i] / (double)rej.NumVertices);

                Matrix<float> rejRates = new Matrix<float>(rej.ToVertexRejectionRates());
                rejectionRatesKey = proj.AddReferenceDirect(ProjectReferenceFormat.W9Matrix, rejRates);
            }

            if (!ctx.TryGetSpecTableMeshRegistered(SpecimenTableKey, DcaConfig.MeshColumnName!, DcaConfig.BaseMeshIndex, null, out Mesh? baseMesh) || baseMesh is null)
                return false;

            Mesh baseMeshCorr = Mesh.FromPointCloud(pclBase, baseMesh);
            long baseMeshCorrRef = proj.AddReferenceDirect(ProjectReferenceFormat.W9Mesh, baseMeshCorr);

            long meanLandmarksKey = default;

            if (GpaItem is not null &&
                ctx.Workspace.TryGet(GpaItem, out Gpa? gpa) &&
                gpa is not null)
            {
                meanLandmarksKey = proj.AddReferenceDirect(ProjectReferenceFormat.W9Pcl, gpa.Mean);
            }

            ProjectEntry entry = proj.AddNewEntry(ProjectEntryKind.MeshCorrespondence);
            entry.Name = ResultEntryName;
            entry.Deps.Add(SpecimenTableKey);
            entry.Payload.Table = specTab;
            entry.Payload.MeshCorrExtra = new MeshCorrespondenceExtraInfo() 
            { 
                DcaConfig = DcaConfig, 
                BaseMeshCorrKey = baseMeshCorrRef,
                MeanLandmarksKey = meanLandmarksKey,
                VertexRejectionRatesKey = rejectionRatesKey
            };

            ctx.WriteLog(ItemIndex, MessageKind.Information, 
                string.Format("The entry '{0}' has been added to the project.", ResultEntryName));

            return true;
        }
    }
}
