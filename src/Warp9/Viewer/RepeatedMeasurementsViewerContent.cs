using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Text;
using System.Windows.Controls;
using Warp9.Controls;
using Warp9.Data;
using Warp9.Forms;
using Warp9.Model;
using Warp9.Native;
using Warp9.Processing;

namespace Warp9.Viewer
{
    public class RepeatedMeasurementsViewerContent : GroupColormapMeshViewerContentBase
    {
        public RepeatedMeasurementsViewerContent(Project proj, long dcaEntityKey, string name) :
            base(proj, dcaEntityKey, name)
        {
            sidebar = new RepeatedMeasurementsSideBar(this);
        }

        RepeatedMeasurementsSideBar sidebar;
        int sourceOperationIndex = 0;
               
        static readonly List<string> sourceOperationList = new List<string>
        {
            "A", "B", "B - A"
        };

       
        public List<string> SourceOperationList => sourceOperationList;

        public int SourceOperationIndex
        {
            get { return sourceOperationIndex; }
            set { sourceOperationIndex = value; UpdateMappedField(true); OnPropertyChanged("SourceOperationIndex"); }
        }

        public override void AttachRenderer(WpfInteropRenderer renderer)
        {
            field = null;
            meshMean = GetVisibleMesh();

            if (meshMean is not null)
                Scene.Mesh0!.Mesh = new ReferencedData<Mesh>(meshMean);

            base.AttachRenderer(renderer);
        }

        public override Page? GetSidebar()
        {
            return sidebar;
        }

        public void InvokeGroupSelectionDialog(int group)
        {
            if (group != 0 && group != 1)
                throw new ArgumentException();

            RepeatedMeasurementsConfigWindow wnd = new RepeatedMeasurementsConfigWindow();
            wnd.ShowDialog();
        }

        public void SwapGroups()
        {
            throw new NotImplementedException();
            /*SpecimenTableSelection selT = selectionA;
            selectionA = selectionB;
            selectionB = selT;
            UpdateGroups(true, true);*/
        }

        public override void UpdateGroups(bool a, bool b)
        {
            UpdateMappedField(true);
        }

        protected override void UpdateMappedField(bool recalcField)
        {
            base.UpdateMappedField(recalcField);
        }

      
    }
}
