using SharpDX.Direct3D11;
using System;
using System.Drawing;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using Warp9.Data;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;

namespace Warp9.Viewer
{
    [Flags]
    public enum MeshRenderStyle : uint
    {
        ColorFlat = StockShaders.PshConst_Flags_ColorFlat,
        ColorArray = StockShaders.PshConst_Flags_ColorArray,
        ColorTex = StockShaders.PshConst_Flags_ColorTex,
        ColorLut = StockShaders.PshConst_Flags_ColorScale,

        PhongBlinn = StockShaders.PshConst_Flags_PhongBlinn,
        DiffuseLighting = StockShaders.PshConst_Flags_DiffuseLighting,

        EstimateNormals = StockShaders.PshConst_Flags_EstimateNormals,

        ShowValueLevel = StockShaders.PshConst_Flags_ValueLevel
    };

    public class RenderItemMesh : RenderItemBase
    {
        public RenderItemMesh()
        {
        }

        Mesh? mesh;
        Lut? lut;
        bool useDynamicArrays = false;
        float[]? valueBuffer;
        ReadOnlyMemory<byte> posUpdateDyn;
        float levelValue, valueMin = 0, valueMax = 1;
        Color fillColor, pointWireColor;
        Matrix4x4 modelMatrix = Matrix4x4.Identity;
        MeshRenderStyle style;
        bool constBuffDirty = true;

        bool renderPoints = false, renderWire = false, renderFace = true, renderCull = false, renderDepth = true, renderBlend = false;

        public bool RenderPoints
        {
            get { return renderPoints; }
            set { renderPoints = value; constBuffDirty = true; ; }
        }

        public bool RenderWireframe
        {
            get { return renderWire; }
            set { renderWire = value; constBuffDirty = true; ; }
        }

        public bool RenderFace
        {
            get { return renderFace; }
            set { renderFace = value; constBuffDirty = true; ; }
        }

        public bool RenderCull
        {
            get { return renderCull; }
            set { renderCull = value; constBuffDirty = true; ; }
        }

        public bool RenderDepth
        {
            get { return renderDepth; }
            set { renderDepth = value; constBuffDirty = true; ; }
        }

        public bool RenderBlend
        {
            get { return renderDepth; }
            set { renderDepth = value; constBuffDirty = true; ; }
        }

        public float ValueMin
        {
            get { return valueMin; }
            set { valueMin = value; constBuffDirty = true; }
        }

        public float ValueMax
        {
            get { return valueMax; }
            set { valueMax = value; constBuffDirty = true; }
        }

        public bool UseDynamicArrays
        {
            get { return useDynamicArrays; }
            set { useDynamicArrays = value; useDynamicArrays = true; }
        }

        public Mesh? Mesh
        {
            get { return mesh; }
            set { mesh = value; Commit(); }
        }

        public Lut? Lut
        {
            get { return lut; }
            set { lut = value; Commit(); }
        }

