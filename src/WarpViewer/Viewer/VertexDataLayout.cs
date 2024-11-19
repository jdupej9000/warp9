using SharpDX.Direct3D11;
using System.Collections.Generic;
using System.Linq;

namespace Warp9.Viewer
{
    public class VertexDataLayout
    {
        public VertexDataLayout(bool isInstance = false)
        {
            inputCls = isInstance ? InputClassification.PerInstanceData : InputClassification.PerVertexData;
            stepRate = isInstance ? 1 : 0;
        }

        List<InputElement> inputElements = new List<InputElement>();
        InputClassification inputCls;
        int stepRate;
        
        public VertexDataLayout AddPosition(SharpDX.DXGI.Format fmt, int byteOffset)
        {
            inputElements.Add(new InputElement("POSITION", 0, fmt, byteOffset, 0, inputCls, stepRate));
            return this;
        }

        public VertexDataLayout AddNormal(SharpDX.DXGI.Format fmt, int byteOffset)
        {
            inputElements.Add(new InputElement("NORMAL", 0, fmt, byteOffset, 0, inputCls, stepRate));
            return this;
        }

        public VertexDataLayout AddTex(SharpDX.DXGI.Format fmt, int index, int byteOffset)
        {
            inputElements.Add(new InputElement("TEXCOORD", index, fmt, byteOffset, 0, inputCls, stepRate));
            return this;
        }

        public VertexDataLayout AddColor(SharpDX.DXGI.Format fmt, int index, int byteOffset)
        {
            inputElements.Add(new InputElement("COLOR", index, fmt, byteOffset, 0, inputCls, stepRate));
            return this;
        }

        public InputElement[] ToArray() => inputElements.ToArray();

        public int StrideBytes => inputElements.Sum((t) => RenderUtils.GetStructSizeBytes(t.Format));


        public void AddToGrandLayout(IList<InputElement> ret, int slot)
        {
            for (int i = 0; i < inputElements.Count; i++)
            {
                InputElement ie = inputElements[i];
                ie.Slot = slot;
                ret.Add(ie);
            }
        }
    }
}
