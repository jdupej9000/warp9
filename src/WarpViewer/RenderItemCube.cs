using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        private CubeRenderStyle style = CubeRenderStyle.FlatColor;
        private Color color = Color.Green;
        private bool buffDirty = true;

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


        protected override bool UpdateJobInternal(RenderJob job, DeviceContext ctx)
        {
            VertexDataLayout layout = new VertexDataLayout();
            layout.AddPosition(SharpDX.DXGI.Format.R32G32B32_Float, 0);
            layout.AddColor(SharpDX.DXGI.Format.R32G32B32A32_Float, 0, 12);
            job.SetVertexBuffer(ctx, 0, RenderUtils.ToByteArray<float>(VertexBuffer), layout);
            job.SetIndexBuffer(ctx, RenderUtils.ToByteArray<int>(IndexBuffer), SharpDX.DXGI.Format.R32_UInt);
            job.SetShader(ctx, ShaderType.Vertex, "VsDefault");
            job.SetShader(ctx, ShaderType.Pixel, "PsDefault");
            job.SetDrawCall(0, true, SharpDX.Direct3D.PrimitiveTopology.TriangleList, 0, IndexBuffer.Length, 0);
            return true;
        }

        public override void UpdateConstantBuffers(RenderJob job)
        {
            base.UpdateConstantBuffers(job);

            if (buffDirty)
            {
                PshConst pshConst = new PshConst();

                if (style == CubeRenderStyle.FlatColor)
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

                buffDirty = false;
            }
        }

    }
}
