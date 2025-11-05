using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Linq.Expressions;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Warp9.Data;
using Warp9.Utils;

namespace Warp9.Viewer
{
    class HudSubText
    {
        public RectangleF Rectangle;
        public float Size;
        public TextRenderFlags Flags;
        public Color Color;
        public required string Text;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct HudCharacterInstanceData
    {
        public Vector2 ScreenPos, ScreenSize;
        public Vector2 TexPos, TexSize;
        public uint Color;
    }

    public class RenderItemHud : RenderItemBase
    {
        public RenderItemHud(FontDefinition font)
        {
            Font = font;

            instanceLayout = new VertexDataLayout(true);
            instanceLayout.AddTex(MeshSegmentFormat.Float32x4, 6, 0);
            instanceLayout.AddTex(MeshSegmentFormat.Float32x4, 7, 16);
            instanceLayout.AddColor(MeshSegmentFormat.Int8x4, 0, 32);
        }

        static float[] VertexBuffer = {
            0, 0,
            0, 1,
            1, 1,

            0, 0,
            1, 1,
            1, 0
        };

        FontDefinition Font { get; init; }

        VertexDataLayout instanceLayout;
        Dictionary<int, HudSubText> subTexts = new Dictionary<int, HudSubText>();
        bool inited = false;
      

        public void SetSubText(int key, string text, float size = 12.0f, Color color = default, RectangleF rect = default, bool relativePos = false, TextRenderFlags flags = TextRenderFlags.AlignLeft)
        {
            subTexts[key] = new HudSubText()
            {
                Rectangle = rect,
                Size = size,
                Color = color,
                Flags = flags,
                Text = text
            };

            Version.Commit(RenderItemDelta.Full);
        }

        public void RemoveSubText(int key)
        {
            subTexts.Remove(key);
            Version.Commit(RenderItemDelta.Full);
        }

        public void ClearSubTexts()
        {
            subTexts.Clear();
            Version.Commit(RenderItemDelta.Full);
        }

        protected override bool UpdateJobInternal(RenderJob job, DeviceContext ctx)
        {
            // TODO: replace with text rendering shaders
            job.SetShader(ctx, ShaderType.Vertex, "VsText");
            job.SetShader(ctx, ShaderType.Pixel, "PsText");

            if (!inited)
            {
                VertexDataLayout layout = new VertexDataLayout();
                layout.AddPosition(MeshSegmentFormat.Float32x2, 0);
                job.SetVertexBuffer(ctx, 0, MemoryMarshal.Cast<float, byte>(VertexBuffer.AsSpan()), layout, false);

                job.SetTexture(ctx, 0, Font.Bitmap, false);                
                inited = true;
            }

            List<HudCharacterInstanceData> instanceData = new List<HudCharacterInstanceData>();
            foreach (var kvp in subTexts)
            {
                 HudSubText hst = kvp.Value;
                 uint color = (uint)hst.Color.ToArgb();
                 TextBufferGenerator.Generate(Font, hst.Size, hst.Text, hst.Rectangle, hst.Flags, (cri) =>
                 {
                     HudCharacterInstanceData hcid = new HudCharacterInstanceData()
                     {
                         ScreenPos = cri.Pos,
                         ScreenSize = cri.Size,
                         TexPos = cri.TexPos,
                         TexSize = cri.TexSize,
                         Color = color
                     };
                     instanceData.Add(hcid);
                 });
            }

            /*instanceData.Add(new HudCharacterInstanceData()
            {
                Color = 0xffffffffu,
                ScreenPos = new Vector2(16, 16),
                ScreenSize = new Vector2(16, 16),
                TexPos = new Vector2(0, 0),
                TexSize = new Vector2(1, 1)
            });*/

            ReadOnlySpan<byte> instanceRaw = MemoryMarshal.Cast<HudCharacterInstanceData, byte>(CollectionsMarshal.AsSpan(instanceData));
            job.SetVertexBuffer(ctx, 1, instanceRaw, instanceLayout, true);

            int numInst = instanceData.Count;
            int numVert = 6;

            DrawCall dc = job.SetInstancedDrawCall(0, false, SharpDX.Direct3D.PrimitiveTopology.TriangleList, 0, numInst, 0, numVert);
            dc.RastMode = RasterizerMode.Solid | RasterizerMode.NoCull;
            dc.DepthMode = DepthMode.NoDepth;
            dc.BlendMode = BlendMode.AlphaBlend;

            return false;
        }

        public override void UpdateConstantBuffers(RenderJob job, IRendererViewport vport)
        {
            base.UpdateConstantBuffers(job, vport);

            ViewProjConst vpc = new ViewProjConst();
            vpc.camera = Vector4.UnitW;
            vpc.viewProj = Matrix4x4.Transpose(Matrix4x4.CreateOrthographicOffCenterLeftHanded(
                0, vport.ViewportSize.Width, vport.ViewportSize.Height, 0, 0.01f, 100.0f));
            job.TrySetConstBuffer(0, 1, vpc);
        }
    }
}
