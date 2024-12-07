using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using Warp9.Controls;
using Warp9.Data;
using Warp9.Model;

namespace Warp9.Viewer
{
    public class DcaViewerContent : IViewerContent
    {
        public DcaViewerContent(Project proj, long dcaEntityKey)
        {
            project = proj;
            entityKey = dcaEntityKey;

            if (!proj.Entries.TryGetValue(entityKey, out ProjectEntry? entry) ||
                entry.Kind != ProjectEntryKind.MeshCorrespondence)
                throw new InvalidOperationException();

            dcaEntry = entry;
        }

        Project project;
        ProjectEntry dcaEntry;
        long entityKey;

        RenderItemMesh meshRend = new RenderItemMesh();

        public void AttachRenderer(WpfInteropRenderer renderer)
        {
            renderer.AddRenderItem(meshRend);

            SpecimenTable tab = dcaEntry.Payload.Table!;
            long corrPclRef = tab.Columns["corrPcl"].GetData<ProjectReferenceLink>()[0].ReferenceIndex;

            int baseIndex = dcaEntry.Payload.MeshCorrExtra.DcaConfig.BaseMeshIndex;
            SpecimenTable mainSpecTable = project.Entries[dcaEntry.Payload.MeshCorrExtra.DcaConfig.SpecimenTableKey].Payload.Table;
            long baseMeshRef = mainSpecTable.Columns[dcaEntry.Payload.MeshCorrExtra.DcaConfig.MeshColumnName].GetData<ProjectReferenceLink>()[baseIndex].ReferenceIndex;

            if (!project.TryGetReference(corrPclRef, out PointCloud corrPcl))
                throw new InvalidOperationException();

            if (!project.TryGetReference(baseMeshRef, out Mesh baseMesh))
                throw new InvalidOperationException();

            Mesh corrMesh = Mesh.FromPointCloud(corrPcl, baseMesh);

            meshRend.Mesh = corrMesh;
            meshRend.Style = MeshRenderStyle.EstimateNormals | MeshRenderStyle.PhongBlinn | MeshRenderStyle.ColorFlat;
            meshRend.Color = Color.Gray;
        }

        public Page? GetSidebar()
        {
            return null;
        }

        public void ViewportResized(Size size)
        {
            
        }
    }
}
