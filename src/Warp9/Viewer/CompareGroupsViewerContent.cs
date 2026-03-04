using System;
using System.CodeDom;
using System.Collections;
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
    enum CompGroupRegType
    {
        None = 0,
        GpaGroups_OpaMean = 1
    }

    public class CompareGroupsViewerContent : GroupColormapMeshViewerContentBase
    {
        public CompareGroupsViewerContent(Project proj, long dcaEntityKey, string name) :
            base(proj, dcaEntityKey, name)
        { 
            selectionA = new SpecimenTableSelection(specTableEntry.Payload.Table);
            selectionB = new SpecimenTableSelection(specTableEntry.Payload.Table);
          
            sidebar = new CompareGroupsSideBar(this);            
        }
               
        SpecimenTableSelection selectionA, selectionB;
        PointCloud? pclA = null;
        Mesh? meshB = null;
        int regIndex = 0;
        CompareGroupsSideBar sidebar;

        protected static readonly List<string> regList = new List<string>
        {
            "None", "Group GPAs + Means OPA"
        };

        public List<string> RegistrationList => regList;

        public int RegistrationIndex
        {
            get { return regIndex; }
            set { regIndex = value; UpdateGroups(true, true); OnPropertyChanged("RegistrationIndex"); }
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

        public override void UpdateGroups(bool a, bool b)
        {
            CompGroupRegType regType = (CompGroupRegType)regIndex;

            if (a)
            {
                pclA = GetCorrPosBlend(selectionA, compareForm, regType);
            }

            if (b)
            {
                MeshBuilder mbB = MeshNormals.MakeNormals(GetCorrPosBlend(selectionB, compareForm, regType), 
                    meshMean!, NormalsAlgorithm.FastRobust);

                mbB.CopyIndicesFrom(meshMean!);
                meshB = mbB.ToMesh();
            }

            sidebar.SetSelectionDescription(Describe(selectionA), Describe(selectionB));
            UpdateMappedField(true);
        }

        public void SwapGroups()
        {
            SpecimenTableSelection selT = selectionA;
            selectionA = selectionB;
            selectionB = selT;
            UpdateGroups(true, true);
        }

       

        private string Describe(SpecimenTableSelection sel)
        {
            string desc = ModelUtils.DescribeSpecimenSelection(specTableEntry.Payload.Table, sel.Selected, out bool complete);
            if (!complete)
                return "≈ " + desc;

            return desc;
        }

        private PointCloud? GetCorrPosBlend(SpecimenTableSelection sel, bool form, CompGroupRegType reg = CompGroupRegType.None)
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

            IEnumerable<(int Index, PointCloud? Item)> groupPcls = ModelUtils.LoadSpecimenTableRefs<PointCloud>(project, pclCol)
                        .Index()
                        .Where((t) => sel.Selected[t.Index]);

            if (reg == CompGroupRegType.None)
            {
                if (cs is not null)
                {
                    return MeshBlend.WeightedMean(groupPcls.Select((t) => (t.Item, cs[t.Index])));
                }
                else
                {
                    return MeshBlend.WeightedMean(groupPcls.Select((t) => (t.Item, 1.0f)));
                }
            }
            else if(reg == CompGroupRegType.GpaGroups_OpaMean)
            { 
                PointCloud[] pcls = groupPcls.Select((t) => t.Item!).ToArray();
                Gpa gpaFit = Gpa.Fit(pcls);

                if (form)
                {
                    // Recompute mean but with properly scaled meshes.
                    return MeshBlend.WeightedMean(pcls.Index().
                        Select((t) => (t.Item, cs[t.Index] / gpaFit.GetTransform(t.Index).cs)));
                }
                else
                {
                    return gpaFit.Mean;
                }
            }

            throw new NotImplementedException();
        }

        protected override void UpdateMappedField(bool recalcField)
        {
            if (pclA is null || meshB is null || meshMean is null ||
                pclA.VertexCount != meshB.VertexCount || pclA.VertexCount != meshMean.VertexCount)
            {
                sidebar?.SetInfoText("");
                AttributeField = null;
                return;
            }

            PointCloud pclAreg = pclA;
            if (RegistrationIndex != 0)
            {
                Rigid3 rigid = RigidTransform.FitOpa(meshB, pclA);
                rigid.cs = 1;
                pclAreg = RigidTransform.TransformPosition(pclA, rigid);
            }
            // TODO: opa on means

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
                        HomoMeshDiff.FaceScalingFactor(field.AsSpan(), meshMean, pclA, meshB, false);
                        break;

                    case 5: // log10 triangle expansion
                        HomoMeshDiff.FaceScalingFactor(field.AsSpan(), meshMean, pclA, meshB, true);
                        break;
                }

                AttributeField = field;
            }

            float rmsDistance = MeshDistance.DistanceProcrustes(pclAreg, 1, meshB, 1, null);
            sidebar?.SetInfoText(string.Format("RMS distance: {0:F4}\nVertex count: {1}\n# Group A: {2}\n# Group B: {3}", 
                rmsDistance, pclA.VertexCount, selectionA.NumSelected(), selectionB.NumSelected()));

            base.UpdateMappedField(recalcField);
        }
    }
}
