using System;
using System.CodeDom;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.SqlTypes;
using System.Drawing;
using System.Drawing.Drawing2D;
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

    internal class SpecimenSelectionData
    {
        public SpecimenSelectionData(Project proj, SpecimenTable corrTable, SpecimenTableSelection sel, IFaceCollection faces)
        {
            project = proj;
            selection = sel;
            this.faces = faces;
            this.corrTable = corrTable;
        }

        Project project;
        SpecimenTableSelection selection;
        SpecimenTable corrTable;
        Mesh? mean;
        IFaceCollection faces;
        PointCloud[]? samples;
                
        public Mesh? MeanMesh => mean;
        public PointCloud[]? Samples => samples;
        public bool IsValid => samples is not null;

        public void Invalidate(CompGroupRegType reg, bool form)
        {
            samples = null; 

            if (!corrTable.Columns.TryGetValue(ModelConstants.CorrespondencePclColumnName, out SpecimenTableColumn? col) ||
               col is not SpecimenTableColumn<ProjectReferenceLink> pclCol)
                throw new InvalidOperationException();

            float[]? cs = null;
            if (form &&
                corrTable.Columns.TryGetValue(ModelConstants.CentroidSizeColumnName, out SpecimenTableColumn? col2) &&
                col2 is SpecimenTableColumn<double> csCol)
            {
                cs = csCol.Data.ConvertAll((t) => (float)t).ToArray();
            }
            
            IEnumerable<(int Index, PointCloud? Item)> groupPcls = ModelUtils.LoadSpecimenTableRefs<PointCloud>(project, pclCol)
                        .Index()
                        .Where((t) => selection.Selected[t.Index]);

            if (reg == CompGroupRegType.None && cs is null)
            {
                samples = groupPcls.Select((t) => t.Item!).ToArray();
            }
            else if (reg == CompGroupRegType.None && cs is not null)
            {
                samples = groupPcls.Select((t) => MeshBlend.ScalePosition(t.Item!, cs[t.Index])).ToArray();
            }
            else if (reg == CompGroupRegType.GpaGroups_OpaMean)
            {
                samples = groupPcls.Select((t) => t.Item!).ToArray();
                Gpa gpaFit = Gpa.Fit(samples);
                int m = samples.Length;

                for (int i = 0; i < m; i++)
                {
                    if (cs is null)
                    {
                        samples[i] = gpaFit.GetTransformed(i);
                    }
                    else
                    {
                        float[] cssel = groupPcls.Select((t) => cs[t.Index]).ToArray();
                        samples[i] = MeshBlend.ScalePosition(gpaFit.GetTransformed(i), cssel[i]);
                    }
                }
            }

            if (samples is null || samples.Length == 0)
            {
                mean = null;
                return;
            }

            PointCloud pclMean = MeshBlend.Mean(samples);
            MeshBuilder mbMean = MeshNormals.MakeNormals(pclMean, faces, NormalsAlgorithm.FastRobust);
            mbMean.CopyIndicesFrom(faces);
            mean = mbMean.ToMesh();
        }

        public void ApplyCorrection(Rigid3 rigid)
        {
            if(mean is not null)
                RigidTransform.TransformPosition(mean, rigid);

            if(samples is not null)
            {
                for (int i = 0; i < samples.Length; i++)
                    RigidTransform.TransformPosition(samples[i], rigid);
            }
        }
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
        SpecimenSelectionData dataA, dataB;   
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
            {
                Scene.Mesh0!.Mesh = new ReferencedData<Mesh>(meshMean);                
                dataA = new SpecimenSelectionData(project, dcaEntry.Payload.Table!, selectionA, meshMean);
                dataB = new SpecimenSelectionData(project, dcaEntry.Payload.Table!, selectionB, meshMean);
            }

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

            if((a || b) && dataA is not null && dataB is not null)
            {   
                dataA.Invalidate(regType, compareForm);
                dataB.Invalidate(regType, compareForm);

                if (regType != CompGroupRegType.None &&
                    dataA.MeanMesh is not null &&
                    dataB.MeanMesh is not null)
                {
                    Rigid3 rigid = RigidTransform.FitOpa(dataB.MeanMesh, dataA.MeanMesh);
                    rigid.cs = 1;
                    dataA.ApplyCorrection(rigid);
                    //pclAreg = RigidTransform.TransformPosition(pclA, rigid);
                }
            }

            sidebar.SetSelectionDescription(Describe(selectionA), Describe(selectionB));
            UpdateMappedField(true);
        }

        public void SwapGroups()
        {
            SpecimenTableSelection selT = selectionA;
            selectionA = selectionB;
            selectionB = selT;

            SpecimenSelectionData dataT = dataA;
            dataA = dataB;
            dataB = dataT;

            sidebar.SetSelectionDescription(Describe(selectionA), Describe(selectionB));
            UpdateMappedField(true);
        }

       
        private string Describe(SpecimenTableSelection sel)
        {
            string desc = ModelUtils.DescribeSpecimenSelection(specTableEntry.Payload.Table, sel.Selected, out bool complete);
            if (!complete)
                return "≈ " + desc;

            return desc;
        }
     
        protected override void UpdateMappedField(bool recalcField)
        {
            if (dataA is null || dataB is null || !dataA.IsValid || !dataB.IsValid || dataA.MeanMesh is null)
            {
                sidebar?.SetInfoText("");
                AttributeField = null;
                return;
            }

            int nv = dataA.MeanMesh!.VertexCount;

            StringBuilder infoText = new StringBuilder();

            if (recalcField)
            {
                if(field is null) field = new float[nv];                               

                switch (mappedFieldIndex)
                {
                    case 0: // normaliwise dispersion of A
                        if (dataA.MeanMesh is not null)
                            HomoMeshDiff.NormalwiseDispersion(field.AsSpan(), dataA.MeanMesh, dataA.Samples);
                        break;

                    case 1: // tangential dispersion of A
                        if (dataA.MeanMesh is not null)
                            HomoMeshDiff.TangentialDispersion(field.AsSpan(), dataA.MeanMesh, dataA.Samples);
                        break;

                    case 2: // vertex distance
                        if(dataA.MeanMesh is not null && dataB.MeanMesh is not null)
                            HomoMeshDiff.VertexDistance(field.AsSpan(), dataA.MeanMesh, dataB.MeanMesh);
                        break;

                    case 3: // signed vertex distance
                        if (dataA.MeanMesh is not null && dataB.MeanMesh is not null)
                            HomoMeshDiff.SignedVertexDistance(field.AsSpan(), dataA.MeanMesh, dataB.MeanMesh);
                        break;

                    case 4: // surface distance
                        if (dataA.MeanMesh is not null && dataB.MeanMesh is not null)
                            HomoMeshDiff.SurfaceDistance(field.AsSpan(), dataA.MeanMesh, dataB.MeanMesh);
                        break;

                    case 5: // signed surface distance
                        if (dataA.MeanMesh is not null && dataB.MeanMesh is not null)
                            HomoMeshDiff.SignedSurfaceDistance(field.AsSpan(), dataA.MeanMesh, dataB.MeanMesh);
                        break;

                    case 6: // triangle expansion
                        if (dataA.MeanMesh is not null && dataB.MeanMesh is not null)
                            HomoMeshDiff.FaceScalingFactor(field.AsSpan(), dataA.MeanMesh, dataA.MeanMesh, dataB.MeanMesh, false);
                        break;

                    case 7: // log10 triangle expansion
                        if (dataA.MeanMesh is not null && dataB.MeanMesh is not null)
                            HomoMeshDiff.FaceScalingFactor(field.AsSpan(), dataA.MeanMesh, dataA.MeanMesh, dataB.MeanMesh, true);
                        break;

                    case 8: // signed vertex distance significance
                        infoText.AppendLine("WARNING: p-values uncorrected!");
                        if (dataA.MeanMesh is not null && dataB.MeanMesh is not null)
                            HomoMeshDiff.SignedVertexDistanceWelchTest(field.AsSpan(), dataB.MeanMesh, dataA.Samples, dataB.Samples);
                        break;

                    case 9: // signed surface distance significance
                        infoText.AppendLine("WARNING: p-values uncorrected!");
                        if (dataA.MeanMesh is not null && dataB.MeanMesh is not null)
                            HomoMeshDiff.SignedSurfaceDistanceWelchTest(field.AsSpan(), dataB.MeanMesh, dataA.Samples, dataB.Samples);
                        break;
                }

                AttributeField = field;
            }
            
            if (dataA.MeanMesh is not null && dataB.MeanMesh is not null)
            {
                float rmsDistance = MeshDistance.DistanceProcrustes(dataA.MeanMesh!, 1, dataB.MeanMesh!, 1, null);
                infoText.AppendFormat("RMS distance: {0:F4}\nVertex count: {1}\n# Group A: {2}\n# Group B: {3}",
                    rmsDistance, nv, selectionA.NumSelected(), selectionB.NumSelected());                
            }

            sidebar?.SetInfoText(infoText.ToString());

            base.UpdateMappedField(recalcField);
        }
    }
}
