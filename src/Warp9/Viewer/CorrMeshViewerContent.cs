using System;
using System.Collections.Generic;
using System.ComponentModel;
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
    public class CorrMeshViewerContent : IViewerContent, INotifyPropertyChanged
    {
        public CorrMeshViewerContent(Project proj, long dcaEntityKey, string name)
        {
            project = proj;
            entityKey = dcaEntityKey;

            if (!proj.Entries.TryGetValue(entityKey, out ProjectEntry? entry) ||
                entry.Kind != ProjectEntryKind.MeshCorrespondence)
                throw new InvalidOperationException();

            dcaEntry = entry;
            Name = name;

            sidebar = new CorrMeshSideBar(this);
        }

        Project project;
        ProjectEntry dcaEntry;
        Page sidebar;
        long entityKey;

        RenderItemMesh meshRend = new RenderItemMesh();

        int meshIndex = 0;

        public event PropertyChangedEventHandler? PropertyChanged;
        public event EventHandler ViewUpdated;

        public string Name { get; private init; }

        public int MeshIndex
        {
            get { return meshIndex; }
            set { ShowMesh(value); OnPropertyChanged("MeshIndex"); }
        }

        public void AttachRenderer(WpfInteropRenderer renderer)
        {
            renderer.AddRenderItem(meshRend);
            meshRend.Style = MeshRenderStyle.EstimateNormals | MeshRenderStyle.PhongBlinn | MeshRenderStyle.ColorFlat;
            meshRend.Color = Color.Gray;

            ShowMesh(0);
        }

        public Page? GetSidebar()
        {
            return sidebar;
        }

        public void ViewportResized(Size size)
        {
            
        }

        private void ShowMesh(int index)
        {
            SpecimenTable tab = dcaEntry.Payload.Table!;
            long corrPclRef = tab.Columns["corrPcl"].GetData<ProjectReferenceLink>()[index].ReferenceIndex;

            int baseIndex = dcaEntry.Payload.MeshCorrExtra.DcaConfig.BaseMeshIndex;
            SpecimenTable mainSpecTable = project.Entries[dcaEntry.Payload.MeshCorrExtra.DcaConfig.SpecimenTableKey].Payload.Table;
            long baseMeshRef = mainSpecTable.Columns[dcaEntry.Payload.MeshCorrExtra.DcaConfig.MeshColumnName].GetData<ProjectReferenceLink>()[baseIndex].ReferenceIndex;

            if (!project.TryGetReference(corrPclRef, out PointCloud corrPcl))
                throw new InvalidOperationException();

            if (!project.TryGetReference(baseMeshRef, out Mesh baseMesh))
                throw new InvalidOperationException();

            Mesh corrMesh = Mesh.FromPointCloud(corrPcl, baseMesh);

            meshRend.Mesh = corrMesh;
            meshIndex = index;
        }

        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
            ViewUpdated?.Invoke(this, EventArgs.Empty);
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
