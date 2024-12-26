using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using Warp9.Controls;
using Warp9.Model;
using Warp9.Native;

namespace Warp9.Viewer
{
    public class CompareGroupsViewerContent : IViewerContent
    {
        public CompareGroupsViewerContent(Project proj, long dcaEntityKey, string name)
        {
            project = proj;
            entityKey = dcaEntityKey;

            if (!proj.Entries.TryGetValue(entityKey, out ProjectEntry? entry) ||
                entry.Kind != ProjectEntryKind.MeshCorrespondence)
                throw new InvalidOperationException();

            dcaEntry = entry;
            Name = name;

            sidebar = new CompareGroupsSideBar();
        }

        Project project;
        ProjectEntry dcaEntry;
        Page sidebar;
        long entityKey;
        bool renderWireframe = false, renderFill = true, renderSmooth = true, renderGrid = true;

        RenderItemMesh meshRend = new RenderItemMesh();
        RenderItemGrid gridRend = new RenderItemGrid();

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

        public void AttachRenderer(WpfInteropRenderer renderer)
        {
            renderer.AddRenderItem(meshRend);
            renderer.AddRenderItem(gridRend);
            UpdateRendererConfig();
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
            meshRend.Style = MeshRenderStyle.PhongBlinn | MeshRenderStyle.ColorFlat | (renderSmooth ? 0 : MeshRenderStyle.EstimateNormals);
            meshRend.RenderWireframe = renderWireframe;
            meshRend.RenderFace = renderFill;
            meshRend.RenderPoints = false;
            meshRend.RenderCull = false;
            meshRend.FillColor = Color.LightGray;
            meshRend.PointWireColor = Color.Black;

            gridRend.Visible = renderGrid;
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
