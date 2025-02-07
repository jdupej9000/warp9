using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Warp9.Data;
using Warp9.Utils;

namespace Warp9.Viewer
{
    class HudSubText
    {
    }

    public class RenderItemHud : RenderItemBase
    {
        public RenderItemHud(FontDefinition font)
        {
            Font = font;
        }

        FontDefinition Font { get; init; }

        Dictionary<int, HudSubText> subTexts = new Dictionary<int, HudSubText>();
        bool subTextsDirty = false;

        public void SetSubText(int key, string text, float size = 12.0f, Color color = default, RectangleF rect = default, bool relativePos = false, TextRenderFlags flags = TextRenderFlags.AlignLeft)
        {
            if (!subTexts.ContainsKey(key))
                subTextsDirty = true;
        }

        public void RemoveSubText(int key)
        {
            subTexts.Remove(key);
            subTextsDirty = true;
        }

        public void ClearSubTexts()
        {
            subTexts.Clear();
            subTextsDirty = true;
        }

        protected override bool UpdateJobInternal(RenderJob job, DeviceContext ctx)
        {
            return false;
        }

        public override void UpdateConstantBuffers(RenderJob job)
        {
            base.UpdateConstantBuffers(job);
        }
    }
}
