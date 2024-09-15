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
        internal Mesh(int nv, int nt, byte[] vx, Dictionary<MeshSegmentType, MeshSegment> segs, byte[] ix, MeshSegment? idxSeg)
        {
            meshSegments = segs;
            vertexData = vx;
            indexData = Array.Empty<byte>();
            VertexCount = nv;
            FaceCount = nt;
            indexData = ix;
            indexSegment = idxSeg;
        }

        Dictionary<MeshSegmentType, MeshSegment> meshSegments = new Dictionary<MeshSegmentType, MeshSegment>();
        readonly MeshSegment? indexSegment;

        // Vertex data is formatted as structure of arrays. Say the contents are position of vec3 and 
        // texcoord0 of vec2. In that case the following are in vertexData back to back:
        // - all x coords of positions
        // - all y coords of positions
        // - all z coords of positions
        // - all u coords of texcoord0
        // - all v coords of texcoord1
        readonly byte[] vertexData;

        // IndexData, if present is a typical array of structures with triplets of integers grouped together,
        // that describe one face.
        readonly byte [] indexData;

        public int VertexCount { get; private init; }
        public int FaceCount { get; private init; }
        public bool IsIndexed => indexSegment is not null;

        public bool TryGetRawData(MeshSegmentType kind, int coord, out ReadOnlySpan<byte> data)
        {
            if (meshSegments.TryGetValue(kind, out MeshSegment? seg))
            {
                if (coord == AllCoords)
                {
                    data = new ReadOnlySpan<byte>(vertexData,
                        seg.Offset,
                        seg.TotalLength);
                    return true;
                }
                else if (coord >= 0 && coord < seg.StructElemCount)
                {
                    data = new ReadOnlySpan<byte>(vertexData,
                        seg.Offset + coord * seg.ChannelLength,
                       seg.ChannelLength);
                    return true;
                }
            }

            data = new ReadOnlySpan<byte>();
            return false;
        }

        public MeshBuilder ToBuilder()
        {
            MeshBuilder ret = new MeshBuilder(vertexData, meshSegments, indexData, indexSegment);
            return ret;
        }
 
        public const int AllCoords = -1;
    }
}
