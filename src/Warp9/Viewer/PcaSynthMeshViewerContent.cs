﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using Warp9.Controls;
using Warp9.Data;
using Warp9.Model;

namespace Warp9.Viewer
{
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

            sidebar = new PcaSynthMeshSideBar(this);
        }

        Project project;
        ProjectEntry pcaEntry;
        long entityKey;
        PcaSynthMeshSideBar sidebar;
        MatrixCollection pcaData;
        int indexPcScatterX = 0, indexPcScatterY = 1;

        public List<string> PrincipalComponents => CreatePrincipalComponentList().ToList();
        public List<string> Groupings { get; } = new List<string>() { "(nothing)" };
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

        public override void AttachRenderer(WpfInteropRenderer renderer)
        {
            base.AttachRenderer(renderer);
        }

        protected override void UpdateRendererConfig()
        {
            base.UpdateRendererConfig();
        }

        private void UpdateScatter()
        {
            if (pcaData.TryGetMatrix(Native.Pca.KeyScores, out Matrix<float>? pcaScores) &&
              pcaScores is not null &&
              indexPcScatterX >=0 && indexPcScatterX < pcaScores.Columns &&
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
    }
}
