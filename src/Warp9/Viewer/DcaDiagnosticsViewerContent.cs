using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Xml.Linq;
using Warp9.Controls;
using Warp9.Data;
using Warp9.Model;
using Warp9.Processing;

namespace Warp9.Viewer
{
    public class DcaDiagnosticsViewerContent : ColormapMeshViewerContentBase
    {
        public DcaDiagnosticsViewerContent(Project proj, long dcaEntityKey, string name) :
            base(proj, name)
        {
            entityKey = dcaEntityKey;

            if (!proj.Entries.TryGetValue(entityKey, out ProjectEntry? entry) ||
                entry.Kind != ProjectEntryKind.MeshCorrespondence)
                throw new InvalidOperationException();

            dcaEntry = entry;
            sidebar = new DcaDiagnosticsSideBar(this);
        }

        DcaDiagnosticsSideBar sidebar;
        ProjectEntry dcaEntry;       
        long entityKey;
        int mappedFieldIndex = 0;
        int nv = 0;

        static readonly List<string> mappedFieldsList = new List<string>
        {
            "Vertex rejection ratio", "Index"
        };

        public int MappedFieldIndex
        {
            get { return mappedFieldIndex; }
            set { mappedFieldIndex = value; UpdateMappedField(true); OnPropertyChanged("MappedFieldIndex"); }
        }

        public List<string> MappedFieldsList => mappedFieldsList;
            

        public override void AttachRenderer(WpfInteropRenderer renderer)
        {
            ShowMesh();
            UpdateMappedField(true);

            base.AttachRenderer(renderer);
        }

        public override Page? GetSidebar()
        {
            return sidebar;
        }

        private void ShowMesh()
        {
            if (!dcaEntry.Payload.Table!.Columns.TryGetValue(ModelConstants.CorrespondencePclColumnName, out SpecimenTableColumn? col) ||
              col is not SpecimenTableColumn<ProjectReferenceLink> pclCol)
                throw new InvalidOperationException();

            PointCloud? meanPcl = MeshBlend.Mean(ModelUtils.LoadSpecimenTableRefs<PointCloud>(project, pclCol));
            if (meanPcl is null)
                return;

            if (!project.TryGetReference(dcaEntry.Payload.MeshCorrExtra!.BaseMeshCorrKey, out Mesh? baseMesh) || baseMesh is null)
                throw new InvalidOperationException();

            Scene.Mesh0!.Mesh = new ReferencedData<Mesh>(MeshNormals.MakeNormals(Mesh.FromPointCloud(meanPcl, baseMesh)));
            nv = meanPcl.VertexCount;
        }

        private float[]? MakeRejectionMap()
        {
            long rejectionRatesKey = dcaEntry.Payload.MeshCorrExtra?.VertexRejectionRatesKey ?? 0;
            if (rejectionRatesKey == 0)
                return null;

            if (!project.TryGetReference(rejectionRatesKey, out MatrixCollection? rejectionRatesCol) || 
                rejectionRatesCol is null ||
                !rejectionRatesCol.TryGetMatrix(ModelConstants.VertexRejectionRatesKey, out Matrix<float>? rejectionRates) ||
                rejectionRates is null)
                return null;

            return rejectionRates.Data;
        }

        private float[] MakeIndexMap()
        {
            float[] ret = new float[nv];
            for(int i = 0; i < nv; i++)
                ret[i] = (float)i / (float)(nv-1);
            return ret;
        }

        protected override void UpdateMappedField(bool recalcField)
        {
            if (recalcField)
            {
                AttributeField = mappedFieldIndex switch
                {
                    0 => MakeRejectionMap(),
                    1 => MakeIndexMap(),
                    _ => null
                };
            }

            base.UpdateMappedField(recalcField);
        }
    }
}