        public float LevelValue
        {
            get { return levelValue; }
            set { levelValue = value; constBuffDirty = true; }
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

        public Color PointWireColor
        {
            get { return pointWireColor; }
            set { pointWireColor = value; constBuffDirty = true; }
        }

        public Matrix4x4 ModelMatrix
        {
            get { return modelMatrix; }
            set { modelMatrix = value; constBuffDirty = true; }
        }

        public void SetValueField(float[] val)
        {
            valueBuffer = val;
            Commit();
        }

        public void UpdatePosData(ReadOnlyMemory<byte> newPos)
        {
            posUpdateDyn = newPos;
            Commit(RenderJobInvalidation.DynamicData);
        }

        protected override void PartialUpdateJobInternal(RenderJobInvalidation kind, RenderJob job, DeviceContext ctx)
        {
            if (kind == RenderJobInvalidation.DynamicData)
            {
                // TODO: lock this
                if (!posUpdateDyn.IsEmpty)
                {
                    job.TryUpdateDynamicVertexBuffer(ctx, 0, posUpdateDyn.Span);
                }
            }
        }

        protected override bool UpdateJobInternal(RenderJob job, DeviceContext ctx)
        {
            if (mesh is null)
            {
                SetError("Mesh is null");
                return true;
            }

            job.SetShader(ctx, ShaderType.Vertex, "VsDefault");
            job.SetShader(ctx, ShaderType.Pixel, "PsDefault");
                       
            MeshView? posView = mesh.GetView(MeshViewKind.Pos3f);
            if (posView is null)
            {
                SetError("Mesh has no vertex position view.");
                return true;
            }

            if(posUpdateDyn.IsEmpty)
                job.SetVertexBuffer(ctx, 0, posView.RawData, posView.GetLayout(), useDynamicArrays);
            else
                job.SetVertexBuffer(ctx, 0, posUpdateDyn.Span, posView.GetLayout(), useDynamicArrays);

            MeshView? normalView = mesh.GetView(MeshViewKind.Normal3f);
            if (normalView is not null)
                job.SetVertexBuffer(ctx, 2, normalView.RawData, normalView.GetLayout(), false);

            if (valueBuffer is not null)
            {
                VertexDataLayout layoutValue = new VertexDataLayout();
                layoutValue.AddTex(SharpDX.DXGI.Format.R32_Float, 1, 0);
                job.SetVertexBuffer(ctx, 1, MemoryMarshal.Cast<float, byte>(valueBuffer.AsSpan()), layoutValue);
            }

            DrawCall dcFace, dcWire;
            if (mesh.IsIndexed)
            { 
                if (!mesh.TryGetIndexData(out ReadOnlySpan<FaceIndices> idxData))
                {
                    SetError("Mesh is indexed but has no index view.");
                    return true;
                }

                job.SetIndexBuffer(ctx, MemoryMarshal.Cast<FaceIndices, byte>(idxData), SharpDX.DXGI.Format.R32_UInt);
                dcFace = job.SetDrawCall(0, true, SharpDX.Direct3D.PrimitiveTopology.TriangleList,
                    0, mesh.FaceCount * 3);
                dcWire = job.SetDrawCall(1, true, SharpDX.Direct3D.PrimitiveTopology.TriangleList,
                    0, mesh.FaceCount * 3);
            }
            else
            {
                dcFace = job.SetDrawCall(0, false, SharpDX.Direct3D.PrimitiveTopology.TriangleList,
                    0, mesh.VertexCount);
                dcWire = job.SetDrawCall(1, false, SharpDX.Direct3D.PrimitiveTopology.TriangleList,
                    0, mesh.VertexCount);
            }

            DrawCall dcPoints = job.SetDrawCall(2, false, SharpDX.Direct3D.PrimitiveTopology.PointList,
                0, mesh.VertexCount);

            UpdateDrawCallSettings(job);

            if (lut is not null)
                job.SetTexture(ctx, 1, lut, true);

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
                    valueLevel = levelValue,
                    flags = (uint)style,
                    ambStrength = 0.1f,
                    valueMin = valueMin,
                    valueScale = 1.0f / (valueMax - valueMin)
                };
                job.TrySetConstBuffer(0, StockShaders.Name_PshConst, pshConst);

                PshConst pshConstWirePoint = new PshConst
                {
                    color = RenderUtils.ToNumColor(pointWireColor),
                    valueLevel = levelValue,
                    flags = (uint)(MeshRenderStyle.ColorFlat),
                    ambStrength = 0.1f
                };
                job.TrySetConstBuffer(1, StockShaders.Name_PshConst, pshConstWirePoint);
                job.TrySetConstBuffer(2, StockShaders.Name_PshConst, pshConstWirePoint);

                ModelConst mc = new ModelConst();
                mc.model = Matrix4x4.Transpose(modelMatrix);
             
                job.TrySetConstBuffer(-1, StockShaders.Name_ModelConst, mc);

                UpdateDrawCallSettings(job);
                constBuffDirty = false;
            }
        }

        private void UpdateDrawCallSettings(RenderJob job)
        {
            job.TryEnableDrawCall(0, renderFace);
            job.TryEnableDrawCall(1, renderWire);
            job.TryEnableDrawCall(2, renderPoints);

            if (job.TryGetDrawCall(0, out DrawCall? dcFace) && dcFace is not null)
            {
                dcFace.RastMode = RasterizerMode.Solid;
                if (renderCull) dcFace.RastMode |= RasterizerMode.CullBack;

                dcFace.DepthMode = renderDepth ? DepthMode.UseDepth : DepthMode.NoDepth;
                dcFace.BlendMode = renderBlend ? BlendMode.AlphaBlend : BlendMode.Default;
            }

            if (job.TryGetDrawCall(1, out DrawCall? dcWire) && dcWire is not null)
            {
                dcWire.RastMode = RasterizerMode.Wireframe;
                if (renderCull) dcWire.RastMode |= RasterizerMode.CullBack;

                dcWire.DepthMode = renderDepth ? DepthMode.UseDepth : DepthMode.NoDepth;
                dcWire.BlendMode = renderBlend ? BlendMode.AlphaBlend : BlendMode.Default;
            }

            if (job.TryGetDrawCall(2, out DrawCall? dcPoint) && dcPoint is not null)
            {
                dcPoint.RastMode = RasterizerMode.Solid;
                dcPoint.DepthMode = renderDepth ? DepthMode.UseDepth : DepthMode.NoDepth;
                dcPoint.BlendMode = renderBlend ? BlendMode.AlphaBlend : BlendMode.Default;
            }
        }

        private void SetError(string err)
        {
            // TODO: disable all drawcalls
        }
    }
}
