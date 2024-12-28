using System;
using System.CodeDom;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.SqlTypes;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using Warp9.Controls;
using Warp9.Data;
using Warp9.Forms;
using Warp9.Model;
using Warp9.Native;
using Warp9.Processing;

namespace Warp9.Viewer
{
    public class CompareGroupsViewerContent : IViewerContent
    {
        public CompareGroupsViewerContent(Project proj, long dcaEntityKey, string name)
        {
            project = proj;
            entityKey = dcaEntityKey;

            if (!proj.Entries.TryGetValue(entityKey, out ProjectEntry? entry) ||
                entry is null ||
                entry.Kind != ProjectEntryKind.MeshCorrespondence)
            {
                throw new InvalidOperationException();
            }

            long specTableKey = entry.Payload.MeshCorrExtra!.DcaConfig.SpecimenTableKey;
            if (!project.Entries.TryGetValue(specTableKey, out ProjectEntry? specTableEntry) ||
                specTableEntry is null ||
                specTableEntry.Kind != ProjectEntryKind.Specimens ||
                specTableEntry.Payload.Table is null)
            {
                throw new InvalidOperationException();
            }

            selectionA = new SpecimenTableSelection(specTableEntry.Payload.Table);
            selectionB = new SpecimenTableSelection(specTableEntry.Payload.Table);

            dcaEntry = entry;
            Name = name;

            sidebar = new CompareGroupsSideBar(this);
        }

        Project project;
        ProjectEntry dcaEntry;
        SpecimenTableSelection selectionA, selectionB;
        PointCloud? pclA = null, pclB = null;
        Page sidebar;
        long entityKey;
        bool renderWireframe = false, renderFill = true, renderSmooth = true, renderGrid = true, renderPhong = true;
        int mappedFieldIndex = 0;

        RenderItemMesh meshRend = new RenderItemMesh();
        RenderItemGrid gridRend = new RenderItemGrid();

        static readonly List<string> mappedFieldsList = new List<string>
        {
            "Vertex distance", "Signed vertex distance",
            "Surface distance", "Signed surface distance",
            "Triangle expansion", "Triangle shape"
        };

        public event PropertyChangedEventHandler? PropertyChanged;
        public event EventHandler ViewUpdated;

        public string Name { get; private init; }

        public bool RenderWireframe
        {
            get { return renderWireframe; }
            set { renderWireframe = value; UpdateRendererConfig(); OnPropertyChanged("RenderWireframe"); }
        }

        public bool RenderFill
        {
            get { return renderFill; }
            set { renderFill = value; UpdateRendererConfig(); OnPropertyChanged("RenderFill"); }
        }

        public bool RenderSmoothNormals
        {
            get { return renderSmooth; }
            set { renderSmooth = value; UpdateRendererConfig(); OnPropertyChanged("RenderSmoothNormals"); }
        }

        public bool RenderGrid
        {
            get { return renderGrid; }
            set { renderGrid = value; UpdateRendererConfig(); OnPropertyChanged("RenderGrid"); }
        }

        public bool RenderPhong
        {
            get { return renderPhong; }
            set { renderPhong = value; UpdateRendererConfig(); OnPropertyChanged("RenderPhong"); }
        }

        public int MappedFieldIndex
        {
            get { return mappedFieldIndex; }
            set { mappedFieldIndex = value; UpdateMappedField(); OnPropertyChanged("MappedFieldIndex"); }
        }

        public List<string> MappedFieldsList => mappedFieldsList;

        public void AttachRenderer(WpfInteropRenderer renderer)
        {
            UpdateRendererConfig();
            meshRend.Mesh = GetVisibleMesh();
            renderer.AddRenderItem(meshRend);
            renderer.AddRenderItem(gridRend);
        }

        public Page? GetSidebar()
        {
            return sidebar;
        }

        public void ViewportResized(Size size)
        {
        }

        public void InvokeGroupSelectionDialog(int group)
        {
            if (group != 0 && group != 1)
                throw new ArgumentException();

            SpecimenTableSelection sel = group == 0 ? selectionA : selectionB;

            SpecimenSelectorWindow ssw = new SpecimenSelectorWindow(sel);
            ssw.ShowDialog();

            // TODO: make Cancel have no effect
            if (group == 0)
                pclA = GetCorrPosBlend(sel);
            else
                pclB = GetCorrPosBlend(sel);

            UpdateMappedField();
        }

        public void SwapGroups()
        {

        }

        private Mesh? GetVisibleMesh()
        {
            if (!dcaEntry.Payload.Table!.Columns.TryGetValue("corrPcl", out SpecimenTableColumn? col) ||
                col is not SpecimenTableColumn<ProjectReferenceLink> pclCol)
                throw new InvalidOperationException();

            PointCloud? meanPcl = MeshBlend.Mean(ModelUtils.LoadSpecimenTableRefs<PointCloud>(project, pclCol));
            if (meanPcl is null)
                return null;

            if (!project.TryGetReference(dcaEntry.Payload.MeshCorrExtra!.BaseMeshCorrKey, out Mesh? baseMesh) || baseMesh is null)
                throw new InvalidOperationException();

            return MeshNormals.MakeNormals(Mesh.FromPointCloud(meanPcl, baseMesh));
        }

        private PointCloud? GetCorrPosBlend(SpecimenTableSelection sel)
        {
            if (!dcaEntry.Payload.Table!.Columns.TryGetValue("corrPcl", out SpecimenTableColumn? col) ||
                col is not SpecimenTableColumn<ProjectReferenceLink> pclCol)
                throw new InvalidOperationException();

            return MeshBlend.Mean(
                ModelUtils.LoadSpecimenTableRefs<PointCloud>(project, pclCol)
                    .Index()
                    .Where((t) => sel.Selected[t.Index])
                    .Select((t) => t.Item));
        }

        private void UpdateRendererConfig()
        {
            MeshRenderStyle style = 0;

            if (renderPhong)
                style |= MeshRenderStyle.PhongBlinn;

            if (!renderSmooth)
                style |= MeshRenderStyle.EstimateNormals;

            if (pclA is null || pclB is null)
                style |= MeshRenderStyle.ColorFlat;
            else
                style |= MeshRenderStyle.ColorLut;

            meshRend.Style = style;
            meshRend.RenderWireframe = renderWireframe;
            meshRend.RenderFace = renderFill;
            meshRend.RenderPoints = false;
            meshRend.RenderCull = false;
            meshRend.FillColor = Color.LightGray;
            meshRend.PointWireColor = Color.Black;
            meshRend.Lut = Lut.Create(256, Lut.ViridisColors);
            gridRend.Visible = renderGrid;
        }

        private void UpdateMappedField()
        {
            UpdateRendererConfig();
            if (pclA is null || pclB is null)
                return;

            int nv = pclA.VertexCount;
            float[] field = new float[nv];

            HomoMeshDiff.VertexDistance(field.AsSpan(), pclA, pclB);

            meshRend.SetValueField(field);
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
