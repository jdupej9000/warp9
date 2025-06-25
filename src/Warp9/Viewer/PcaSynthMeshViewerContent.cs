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
            base(name)
        {
            project = proj;
            entityKey = pcaEntityKey;

            if (!proj.Entries.TryGetValue(entityKey, out ProjectEntry? entry) ||
                entry is null ||
                entry.Kind != ProjectEntryKind.MeshPca ||
                entry.Payload.PcaExtra is null ||
                !project.TryGetReference(entry.Payload.PcaExtra.DataKey, out MatrixCollection? pca) ||
                pca is null)
            {
                throw new InvalidOperationException();
            }

            pcaEntry = entry;
            pcaData = pca;

            if (!project.TryGetReference(pcaEntry.Payload.PcaExtra.TemplateKey, out Mesh? baseMesh) || baseMesh is null)
                throw new InvalidOperationException();

            Pca? pcaObj = Pca.FromMatrixCollection(pca);
            if(pcaObj is null)
                throw new InvalidOperationException();

            pcaObject = pcaObj;
            meanMesh = baseMesh;
            tempSoa = new float[pcaObj.Dimension];
            tempAos = new byte[pcaObj.Dimension * 4];

            Groupings.Add(PcaScatterGrouping.None);
            GatherGroupingsFromEntry(pcaEntityKey);

            sidebar = new PcaSynthMeshSideBar(this);
        }

        Project project;
        ProjectEntry pcaEntry;
        Pca pcaObject;
        long entityKey;
        PcaSynthMeshSideBar sidebar;
        MatrixCollection pcaData;
        Mesh meanMesh;
        int mappedFieldIndex = 0;
        int indexPcScatterX = 0, indexPcScatterY = 1;
        float[] tempSoa;
        byte[] tempAos;

        static readonly List<string> mappedFieldsList = new List<string>
        {
            "None", "PC effect"
        };

        public int MappedFieldIndex
        {
            get { return mappedFieldIndex; }
            set { mappedFieldIndex = value; UpdateMappedField(); OnPropertyChanged("MappedFieldIndex"); }
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
            MeshUtils.CopySoaToAos(MemoryMarshal.Cast<byte, Vector3>(tempAos.AsSpan()), 
                MemoryMarshal.Cast<float, byte>(tempSoa.AsSpan()));
            meshRend.UpdateData(tempAos, MeshSegmentType.Position);
            UpdateViewer();
        }

        public override void AttachRenderer(WpfInteropRenderer renderer)
        {
            base.AttachRenderer(renderer);
            ShowMesh();
            UpdateMappedField();
        }

        protected override void UpdateRendererConfig()
        {
            base.UpdateRendererConfig();
        }

        private void GatherGroupingsFromEntry(long key)
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
        }

        private void ShowMesh()
        {
            meshRend.UseDynamicArrays = true;
            meshRend.Mesh = MeshNormals.MakeNormals(meanMesh);
            UpdateRendererConfig();
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

        private void UpdateMappedField()
        {
            float[]? data = mappedFieldIndex switch
            {
                _ => null
            };

            if (data is null)
            {
                RenderLut = false;
            }
            else
            {
                RenderLut = true;
                meshRend.SetValueField(data);
                sidebar.SetHist(data, meshRend.Lut ?? Lut.Create(256, Lut.ViridisColors), valueMin, valueMax);
            }
        }
    }
}
