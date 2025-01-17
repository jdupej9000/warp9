using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Xml.Linq;
using Warp9.Controls;
using Warp9.Data;
using Warp9.Model;
using Warp9.Processing;

namespace Warp9.Viewer
{
    public class DcaDiagnosticsViewerContent : IViewerContent, INotifyPropertyChanged
    {
        public DcaDiagnosticsViewerContent(Project proj, long dcaEntityKey, string name)
        {
            project = proj;
            entityKey = dcaEntityKey;

            if (!proj.Entries.TryGetValue(entityKey, out ProjectEntry? entry) ||
                entry.Kind != ProjectEntryKind.MeshCorrespondence)
                throw new InvalidOperationException();

            dcaEntry = entry;
            Name = name;

            sidebar = new DcaDiagnosticsSideBar(this);
        }

        Project project;
        ProjectEntry dcaEntry;
        Page sidebar;
        long entityKey;
        Lut lut = Lut.Create(256, Lut.PlasmaColors);

        RenderItemMesh meshRend = new RenderItemMesh();
        RenderItemGrid gridRend = new RenderItemGrid();
        bool  renderGrid = true;

        public event EventHandler ViewUpdated;
        public event PropertyChangedEventHandler? PropertyChanged;

        public string Name { get; private init; }

        public bool RenderGrid
        {
            get { return renderGrid; }
            set { renderGrid = value; UpdateRendererConfig(); OnPropertyChanged("RenderGrid"); }
        }

        public void AttachRenderer(WpfInteropRenderer renderer)
        {
            meshRend.Lut = lut;
            renderer.AddRenderItem(meshRend);
            renderer.AddRenderItem(gridRend);
            ShowMesh();
            ShowRejectionMap();
        }

        public Page? GetSidebar()
        {
            return sidebar;
        }

        public void ViewportResized(Size size)
        {
        }

        private void UpdateRendererConfig()
        {
            meshRend.Style = MeshRenderStyle.DiffuseLighting | MeshRenderStyle.ColorLut | MeshRenderStyle.EstimateNormals;
            meshRend.RenderWireframe = false;
            meshRend.RenderFace = true;
            meshRend.RenderPoints = false;
            meshRend.RenderCull = false;
            meshRend.FillColor = Color.LightGray;
            meshRend.PointWireColor = Color.Black;
            meshRend.ValueMin = 0;
            meshRend.ValueMax = 1;

            gridRend.Visible = renderGrid;
        }

        private void ShowMesh()
        {
            if (!dcaEntry.Payload.Table!.Columns.TryGetValue("corrPcl", out SpecimenTableColumn? col) ||
              col is not SpecimenTableColumn<ProjectReferenceLink> pclCol)
                throw new InvalidOperationException();

            PointCloud? meanPcl = MeshBlend.Mean(ModelUtils.LoadSpecimenTableRefs<PointCloud>(project, pclCol));
            if (meanPcl is null)
                return;

            if (!project.TryGetReference(dcaEntry.Payload.MeshCorrExtra!.BaseMeshCorrKey, out Mesh? baseMesh) || baseMesh is null)
                throw new InvalidOperationException();

            meshRend.Mesh = MeshNormals.MakeNormals(Mesh.FromPointCloud(meanPcl, baseMesh));
           
            UpdateRendererConfig();
        }

        private void ShowRejectionMap()
        {
            long rejectionRatesKey = dcaEntry.Payload.MeshCorrExtra?.VertexRejectionRatesKey ?? 0;
            if (rejectionRatesKey == 0)
                return;

            if (!project.TryGetReference(rejectionRatesKey, out Matrix? rejectionRates) || rejectionRates is null)
                return;

            meshRend.SetValueField(rejectionRates.Data);
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
