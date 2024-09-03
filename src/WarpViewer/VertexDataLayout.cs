using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX;
using SharpDX.Direct3D11;
using Warp9.Viewer;

namespace Warp9.Viewer
{
    public class VertexDataLayout
    {
        List<InputElement> inputElements = new List<InputElement>();
        
        public VertexDataLayout AddPosition(SharpDX.DXGI.Format fmt, int byteOffset)
        {
            inputElements.Add(new InputElement("POSITION", 0, fmt, byteOffset, 0, InputClassification.PerVertexData, 0));
            return this;
        }

        public VertexDataLayout AddNormal(SharpDX.DXGI.Format fmt, int byteOffset)
        {
            inputElements.Add(new InputElement("NORMAL", 0, fmt, byteOffset, 0, InputClassification.PerVertexData, 0));
            return this;
        }

        public VertexDataLayout AddTex(SharpDX.DXGI.Format fmt, int slot, int byteOffset)
        {
            inputElements.Add(new InputElement("TEXCOORD", slot, fmt, byteOffset, 0, InputClassification.PerVertexData, 0));
            return this;
        }

        public VertexDataLayout AddColor(SharpDX.DXGI.Format fmt, int slot, int byteOffset)
        {
            inputElements.Add(new InputElement("COLOR", slot, fmt, byteOffset, 0, InputClassification.PerVertexData, 0));
            return this;
        }

        public InputElement[] ToArray() => inputElements.ToArray();

        public int StrideBytes => inputElements.Sum((t) => RenderUtils.GetStructSizeBytes(t.Format));

        public void AddToGrandLayout(IList<InputElement> inputElements, int slot)
        {
            for (int i = 0; i < inputElements.Count; i++)
            {
                InputElement ie = inputElements[i];
                ie.Slot = slot;
                inputElements.Add(ie);
            }
        }
    }
}
