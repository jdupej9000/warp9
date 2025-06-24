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
using Warp9.Model;
using Warp9.Scene;
using Warp9.Utils;

namespace Warp9.Viewer
{
    public class ColormapMeshViewerContentBase : IViewerContent, INotifyPropertyChanged
    {
        public ColormapMeshViewerContentBase(Project proj, string name)
        {
            Name = name;
            project = proj;
            sceneRend = new ViewerSceneRenderer(project);
            sceneRend.Scene = scene;
            scene.Mesh0 = new MeshSceneElement();
            scene.Grid = new GridSceneElement();
        }


        protected Project project;
        private ViewerScene scene = new ViewerScene();
        private ViewerSceneRenderer sceneRend;

        protected int paletteIndex = 0;

        public event EventHandler? ViewUpdated;
        public event PropertyChangedEventHandler? PropertyChanged;

        public string Name { get; private init; }
        public ViewerScene Scene => scene;

        public List<PaletteItem> Palettes => PaletteItem.KnownPaletteItems;

        public bool RenderLut
        {
            get { return Scene.Mesh0!.Flags.HasFlag(MeshRenderFlags.UseLut); }
            set { SetMeshRendFlag(Scene.Mesh0!, MeshRenderFlags.UseLut, value); OnPropertyChanged("RenderLut"); }
        }

        public bool RenderGrid
        {
            get { return Scene.Grid!.Visible; }
            set { Scene.Grid!.Visible = value; OnPropertyChanged("RenderGrid"); }
        }

        public bool RenderWireframe
        {
            get { return Scene.Mesh0!.Flags.HasFlag(MeshRenderFlags.Wireframe); }
            set { SetMeshRendFlag(Scene.Mesh0!, MeshRenderFlags.Wireframe, value); OnPropertyChanged("RenderWireframe"); }
        }

        public bool RenderFill
        {
            get { return Scene.Mesh0!.Flags.HasFlag(MeshRenderFlags.Fill); }
            set { SetMeshRendFlag(Scene.Mesh0!, MeshRenderFlags.Fill, value); OnPropertyChanged("RenderFill"); }
        }

        public bool RenderSmoothNormals
        {
            get { return Scene.Mesh0!.Flags.HasFlag(MeshRenderFlags.EstimateNormals); }
            set { SetMeshRendFlag(Scene.Mesh0!, MeshRenderFlags.EstimateNormals, value); OnPropertyChanged("RenderSmoothNormals"); }
        }

        public bool RenderDiffuse
        {
            get { return Scene.Mesh0!.Flags.HasFlag(MeshRenderFlags.Diffuse); }
            set { SetMeshRendFlag(Scene.Mesh0!, MeshRenderFlags.Diffuse, value); OnPropertyChanged("RenderDiffuse"); }
        }

        public int PaletteIndex
        {
            get { return paletteIndex; }
            set { paletteIndex = value; UpdateLut(); OnPropertyChanged("PaletteIndex"); }
        }

        public float ValueMin
        {
            get { return Scene.Mesh0!.AttributeMin; }
            set { Scene.Mesh0!.AttributeMin = value; UpdateMappedFieldRange(); OnPropertyChanged("ValueMin"); }
        }

        public float ValueMax
        {
            get { return Scene.Mesh0!.AttributeMax; }
            set { Scene.Mesh0!.AttributeMax = value; UpdateMappedFieldRange(); OnPropertyChanged("ValueMax"); }
        }

        public virtual void AttachRenderer(WpfInteropRenderer renderer)
        {
            sceneRend.AttachToRenderer(renderer);
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
            Scene.Viewport = size;
        }

        public void MeshScaleHover(float? value)
        {
            //valueShow = value;
            //UpdateRendererStyle();
           // UpdateViewer();
        }

        protected void SetMeshRendFlag(MeshSceneElement elem, MeshRenderFlags flag, bool set)
        {
            if(set) elem.Flags |= flag;
            else elem.Flags &= ~flag;
        }

        private void UpdateLut()
        {
            //lut = null;
            //UpdateRendererConfig();
        }

       /* protected virtual void UpdateRendererConfig()
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
        */

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
