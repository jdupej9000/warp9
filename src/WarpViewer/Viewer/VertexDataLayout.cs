using SharpDX.Direct3D11;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Warp9.Data;
using Warp9.Utils;

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

        internal VertexDataLayout Add(string elem, int elemIdx, MeshSegmentFormat fmt)
        {
            inputElements.Add(new InputElement(elem, elemIdx, MiscUtils.GetDxgiFormat(fmt), 0, 0, inputCls, stepRate));
            return this;
        }

        public VertexDataLayout AddPosition(MeshSegmentFormat fmt, int byteOffset)
        {
            inputElements.Add(new InputElement("POSITION", 0, MiscUtils.GetDxgiFormat(fmt), byteOffset, 0, inputCls, stepRate));
            return this;
        }

        public VertexDataLayout AddNormal(MeshSegmentFormat fmt, int byteOffset)
        {
            inputElements.Add(new InputElement("NORMAL", 0, MiscUtils.GetDxgiFormat(fmt), byteOffset, 0, inputCls, stepRate));
            return this;
        }

        public VertexDataLayout AddTex(MeshSegmentFormat fmt, int index, int byteOffset)
        {
            inputElements.Add(new InputElement("TEXCOORD", index, MiscUtils.GetDxgiFormat(fmt), byteOffset, 0, inputCls, stepRate));
            return this;
        }

        public VertexDataLayout AddColor(MeshSegmentFormat fmt, int index, int byteOffset)
        {
            inputElements.Add(new InputElement("COLOR", index, MiscUtils.GetDxgiFormat(fmt), byteOffset, 0, inputCls, stepRate));
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

    public class SimpleVertexLayoutProvider
    {
        private SimpleVertexLayoutProvider(string elem, int elemIndex, bool instance)
        {
            elementName = elem;
            elementIndex = elemIndex;
            isInstance = instance;
        }

        private string elementName;
        private int elementIndex;
        private bool isInstance;

        public VertexDataLayout Generate(MeshSegmentFormat fmt)
        {
            VertexDataLayout layout = new VertexDataLayout(isInstance);
            return layout.Add(elementName, elementIndex, fmt);
        }

        public static SimpleVertexLayoutProvider CreateTexCoord(int elemIndex, bool instance = false)
        {
            return new SimpleVertexLayoutProvider("TEXCOORD", elemIndex, instance);
        }
    }
}
