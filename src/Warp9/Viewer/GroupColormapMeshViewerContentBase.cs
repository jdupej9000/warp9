using System;
using System.Collections.Generic;
using System.Text;
using Warp9.Data;
using Warp9.Model;
using Warp9.Processing;

namespace Warp9.Viewer
{
    public class GroupColormapMeshViewerContentBase : ColormapMeshViewerContentBase
    {
        public GroupColormapMeshViewerContentBase(Project proj, long dcaEntityKey, string name) :
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
            if (!project.Entries.TryGetValue(specTableKey, out ProjectEntry? stEntry) ||
                stEntry is null ||
                stEntry.Kind != ProjectEntryKind.Specimens ||
                stEntry.Payload.Table is null)
            {
                throw new InvalidOperationException();
            }

            specTableEntry = stEntry;
            dcaEntry = entry;
        }

        protected ProjectEntry dcaEntry;
        protected ProjectEntry specTableEntry;
        protected readonly long entityKey;
        protected int mappedFieldIndex = 0;
        protected bool compareForm = false;
        protected Mesh? meshMean = null;
        protected float[]? field = null;

        protected static readonly List<string> mappedFieldsList = new List<string>
        {
            "Vertex distance", "Signed vertex distance",
            "Surface distance", "Signed surface distance",
            "Triangle expansion", "Log10 triangle expansion"
        };

        public List<string> MappedFieldsList => mappedFieldsList;

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

        protected Mesh? GetVisibleMesh()
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

        public virtual void UpdateGroups(bool a, bool b)
        {
        }
    }
}
