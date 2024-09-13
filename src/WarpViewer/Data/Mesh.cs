using SharpDX.D3DCompiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Warp9.Data
{
   

    public class Mesh
    {
        internal Mesh()
        {
        }

        Dictionary<MeshSegmentType, MeshSegment> meshSegments = new Dictionary<MeshSegmentType, MeshSegment>();
        byte[] vertexData, indexData;

        public int VertexCount { get; private init; }
        public int FaceCount {get; private init; }
        public bool IsIndexed { get; private init; }

        public bool TryGetRawData(MeshSegmentType kind, int coord, out ReadOnlySpan<byte> data, out int itemSize)
        {
            if (meshSegments.TryGetValue(kind, out MeshSegment? seg))
            {
                if (coord == AllCoords)
                {
                    data = new ReadOnlySpan<byte>(vertexData,
                        seg.Offset,
                        VertexCount * seg.ItemLength * seg.NumCoords);
                    itemSize = seg.ItemLength;
                }
                else if (coord >= 0 && coord < seg.NumCoords)
                {
                    data = new ReadOnlySpan<byte>(vertexData,
                        seg.Offset + coord * VertexCount * seg.ItemLength,
                        VertexCount * seg.ItemLength);
                    itemSize = seg.ItemLength;
                }
            }

            data = new ReadOnlySpan<byte>();
            itemSize = 0;
            return false;
        }

        public const int AllCoords = -1;
    }
}
