using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using Warp9.Controls;
using Warp9.Data;
using Warp9.Model;
using Warp9.Native;
using Warp9.Processing;

namespace Warp9.Viewer
{
    public class PcaScatterGrouping
    {
        public string DisplayName { get; init; }
        public long SourceEntry { get; init; }
        public string ColumnName { get; init; }
        public string[] Levels { get; init; }

        public override string ToString()
        {
            return DisplayName;
        }

        public static PcaScatterGrouping None = new PcaScatterGrouping()
        {
            DisplayName = "(none)",
            SourceEntry = -1,
            ColumnName = string.Empty,
            Levels = Array.Empty<string>()
        };
    }

    public class PcaSynthMeshViewerContent : ColormapMeshViewerContentBase
    {
        public PcaSynthMeshViewerContent(Project proj, long pcaEntityKey, string name) :
            base(proj, name)
        {
            entityKey = pcaEntityKey;

            if (!proj.Entries.TryGetValue(entityKey, out ProjectEntry? entry) ||
                entry is null ||
                entry.Kind != ProjectEntryKind.MeshPca ||
                entry.Payload.PcaExtra is null ||
                !proj.TryGetReference(entry.Payload.PcaExtra.DataKey, out MatrixCollection? pca) ||
                pca is null)
            {
                throw new InvalidOperationException();
            }

            pcaEntry = entry;
            pcaData = pca;

            if (!proj.TryGetReference(pcaEntry.Payload.PcaExtra.TemplateKey, out Mesh? baseMesh) || baseMesh is null)
                throw new InvalidOperationException();

            Pca? pcaObj = Pca.FromMatrixCollection(pca);
            if(pcaObj is null)
                throw new InvalidOperationException();

            pcaObject = pcaObj;
            meanMesh = baseMesh;
            tempSoa = new float[pcaObj.Dimension];
            posAos = new Vector3[pcaObj.Dimension / 3];
            normAos = new Vector3[pcaObj.Dimension / 3];

            //Groupings.Add(PcaScatterGrouping.None);
            //GatherGroupingsFromEntry(pcaEntityKey);

            sidebar = new PcaSynthMeshSideBar(this);
        }

        ProjectEntry pcaEntry;
        Pca pcaObject;
        long entityKey;
        PcaSynthMeshSideBar sidebar;
        MatrixCollection pcaData;
        Mesh meanMesh;
        int mappedFieldIndex = 0;
        int indexPcScatterX = 0, indexPcScatterY = 1;
        float[] tempSoa;
        Vector3[] posAos, normAos;

        static readonly List<string> mappedFieldsList = new List<string>
        {
            "None", "PC effect"
        };

        public int MappedFieldIndex
        {
            get { return mappedFieldIndex; }
            set { mappedFieldIndex = value; UpdateMappedField(true); OnPropertyChanged("MappedFieldIndex"); }
        }

        public List<string> MappedFieldsList => mappedFieldsList;
        public List<string> PrincipalComponents => CreatePrincipalComponentList().ToList();
        public List<PcaScatterGrouping> Groupings { get; } = new List<PcaScatterGrouping>();
        public int ScatterXAxisPcIndex
        {
            get { return indexPcScatterX; }
            set { indexPcScatterX = value; UpdateScatter(); }
        }

        public int ScatterYAxisPcIndex
        {
            get { return indexPcScatterY; }
            set { indexPcScatterY = value; UpdateScatter(); }
        }

        public override Page? GetSidebar()
        {
            return sidebar;
        }

        public void ScatterPlotPosChanged(ScatterPlotPosInfo sppi)
        {
            pcaObject.Synthesize(tempSoa.AsSpan(), (indexPcScatterX, sppi.Pos.X), (indexPcScatterY, sppi.Pos.Y));
            OverrideVertices(tempSoa);
            UpdateViewer();
        }

        public override void AttachRenderer(WpfInteropRenderer renderer)
        {
            base.AttachRenderer(renderer);
            UpdateScatter();
            ShowMesh();            
            UpdateMappedField(true);
        }

