using Microsoft.VisualBasic;
using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Warp9.Viewer
{
    public class RenderItemGrid : RenderItemBase
    {
        public RenderItemGrid()
        {
            Commit();
        }

        Color zeroXAxisColor = Color.GreenYellow;
        Color zeroYAxisColor = Color.OrangeRed;
        Color majorLineColor = Color.FromArgb(80, 80, 80);
        Color minorLineColor = Color.FromArgb(64, 64, 64);
        Vector2 minorIncrement = new Vector2(0.5f, 0.5f);
        int majorIncrementMul = 5;
        Vector2 spanXYMin = new Vector2(-5, -5);
        Vector2 spanXYMax = new Vector2(5, 5);

        bool visible = true;

        public bool Visible
        {
            get { return visible; }
            set { visible = value; Commit() ; }
        }

        protected override bool UpdateJobInternal(RenderJob job, DeviceContext ctx)
        {
            job.SetShader(ctx, ShaderType.Vertex, "VsDefault");
            job.SetShader(ctx, ShaderType.Pixel, "PsDefault");

            int numVertices = CreateGridVertBuffer(out byte[] vb, out VertexDataLayout vbLayout);
            job.SetVertexBuffer(ctx, 0, vb.AsSpan(), vbLayout, false);

            DrawCall dcGrid = job.SetDrawCall(0, false, SharpDX.Direct3D.PrimitiveTopology.LineList, 0, numVertices);
            dcGrid.RastMode = RasterizerMode.Wireframe;
            dcGrid.BlendMode = BlendMode.Default;
            dcGrid.Enabled = visible; // TODO: Do this without recreating the buffers

            return true;
        }

        public override void UpdateConstantBuffers(RenderJob job)
        {
            base.UpdateConstantBuffers(job);

            ModelConst mc = new ModelConst();
            mc.model = Matrix4x4.Identity;
            job.TrySetConstBuffer(-1, StockShaders.Name_ModelConst, mc);

            PshConst pshConst = new PshConst
            {
                flags = (uint)MeshRenderStyle.ColorArray,
                ambStrength = 0.1f
            };
            job.TrySetConstBuffer(-1, StockShaders.Name_PshConst, pshConst);
        }

        [StructLayout(LayoutKind.Sequential)]
        struct Vertex
        {
            public Vertex(Vector3 pos, Color col)
            {
                Pos = pos;
                Col = RenderUtils.ToNumColor(col);
            }

            public Vector3 Pos;
            public Vector4 Col;
        }

        private int CreateGridVertBuffer(out byte[] vb, out VertexDataLayout vbLayout)
        {
            List<Vertex> vertices = new List<Vertex>();

            ApplyGridlines(spanXYMin.X, spanXYMax.X, minorIncrement.X, majorIncrementMul,
                (x, kind) =>
                {
                    Vector3 p0 = new Vector3(x, spanXYMin.Y, 0);
                    Vector3 p1 = new Vector3(x, spanXYMax.Y, 0);
                    Color col = kind switch
                    {
                        0 => minorLineColor,
                        1 => majorLineColor,
                        2 => zeroXAxisColor,
                        _ => throw new InvalidOperationException()
                    };
                    vertices.Add(new Vertex(p0, col));
                    vertices.Add(new Vertex(p1, col));
                });

            ApplyGridlines(spanXYMin.Y, spanXYMax.Y, minorIncrement.Y, majorIncrementMul,
               (y, kind) =>
               {
                   Vector3 p0 = new Vector3(spanXYMin.X, y, 0);
                   Vector3 p1 = new Vector3(spanXYMax.X, y, 0);
                   Color col = kind switch
                   {
                       0 => minorLineColor,
                       1 => majorLineColor,
                       2 => zeroYAxisColor,
                       _ => throw new InvalidOperationException()
                   };
                   vertices.Add(new Vertex(p0, col));
                   vertices.Add(new Vertex(p1, col));
               });

            vertices.Add(new Vertex(new Vector3(0, spanXYMin.Y, 0), zeroXAxisColor));
            vertices.Add(new Vertex(new Vector3(0, spanXYMax.Y, 0), zeroXAxisColor));

            vertices.Add(new Vertex(new Vector3(spanXYMin.X, 0, 0), zeroYAxisColor));
            vertices.Add(new Vertex(new Vector3(spanXYMax.X, 0, 0), zeroYAxisColor));

            int num = vertices.Count;
            vb = new byte[num * Marshal.SizeOf<Vertex>()];
            Span<Vertex> verticesOut = MemoryMarshal.Cast<byte, Vertex>(vb.AsSpan());
            vertices.CopyTo(verticesOut);

            vbLayout = new VertexDataLayout();
            vbLayout.AddPosition(SharpDX.DXGI.Format.R32G32B32_Float, 0);
            vbLayout.AddColor(SharpDX.DXGI.Format.R32G32B32A32_Float, 0, 12);

            return num;
        }

        private void ApplyGridlines(float x0, float x1, float inc, int maj, Action<float, int> fun)
        {
            int ix0 = (int)(x0 / inc);
            int ix1 = (int)(x1 / inc);

            for (int i = ix0; i <= ix1; i++)
            {
                int kind = 0;
                if (i == 0) continue;
                else if (i % maj == 0) kind = 1;

                fun(i * inc, kind);
            }
        }
    }
}
