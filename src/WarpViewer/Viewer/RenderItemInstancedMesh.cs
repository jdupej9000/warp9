using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.DirectoryServices.ActiveDirectory;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Warp9.Data;

namespace Warp9.Viewer
{
    public class RenderItemInstancedMesh : RenderItemBase
    {
        public RenderItemInstancedMesh()
        {
            modelMatrix = Matrix4x4.Identity;
        }

        Mesh? mesh;
        PointCloud? instances;
        MeshRenderStyle style;
        Matrix4x4 modelMatrix;
        Color fillColor;
        bool renderDepth = true;
        bool constBuffDirty = false;

        public bool RenderDepth
        {
            get { return renderDepth; }
            set { renderDepth = value; constBuffDirty = true; ; }
        }

        public MeshRenderStyle Style
        {
            get { return style; }
            set { style = value; constBuffDirty = true; }
        }

        public Color FillColor
        {
            get { return fillColor; }
            set { fillColor = value; constBuffDirty = true; }
        }

        public Mesh? Mesh
        {
            get { return mesh; }
            set { mesh = value; Commit(); }
        }

        public PointCloud? Instances
        {
            get { return instances; }
            set { instances = value; Commit(); }
        }

        public Matrix4x4 BaseModelMatrix
        {
            get { return modelMatrix; }
            set { modelMatrix = value; constBuffDirty = true; }
        }

        protected override bool UpdateJobInternal(RenderJob job, DeviceContext ctx)
        {
            if (mesh is null)
            {
                SetError("Mesh is null");
                return true;
            }

            if (instances is null)
            {
                SetError("Instances are null");
                return true;
            }

            job.SetShader(ctx, ShaderType.Vertex, "VsDefaultInstanced");
            job.SetShader(ctx, ShaderType.Pixel, "PsDefault");

            MeshView? posView = mesh.GetView(MeshViewKind.Pos3f);
            if (posView is null)
            {
                SetError("Mesh has no vertex position view.");
                return true;
            }
            job.SetVertexBuffer(ctx, 0, posView.RawData, posView.GetLayout(), false);

            MeshView? normalView = mesh.GetView(MeshViewKind.Normal3f);
            if (normalView is not null)
                job.SetVertexBuffer(ctx, 2, normalView.RawData, normalView.GetLayout(), false);

            MeshView? instPosView = instances.GetView(MeshViewKind.Pos3f);
            if (instPosView is null)
            {
                SetError("Instances has no vertex position view.");
                return true;
            }
            VertexDataLayout layoutInst = new VertexDataLayout(true);
            layoutInst.AddTex(SharpDX.DXGI.Format.R32G32B32_Float, 7, 0);
            job.SetVertexBuffer(ctx, 1, instPosView.RawData, layoutInst);

            DrawCall dcMain;
            int numInst = instances.VertexCount;
            int numVert = mesh.VertexCount;
            if (mesh.IsIndexed)
            {
                MeshView? indexView = mesh.GetView(MeshViewKind.Indices3i);
                int numElems = mesh.FaceCount * 3;
                if (indexView is null)
                {
                    SetError("Mesh is indexed but has no index view.");
                    return true;
                }

                job.SetIndexBuffer(ctx, indexView.RawData, SharpDX.DXGI.Format.R32_UInt);

                dcMain = job.SetInstancedDrawCall(0, true, SharpDX.Direct3D.PrimitiveTopology.TriangleList, 0, numInst, 0, numElems);
            }
            else
            {
                dcMain = job.SetInstancedDrawCall(0, false, SharpDX.Direct3D.PrimitiveTopology.TriangleList, 0, numInst, 0, numVert);
            }

            UpdateDrawCallSettings(job);

            constBuffDirty = true;

            return true;
        }

        public override void UpdateConstantBuffers(RenderJob job)
        {
            base.UpdateConstantBuffers(job);

            if (constBuffDirty)
            {
                PshConst pshConst = new PshConst
                {
                    color = RenderUtils.ToNumColor(fillColor),
                    valueLevel = 0,
                    flags = (uint)style,
                    ambStrength = 0.1f
                };
                job.SetConstBuffer(0, StockShaders.Name_PshConst, pshConst);

                ModelConst mc = new ModelConst();
                mc.model = Matrix4x4.Transpose(modelMatrix);

                job.SetConstBuffer(-1, StockShaders.Name_ModelConst, mc);

                UpdateDrawCallSettings(job);

                constBuffDirty = false;
            }
        }

        private void UpdateDrawCallSettings(RenderJob job)
        {
            if (job.TryGetDrawCall(0, out DrawCall? dcFace) && dcFace is not null)
            {
                dcFace.RastMode = RasterizerMode.Solid;
                //if (renderCull) dcFace.RastMode |= RasterizerMode.CullBack;

                dcFace.DepthMode = renderDepth ? DepthMode.UseDepth : DepthMode.NoDepth;
                //dcFace.BlendMode = renderBlend ? BlendMode.AlphaBlend : BlendMode.Default;
            }
        }

        private void SetError(string err)
        {
            // TODO: disable all drawcalls
        }
    }
}
