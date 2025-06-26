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
    public class ColormapMeshViewerContentBase : SceneViewerContentBase
    {
        public ColormapMeshViewerContentBase(Project proj, string name) :
            base(proj, name)
        {
            Scene.Mesh0 = new MeshSceneElement();
        }

        protected int paletteIndex = 0;

        public List<PaletteItem> Palettes => PaletteItem.KnownPaletteItems;

        public bool RenderLut
        {
            get { return Scene.Mesh0!.Flags.HasFlag(MeshRenderFlags.UseLut); }
            set { SetMeshRendFlag(Scene.Mesh0!, MeshRenderFlags.UseLut, value); OnPropertyChanged("RenderLut"); }
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

        public bool RenderSpecular
        {
            get { return Scene.Mesh0!.Flags.HasFlag(MeshRenderFlags.Specular); }
            set { SetMeshRendFlag(Scene.Mesh0!, MeshRenderFlags.Specular, value); OnPropertyChanged("RenderSpecular"); }
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


        public void MeshScaleHover(float? value)
        {
            //valueShow = value;
            //UpdateRendererStyle();
           // UpdateViewer();
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

    }
}
