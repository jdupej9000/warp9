using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Warp9.Viewer
{
    public enum CubeRenderStyle
    {
        FlatColor,
        ColorArray
    };

    public class RenderItemCube : RenderItemBase
    {
        public RenderItemCube()
        {
            Commit();
        }

        static float[] InstanceBuffer = {
            -0.5f, -0.5f, 0.0f,
            0.5f, -0.5f, 0.0f,
            -0.5f, 0.5f, 0.0f,
            0.5f, 0.5f, 0.0f
        };

        static float[] VertexBuffer = {
            1.0f, -1.0f, -1.0f, 0, 0, 0, 1,
            1.0f, -1.0f, 1.0f,  0, 0, 1, 1,
            -1.0f, -1.0f, 1.0f, 0, 1, 0, 1,
            -1.0f, -1.0f, -1.0f,0, 1, 1, 1,
            1.0f, 1.0f, -1.0f,  1, 0, 0, 1,
            1.0f, 1.0f, 1.0f,   1, 0, 1, 1,
            -1.0f, 1.0f, 1.0f,  1, 1, 0, 1,
            -1.0f, 1.0f, -1.0f, 1, 1, 1, 1
        };

        static int[] IndexBuffer = {
            1,2,3,7,6,5,4,5,1,5,6,2,2,6,7,0,3,7,0,1,3,4,7,5,0,4,1,1,5,2,3,2,7,4,0,7
        };

        static float[] VertexBufferTriSoup = {
            1,-1,1,0,-1,0,
            -1,-1,1,0,-1,0,
            -1,-1,-1,0,-1,0,
            -1,1,-1,0,1,0,
            -1,1,1,0,1,0,
            1,1,1,0,1,0,
            1,1,-1,1,0,0,
            1,1,1,1,0,0,
            1,-1,1,1,0,0,
            1,1,1,0,0,1,
            -1,1,1,0,0,1,
            -1,-1,1,0,0,1,
            -1,-1,1,-1,0,0,
            -1,1,1,-1,0,0,
            -1,1,-1,-1,0,0,
            1,-1,-1,0,0,-1,
            -1,-1,-1,0,0,-1,
            -1,1,-1,0,0,-1,
            1,-1,-1,0,-1,0,
            1,-1,1,0,-1,0,
            -1,-1,-1,0,-1,0,
            1,1,-1,0,1,0,
            -1,1,-1,0,1,0,
            1,1,1,0,1,0,
            1,-1,-1,1,0,0,
            1,1,-1,1,0,0,
            1,-1,1,1,0,0,
            1,-1,1,0,0,1,
            1,1,1,0,0,1,
            -1,-1,1,0,0,1,
            -1,-1,-1,-1,0,0,
            -1,-1,1,-1,0,0,
            -1,1,-1,-1,0,0,
            1,1,-1,0,0,-1,
            1,-1,-1,0,0,-1,
            -1,1,-1,0,0,-1
        };

        private CubeRenderStyle style = CubeRenderStyle.FlatColor;
        private Color color = Color.Green;
        private bool wireframe = false, triangleSoup = false, instances = false;
        private bool buffDirty = true;
        public bool TriangleSoup
        {
            get { return triangleSoup; }
            set { triangleSoup = value; Commit(); }
        }

        public CubeRenderStyle Style
        {
            get { return style; }
            set { style = value; buffDirty = true; }
        }

        public Color Color
        {
            get { return color; }
            set { color = value; buffDirty = true; }
        }

        public bool AddWireframe
        {
            get { return wireframe; }
            set { wireframe = value; buffDirty = true; }
        }

        public bool UseInstances 
        {
            get { return instances; }
            set { instances = value; Commit(); }
        }
    

        protected override bool UpdateJobInternal(RenderJob job, DeviceContext ctx)
        {
            job.SetShader(ctx, ShaderType.Vertex, instances ? "VsDefaultInstanced" : "VsDefault");
            job.SetShader(ctx, ShaderType.Pixel, "PsDefault");

            if (triangleSoup)
            {
                VertexDataLayout layout = new VertexDataLayout();
                layout.AddPosition(SharpDX.DXGI.Format.R32G32B32_Float, 0)
                    .AddNormal(SharpDX.DXGI.Format.R32G32B32_Float, 12);
                job.SetVertexBuffer(ctx, 0, RenderUtils.ToByteArray<float>(VertexBufferTriSoup), layout);
            }
            else
            {
                VertexDataLayout layout = new VertexDataLayout();
                layout.AddPosition(SharpDX.DXGI.Format.R32G32B32_Float, 0)
                    .AddColor(SharpDX.DXGI.Format.R32G32B32A32_Float, 0, 12);
                job.SetVertexBuffer(ctx, 0, RenderUtils.ToByteArray<float>(VertexBuffer), layout);
                job.SetIndexBuffer(ctx, RenderUtils.ToByteArray<int>(IndexBuffer), SharpDX.DXGI.Format.R32_UInt);

            }

            if (instances)
            {
                VertexDataLayout layoutInst = new VertexDataLayout(true);
                layoutInst.AddTex(SharpDX.DXGI.Format.R32G32B32_Float, 1, 0);
                job.SetVertexBuffer(ctx, 1, RenderUtils.ToByteArray<float>(InstanceBuffer), layoutInst);
            }

            DrawCall dcWire;
            int numInst = InstanceBuffer.Length / 3;

            if (triangleSoup)
            {
                int numVert = VertexBufferTriSoup.Length / 6;

                if (instances)
                {
                    job.SetInstancedDrawCall(0, false, SharpDX.Direct3D.PrimitiveTopology.TriangleList, 0, numInst, 0, numVert);
                    dcWire = job.SetInstancedDrawCall(1, false, SharpDX.Direct3D.PrimitiveTopology.TriangleList, 0, numInst, 0, numVert);
                }
                else
                {
                    job.SetDrawCall(0, false, SharpDX.Direct3D.PrimitiveTopology.TriangleList, 0, numVert);
                    dcWire = job.SetDrawCall(1, false, SharpDX.Direct3D.PrimitiveTopology.TriangleList, 0, numVert);
                }
            }
            else
            {
                if (instances)
                {
                    job.SetInstancedDrawCall(0, true, SharpDX.Direct3D.PrimitiveTopology.TriangleList, 0, numInst, 0, IndexBuffer.Length);
                    dcWire = job.SetInstancedDrawCall(1, true, SharpDX.Direct3D.PrimitiveTopology.TriangleList, 0, numInst, 0, IndexBuffer.Length);
                }
                else
                {
                    job.SetDrawCall(0, true, SharpDX.Direct3D.PrimitiveTopology.TriangleList, 0, IndexBuffer.Length, 0);
                    dcWire = job.SetDrawCall(1, true, SharpDX.Direct3D.PrimitiveTopology.TriangleList, 0, IndexBuffer.Length, 0);
                }
            }

            dcWire.RastMode = RasterizerMode.Wireframe | RasterizerMode.CullBack;
            dcWire.DepthMode = DepthMode.NoDepth;

            PshConst pshConstWire = new PshConst();
            pshConstWire.flags = StockShaders.PshConst_Flags_ColorFlat;
            pshConstWire.color = RenderUtils.ToNumColor(Color.White);
            job.SetConstBuffer(1, StockShaders.Name_PshConst, pshConstWire);

            buffDirty = true;

            return true;
        }

        public override void UpdateConstantBuffers(RenderJob job)
        {
            base.UpdateConstantBuffers(job);

            if (buffDirty)
            {
                PshConst pshConst = new PshConst();

                if (triangleSoup == true)
                {
                    pshConst.flags = StockShaders.PshConst_Flags_ColorFlat | StockShaders.PshConst_Flags_PhongBlinn;
                    pshConst.color = RenderUtils.ToNumColor(color);
                    pshConst.ambStrength = 0.1f;
                }
                else if (style == CubeRenderStyle.FlatColor)
                {
                    pshConst.flags = StockShaders.PshConst_Flags_ColorFlat;
                    pshConst.color = RenderUtils.ToNumColor(color);
                }
                else if(style == CubeRenderStyle.ColorArray)
                {
                    pshConst.flags = StockShaders.PshConst_Flags_ColorArray;
                    pshConst.color = RenderUtils.ToNumColor(color);
                }

                job.SetConstBuffer(0, StockShaders.Name_PshConst, pshConst);

                job.EnableDrawCall(1, wireframe);

                buffDirty = false;
            }
        }

    }
}
