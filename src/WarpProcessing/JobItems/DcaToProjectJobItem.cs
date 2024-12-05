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
        public DcaToProjectJobItem(int index, long specTableKey, string corrPclsItem, string? corrLmsItem, string resultEntryName, DcaConfiguration cfg) :
            base(index, "Updating project", JobItemFlags.FailuesAreFatal | JobItemFlags.RunsAlone)
        {
            SpecimenTableKey = specTableKey;
            CorrespondencePclItem = corrPclsItem;
            CorrespondenceLandmarksItem = corrLmsItem;
            ResultEntryName = resultEntryName;
            DcaConfig = cfg;
        }

        public long SpecimenTableKey { get; init; }
        public string CorrespondencePclItem { get; init; }
        public string? CorrespondenceLandmarksItem { get; init; }
        public string ResultEntryName { get; init; }
        public DcaConfiguration DcaConfig { get; init; }

        protected override bool RunInternal(IJob job, ProjectJobContext ctx)
        {
            Project proj = ctx.Project;
            SpecimenTable specTab = new SpecimenTable();

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

            // TODO: additional data

            ProjectEntry entry = proj.AddNewEntry(ProjectEntryKind.MeshCorrespondence);
            entry.Name = ResultEntryName;
            entry.Deps.Add(SpecimenTableKey);
            entry.Payload.Table = specTab;
            entry.Payload.MeshCorrExtra = new MeshCorrespondenceExtraInfo() { DcaConfig = DcaConfig };

            ctx.WriteLog(ItemIndex, MessageKind.Information, 
                string.Format("The entry '{0}' has been added to the project.", ResultEntryName));

            return true;
        }
    }
}
