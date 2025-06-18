using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Warp9.Controls;
using Warp9.Data;
using Warp9.Utils;

namespace Warp9.Viewer
{
    public class ColormapMeshViewerContentBase : IViewerContent, INotifyPropertyChanged
    {
        public ColormapMeshViewerContentBase(string name)
        {
            Name = name;
        }

        protected RenderItemMesh meshRend = new RenderItemMesh();
        protected RenderItemGrid gridRend = new RenderItemGrid();
        protected bool renderWireframe = false, renderFill = true, renderSmooth = true, renderGrid = true, renderDiffuse = true, renderLut = true;
        protected float? valueShow = null;
        protected float valueMin = 0, valueMax = 1;
        protected Lut? lut = null;
        protected int paletteIndex = 0;

        public event EventHandler? ViewUpdated;
        public event PropertyChangedEventHandler? PropertyChanged;

        public string Name { get; private init; }

        public List<PaletteItem> Palettes => PaletteItem.KnownPaletteItems;

        public bool RenderLut
        {
            get { return renderLut; }
            set { renderLut = value; UpdateRendererConfig(); OnPropertyChanged("RenderLut"); }
        }

        public bool RenderGrid
        {
            get { return renderGrid; }
            set { renderGrid = value; UpdateRendererConfig(); OnPropertyChanged("RenderGrid"); }
        }

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

        public bool RenderDiffuse
        {
            get { return renderDiffuse; }
            set { renderDiffuse = value; UpdateRendererConfig(); OnPropertyChanged("RenderDiffuse"); }
        }

        public int PaletteIndex
        {
            get { return paletteIndex; }
            set { paletteIndex = value; UpdateLut(); OnPropertyChanged("PaletteIndex"); }
        }

        public float ValueMin
        {
            get { return valueMin; }
            set { valueMin = value; UpdateMappedFieldRange(); OnPropertyChanged("ValueMin"); }
        }

        public float ValueMax
        {
            get { return valueMax; }
            set { valueMax = value; UpdateMappedFieldRange(); OnPropertyChanged("ValueMax"); }
        }

        public virtual void AttachRenderer(WpfInteropRenderer renderer)
        {
            renderer.AddRenderItem(meshRend);
            renderer.AddRenderItem(gridRend);
            UpdateRendererConfig();
        }

        public virtual Page? GetSidebar()
        {
            return null;
        }

        public void UpdateViewer()
        {
            ViewUpdated?.Invoke(this, EventArgs.Empty);
        }


        public void ViewportResized(System.Drawing.Size size)
        {
        }
        public void MeshScaleHover(float? value)
        {
            valueShow = value;
            UpdateRendererStyle();
            UpdateViewer();
        }

        protected virtual void UpdateRendererStyle()
        {
            MeshRenderStyle style = 0;

            if (renderDiffuse)
                style |= MeshRenderStyle.DiffuseLighting;

            if (!renderSmooth)
                style |= MeshRenderStyle.EstimateNormals;

            if (renderLut)
                style |= MeshRenderStyle.ColorLut;
            else
                style |= MeshRenderStyle.ColorFlat;

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

        protected virtual void UpdateRendererConfig()
        {
            UpdateRendererStyle();
            meshRend.RenderWireframe = renderWireframe;
            meshRend.RenderFace = renderFill;
            meshRend.RenderPoints = false;
            meshRend.RenderCull = false;
            meshRend.FillColor = System.Drawing.Color.LightGray;
            meshRend.PointWireColor = System.Drawing.Color.Black;
            Lut lutLocal = lut ?? Lut.Create(256, Palettes[PaletteIndex].Stops);          
            lut = lutLocal;
            meshRend.Lut = lutLocal;
            meshRend.ValueMin = valueMin;
            meshRend.ValueMax = valueMax;
            gridRend.Visible = renderGrid;
        }

        protected virtual void UpdateMappedFieldRange()
        {

        }

        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
            UpdateViewer();
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
