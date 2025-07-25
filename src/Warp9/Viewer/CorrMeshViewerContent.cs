﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using Warp9.Controls;
using Warp9.Data;
using Warp9.Model;
using Warp9.Processing;
using Warp9.Scene;

namespace Warp9.Viewer
{
    public class CorrMeshViewerContent : SceneViewerContentBase
    {
        public CorrMeshViewerContent(Project proj, long dcaEntityKey, string name) :
            base(proj, name)
        {
            Scene.Mesh0 = new MeshSceneElement();

            entityKey = dcaEntityKey;

            if (!proj.Entries.TryGetValue(entityKey, out ProjectEntry? entry) ||
                entry.Kind != ProjectEntryKind.MeshCorrespondence)
                throw new InvalidOperationException();

            dcaEntry = entry;            
            sidebar = new CorrMeshSideBar(this);
        }

      
        ProjectEntry dcaEntry;
        Page sidebar;
        long entityKey;

        int meshIndex = 0;


        public int MeshIndex
        {
            get { return meshIndex; }
            set { ShowMesh(value); Scene.Mesh0!.Version.Commit(RenderItemDelta.Full); OnPropertyChanged("MeshIndex"); }
        }

        public bool RenderWireframe
        {
            get { return Scene.Mesh0!.Flags.HasFlag(MeshRenderFlags.Wireframe); }
            set { SetMeshRendFlag(Scene.Mesh0!, MeshRenderFlags.Wireframe, value); OnPropertyChanged("RenderWireframe"); }
        }

        public bool RenderFill
        {
            get { return Scene.Mesh0!.Flags.HasFlag(MeshRenderFlags.Fill); }
            set { SetMeshRendFlag(Scene.Mesh0!, MeshRenderFlags.Fill, value); OnPropertyChanged("RenderFill"); }
        }

        public bool RenderSmoothNormals
        {
            get { return Scene.Mesh0!.Flags.HasFlag(MeshRenderFlags.EstimateNormals); }
            set { SetMeshRendFlag(Scene.Mesh0!, MeshRenderFlags.EstimateNormals, value); OnPropertyChanged("RenderSmoothNormals"); }
        }

        public override string DescribeScene()
        {
            return string.Format("Correspondence mesh #{0}", meshIndex);
        }

        public override void AttachRenderer(WpfInteropRenderer renderer)
        {
            ShowMesh(0);
            base.AttachRenderer(renderer);
        }

        public override Page? GetSidebar()
        {
            return sidebar;
        }


        private void ShowMesh(int index)
        {
            meshIndex = index;
            SpecimenTable tab = dcaEntry.Payload.Table!;
            long corrPclRef = tab.Columns[ModelConstants.CorrespondencePclColumnName].GetData<ProjectReferenceLink>()[index].ReferenceIndex;

            //int baseIndex = dcaEntry.Payload.MeshCorrExtra.DcaConfig.BaseMeshIndex;
            SpecimenTable mainSpecTable = project.Entries[dcaEntry.Payload.MeshCorrExtra.DcaConfig.SpecimenTableKey].Payload.Table;
            //long baseMeshRef = mainSpecTable.Columns[dcaEntry.Payload.MeshCorrExtra.DcaConfig.MeshColumnName].GetData<ProjectReferenceLink>()[baseIndex].ReferenceIndex;
            long baseMeshRef = dcaEntry.Payload.MeshCorrExtra.BaseMeshCorrKey;

            if (!project.TryGetReference(corrPclRef, out PointCloud? corrPcl) || corrPcl is null)
                throw new InvalidOperationException();

            if (!project.TryGetReference(baseMeshRef, out Mesh? baseMesh) || baseMesh is null)
                throw new InvalidOperationException();

            if (corrPcl.VertexCount != baseMesh.VertexCount)
                throw new InvalidOperationException("Vertex count");

            Mesh corrMesh = MeshNormals.MakeNormals(Mesh.FromPointCloud(corrPcl, baseMesh));
            Scene.Mesh0!.Mesh = new ReferencedData<Mesh>(corrMesh);
        }
    }
}
