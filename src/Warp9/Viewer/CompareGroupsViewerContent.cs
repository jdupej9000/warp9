﻿using System;
using System.CodeDom;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.SqlTypes;
using System.Drawing;
using System.Dynamic;
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
using Warp9.Utils;

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
        PointCloud? pclA = null;
        Mesh? meshB = null;
        Lut? lut = null;
        Mesh? meshMean = null;
        CompareGroupsSideBar sidebar;
        long entityKey;
        bool renderWireframe = false, renderFill = true, renderSmooth = true, renderGrid = true, renderDiffuse = true;
        bool compareForm = false;
        float valueMin = 0, valueMax = 1;
        float? valueShow = null; 
        int mappedFieldIndex = 0, paletteIndex = 0;

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

        public bool RenderDiffuse
        {
            get { return renderDiffuse; }
            set { renderDiffuse = value; UpdateRendererConfig(); OnPropertyChanged("RenderDiffuse"); }
        }

        public int MappedFieldIndex
        {
            get { return mappedFieldIndex; }
            set { mappedFieldIndex = value; UpdateMappedField(); OnPropertyChanged("MappedFieldIndex"); }
        }

        public float ValueMin
        {
            get { return valueMin; }
            set { valueMin = value; UpdateMappedField(); OnPropertyChanged("ValueMin"); }
        }

        public float ValueMax
        {
            get { return valueMax; }
            set { valueMax = value; UpdateMappedField(); OnPropertyChanged("ValueMax"); }
        }

        public bool ModelsForm
        {
            get { return compareForm; }
            set { compareForm = value; UpdateGroups(true, true); OnPropertyChanged("ModelsForm"); }
        }

        public int PaletteIndex
        {
            get { return paletteIndex; }
            set { paletteIndex = value; UpdateLut(); OnPropertyChanged("PaletteIndex"); }
        }


        public List<string> MappedFieldsList => mappedFieldsList;
        public List<PaletteItem> Palettes => PaletteItem.KnownPaletteItems;

        public void AttachRenderer(WpfInteropRenderer renderer)
        {
            UpdateRendererConfig();
            meshMean = GetVisibleMesh();
            meshRend.Mesh = meshMean;
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

            if (meshMean is null)
                throw new InvalidOperationException();

            SpecimenTableSelection sel = group == 0 ? selectionA : selectionB;

            SpecimenSelectorWindow ssw = new SpecimenSelectorWindow(sel);
            ssw.ShowDialog();

            UpdateGroups(group == 0, group == 1);
        }

        public void UpdateGroups(bool a, bool b)
        {
            if (a)
            {
                pclA = GetCorrPosBlend(selectionA, compareForm);
            }

            if (b)
            {
                MeshBuilder mbB = MeshNormals.MakeNormals(GetCorrPosBlend(selectionB, compareForm), meshMean);
                mbB.CopyIndicesFrom(meshMean);
                meshB = mbB.ToMesh();
            }

            UpdateMappedField();
        }

        public void SwapGroups()
        {
        }

        public void MeshScaleHover(float? value)
        {
            valueShow = value;
            UpdateRendererStyle();
            ViewUpdated?.Invoke(this, EventArgs.Empty);
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

        private PointCloud? GetCorrPosBlend(SpecimenTableSelection sel, bool form)
        {
            if (!dcaEntry.Payload.Table!.Columns.TryGetValue("corrPcl", out SpecimenTableColumn? col) ||
                col is not SpecimenTableColumn<ProjectReferenceLink> pclCol)
                throw new InvalidOperationException();

            float[]? cs = null;
            if (form &&
                dcaEntry.Payload.Table!.Columns.TryGetValue("cs", out SpecimenTableColumn? col2) &&
                col2 is SpecimenTableColumn<double> csCol)
            {
                cs = csCol.Data.Select((t) => (float)t).ToArray();
            }

            if (cs is not null)
            {
                return MeshBlend.WeightedMean(
                    ModelUtils.LoadSpecimenTableRefs<PointCloud>(project, pclCol)
                        .Index()
                        .Where((t) => sel.Selected[t.Index])
                        .Select((t) => (t.Item, cs[t.Index])));
            }
            else
            {
                return MeshBlend.WeightedMean(
                    ModelUtils.LoadSpecimenTableRefs<PointCloud>(project, pclCol)
                        .Index()
                        .Where((t) => sel.Selected[t.Index])
                        .Select((t) => (t.Item, 1.0f)));
            }
        }

        private void UpdateRendererStyle()
        {
            MeshRenderStyle style = 0;

            if (renderDiffuse)
                style |= MeshRenderStyle.DiffuseLighting;

            if (!renderSmooth)
                style |= MeshRenderStyle.EstimateNormals;

            if (pclA is null || meshB is null)
                style |= MeshRenderStyle.ColorFlat;
            else
                style |= MeshRenderStyle.ColorLut;

            if (valueShow.HasValue)
            {
                style |= MeshRenderStyle.ShowValueLevel;
                meshRend.LevelValue = valueShow.Value;
            }

            meshRend.Style = style;
        }

        private void UpdateLut()
        {
            lut = null;
            UpdateRendererConfig();
        }

        private void UpdateRendererConfig()
        {
            UpdateRendererStyle();
            meshRend.RenderWireframe = renderWireframe;
            meshRend.RenderFace = renderFill;
            meshRend.RenderPoints = false;
            meshRend.RenderCull = false;
            meshRend.FillColor = Color.LightGray;
            meshRend.PointWireColor = Color.Black;
            Lut lutLocal = lut ?? Lut.Create(256, Palettes[PaletteIndex].Stops);
            sidebar?.SetLut(lutLocal);
            lut = lutLocal;
            meshRend.Lut = lutLocal;
            meshRend.ValueMin = valueMin;
            meshRend.ValueMax = valueMax;
            gridRend.Visible = renderGrid;
        }

        private void UpdateMappedField()
        {
            UpdateRendererConfig();
            if (pclA is null || meshB is null)
                return;

            int nv = pclA.VertexCount;
            float[] field = new float[nv];

            switch (mappedFieldIndex)
            {
                case 0: // vertex distance
                    HomoMeshDiff.VertexDistance(field.AsSpan(), pclA, meshB);
                    break;

                case 1: // signed vertex distance
                    HomoMeshDiff.SignedVertexDistance(field.AsSpan(), pclA, meshB);
                    break;

                case 2: // surface distance
                    HomoMeshDiff.SurfaceDistance(field.AsSpan(), pclA, meshB);
                    break;

                case 3: // signed surface distance
                    HomoMeshDiff.SignedSurfaceDistance(field.AsSpan(), pclA, meshB);
                    break;
            }

            meshRend.SetValueField(field);
            sidebar.SetHist(field, meshRend.Lut ?? Lut.Create(256, Lut.ViridisColors), valueMin, valueMax);
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
