using System;
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
    public class CompareGroupsViewerContent : ColormapMeshViewerContentBase
    {
        public CompareGroupsViewerContent(Project proj, long dcaEntityKey, string name) :
            base(proj, name)
        {
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

            sidebar = new CompareGroupsSideBar(this);            
        }

        ProjectEntry dcaEntry;
        SpecimenTableSelection selectionA, selectionB;
        PointCloud? pclA = null;
        Mesh? meshB = null;
        Mesh? meshMean = null;
        CompareGroupsSideBar sidebar;
        long entityKey;       
        bool compareForm = false;
        float[]? field = null;
        int mappedFieldIndex = 0;

        static readonly List<string> mappedFieldsList = new List<string>
        {
            "Vertex distance", "Signed vertex distance",
            "Surface distance", "Signed surface distance",
            "Triangle expansion", "Triangle shape"
        };
              
        public int MappedFieldIndex
        {
            get { return mappedFieldIndex; }
            set { mappedFieldIndex = value; UpdateMappedField(true); OnPropertyChanged("MappedFieldIndex"); }
        }

        public bool ModelsForm
        {
            get { return compareForm; }
            set { compareForm = value; UpdateGroups(true, true); OnPropertyChanged("ModelsForm"); }
        }


        public List<string> MappedFieldsList => mappedFieldsList;

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

            if (meshMean is null)
                throw new InvalidOperationException();

            SpecimenTableSelection sel = group == 0 ? selectionA : selectionB;

            SpecimenSelectorWindow ssw = new SpecimenSelectorWindow(sel);
            ssw.ShowDialog();

            UpdateGroups(group == 0, group == 1);
        }

        public override string DescribeScene()
        {
            string descA = ModelUtils.DescribeSpecimenSelection(selectionA.Table, selectionA.Selected, out bool complA);
            string descB = ModelUtils.DescribeSpecimenSelection(selectionB.Table, selectionB.Selected, out bool complB);

            return string.Format("{0} on {5} of ({1}){2} - ({3}){4}",
                mappedFieldsList[mappedFieldIndex],
                descB, complB ? "?" : string.Empty,
                descA, complA ? "?" : string.Empty,
                ModelsForm ? "form" : "shape");
        }

        public void UpdateGroups(bool a, bool b)
        {
            if (a)
            {
                pclA = GetCorrPosBlend(selectionA, compareForm);
            }

            if (b)
            {
                MeshBuilder mbB = MeshNormals.MakeNormals(GetCorrPosBlend(selectionB, compareForm), meshMean!, NormalsAlgorithm.FastRobust);
                mbB.CopyIndicesFrom(meshMean!);
                meshB = mbB.ToMesh();
            }

            UpdateMappedField(true);
        }

        public void SwapGroups()
        {
        }
               

        private Mesh? GetVisibleMesh()
        {
            if (!dcaEntry.Payload.Table!.Columns.TryGetValue(ModelConstants.CorrespondencePclColumnName, out SpecimenTableColumn? col) ||
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
            if (!dcaEntry.Payload.Table!.Columns.TryGetValue(ModelConstants.CorrespondencePclColumnName, out SpecimenTableColumn? col) ||
                col is not SpecimenTableColumn<ProjectReferenceLink> pclCol)
                throw new InvalidOperationException();

            float[]? cs = null;
            if (form &&
                dcaEntry.Payload.Table!.Columns.TryGetValue(ModelConstants.CentroidSizeColumnName, out SpecimenTableColumn? col2) &&
                col2 is SpecimenTableColumn<double> csCol)
            {
                cs = csCol.Data.ConvertAll((t) => (float)t).ToArray();
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

        protected override void UpdateMappedField(bool recalcField)
        {
            if (pclA is null || meshB is null)
                return;

            if (recalcField)
            {
                int nv = pclA.VertexCount;
                
                if(field is null) field = new float[nv];

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

                    case 4: // triangle expansion
                        // TODO
                        break;

                    case 5: // triangle shape
                        // TODO
                        break;
                }

                AttributeField = field;
            }

            base.UpdateMappedField(recalcField);
        }
    }
}
