using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Xml.Linq;
using Warp9.Controls;
using Warp9.Model;
using Warp9.Scene;

namespace Warp9.Viewer
{
    public class SceneViewerContentBase : IViewerContent, INotifyPropertyChanged
    {
        public SceneViewerContentBase(Project proj, string name)
        {
            Name = name;
            project = proj;
            sceneRend = new ViewerSceneRenderer(project);
            sceneRend.Scene = scene;
            scene.Grid = new GridSceneElement();
        }

        protected Project project;
        private ViewerScene scene = new ViewerScene();
        private ViewerSceneRenderer? sceneRend;

        public event EventHandler? ViewUpdated;
        public event PropertyChangedEventHandler? PropertyChanged;

        public string Name { get; private init; }
        public ViewerScene Scene => scene;

        public bool RenderGrid
        {
            get { return Scene.Grid!.Visible; }
            set { Scene.Grid!.Visible = value; OnPropertyChanged("RenderGrid"); }
        }

        public virtual void AttachRenderer(WpfInteropRenderer renderer)
        {
            if (sceneRend is not null && sceneRend.Renderer == renderer)
                return;

            sceneRend = new ViewerSceneRenderer(project);
            sceneRend.AttachToRenderer(renderer);
            sceneRend.Scene = scene;
        }

        public void DetachRenderer()
        {
            if (sceneRend is not null)
            {
                sceneRend.DetachRenderer();
                sceneRend = null;
            }            
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

        public void ViewChanged(CameraInfo ci)
        {
            Scene.ViewMatrix = ci.ViewMat;           
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

        public static void SetMeshRendFlag(MeshSceneElement elem, MeshRenderFlags flag, bool set)
        {
            if (set) elem.Flags |= flag;
            else elem.Flags &= ~flag;
        }
    }
}