        /*private void GatherGroupingsFromEntry(long key)
        {
            if (project.Entries.TryGetValue(key, out ProjectEntry? entry) && entry is not null)
            {
                if (entry.Payload.Table is not null)
                {
                    foreach (var kvp in entry.Payload.Table.Columns)
                    {
                        if (kvp.Value.ColumnType == SpecimenTableColumnType.Factor && kvp.Value.Names is not null)
                        {
                            Groupings.Add(new PcaScatterGrouping() 
                            { 
                                DisplayName = $"{kvp.Key} in {entry.Name}",
                                SourceEntry = entry.Id,
                                ColumnName = kvp.Key,
                                Levels = kvp.Value.Names
                            });
                        }
                    }
                }

                foreach (long dep in entry.Deps)
                    GatherGroupingsFromEntry(dep);

                if (entry.Deps.Count == 0 && entry.Payload.PcaExtra is not null)
                    GatherGroupingsFromEntry(entry.Payload.PcaExtra.Info.ParentEntityKey);

                if (entry.Deps.Count == 0 && entry.Payload.MeshCorrExtra is not null)
                    GatherGroupingsFromEntry(entry.Payload.MeshCorrExtra.DcaConfig.SpecimenTableKey);
            }
        }*/

        private void ShowMesh()
        {
            // Set the base mesh, but we'll only keep indices from it. Also set the key to
            // allow deduplication when saving.
            Scene.Mesh0!.Mesh = new ReferencedData<Mesh>(meanMesh, pcaEntry.Payload.PcaExtra.TemplateKey);

            // Override positions with the mean PCA model. Override normals, too.
            pcaObject.Synthesize(tempSoa.AsSpan());
            OverrideVertices(tempSoa);           
        }

        private void OverrideVertices(float[] synthSoa)
        {
            MeshUtils.CopySoaToAos(posAos.AsSpan(), MemoryMarshal.Cast<float, byte>(synthSoa.AsSpan()));
            Scene.Mesh0!.PositionOverride = new ReferencedData<Vector3[]>(posAos);

            // Recalculate normals and override them, too.
            if (meanMesh.TryGetIndexData(out ReadOnlySpan<FaceIndices> indices))
            {
                MeshNormals.MakeNormalsFast(normAos.AsSpan(), posAos.AsSpan(), indices);
                Scene.Mesh0!.NormalOverride = new ReferencedData<Vector3[]>(normAos);
            }
        }

        private void UpdateScatter()
        {
            if (pcaData.TryGetMatrix(Native.Pca.KeyScores, out Matrix<float>? pcaScores) &&
              pcaScores is not null &&
              indexPcScatterX >= 0 && indexPcScatterX < pcaScores.Columns &&
              indexPcScatterY >= 0 && indexPcScatterY < pcaScores.Columns)
            {
                sidebar.UpdateScatterplot(pcaScores.GetColumn(indexPcScatterX), pcaScores.GetColumn(indexPcScatterY));
            }
        }

        private IEnumerable<string> CreatePrincipalComponentList()
        {
            if (pcaData.TryGetMatrix(Native.Pca.KeyPcVariance, out Matrix<float>? pcaVar) &&
                pcaVar is not null &&
                pcaData.TryGetMatrix(Native.Pca.KeyScores, out Matrix<float>? pcaScores) &&
                pcaScores is not null)
            {
                int n = Math.Min(pcaVar.Rows, pcaScores.Columns);

                for (int i = 0; i < n; i++)
                {
                    yield return string.Format("PC{0} ({1:F1} %)", i + 1, 100.0f * pcaVar[i, 0]);
                }
            }
        }

        protected override void UpdateMappedField(bool recalcField)
        {
            if (recalcField)
            {
                AttributeField = mappedFieldIndex switch
                {
                    _ => null
                };
            }

            base.UpdateMappedField(recalcField);
        }
    }
}
