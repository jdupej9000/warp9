using SharpDX.D3DCompiler;
using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
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

        readonly Dictionary<MeshViewKind, MeshView?> meshViews = new Dictionary<MeshViewKind, MeshView?>();
        readonly Dictionary<MeshSegmentType, MeshSegment> meshSegments = new Dictionary<MeshSegmentType, MeshSegment>();
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

        public const int AllCoords = -1;

        public static readonly Mesh Empty = new Mesh(0, 0, Array.Empty<byte>(), new Dictionary<MeshSegmentType, MeshSegment>(), Array.Empty<byte>(), null);

        public int VertexCount { get; private init; }
        public int FaceCount { get; private init; }
        public bool IsIndexed => indexSegment is not null;

        public bool HasSegment(MeshSegmentType kind)
        {
            return meshSegments.ContainsKey(kind);
        }

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

        public MeshView? GetView(MeshViewKind kind, bool cache=true)
        {
            if (meshViews.TryGetValue(kind, out MeshView? v))
                return v;

            MeshView? view = kind switch
            {
                MeshViewKind.Pos3f => MakeVertexView(MeshSegmentType.Position, kind),
                MeshViewKind.Normal3f => MakeVertexView(MeshSegmentType.Normal, kind),
                MeshViewKind.Indices3i => MakeIndexView(),
                _ => throw new NotSupportedException()
            };

            if (cache) 
                meshViews[kind] = view;

            return view;
        }

        public MeshBuilder ToBuilder()
        {
            MeshBuilder ret = new MeshBuilder(vertexData, meshSegments, indexData, indexSegment);
            return ret;
        }

        private MeshView? MakeIndexView()
        {
            if (!IsIndexed) 
                return null;

            return new MeshView(MeshViewKind.Indices3i, indexData, typeof(FaceIndices));
        }

        private MeshView? MakeVertexView(MeshSegmentType t, MeshViewKind kind)
        {
            if (meshSegments.TryGetValue(t, out MeshSegment? seg))
            {
                byte[] data = new byte[seg.TotalLength];
                seg.Copy(data, vertexData);
                return new MeshView(kind, data, seg.GetElementType());
            }

            return null;
        }
    }
}
