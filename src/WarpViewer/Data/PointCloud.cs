using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Warp9.Data
{
    public class PointCloud
    {
        internal PointCloud(PointCloud other)
        {
            meshSegments = other.meshSegments;
            vertexData = other.vertexData;
            VertexCount = other.VertexCount;
        }

        internal PointCloud(int nv, byte[] vx, Dictionary<MeshSegmentType, MeshSegment> segs)
        {
            meshSegments = segs;
            vertexData = vx;
            VertexCount = nv;
        }

        internal readonly Dictionary<MeshSegmentType, MeshSegment> meshSegments = new Dictionary<MeshSegmentType, MeshSegment>();
        protected readonly Dictionary<MeshViewKind, MeshView?> meshViews = new Dictionary<MeshViewKind, MeshView?>();

        // Vertex data is formatted as structure of arrays. Say the contents are position of vec3 and 
        // texcoord0 of vec2. In that case the following are in vertexData back to back:
        // - all x coords of positions
        // - all y coords of positions
        // - all z coords of positions
        // - all u coords of texcoord0
        // - all v coords of texcoord1
        protected readonly byte[] vertexData;

        public const int AllCoords = -1;

        public static readonly PointCloud Empty = new PointCloud(0, Array.Empty<byte>(), new Dictionary<MeshSegmentType, MeshSegment>());

        public int VertexCount { get; private init; }


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

        public MeshView? GetView(MeshViewKind kind, bool cache = true)
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

        protected virtual MeshView? MakeIndexView()
        {
            throw new InvalidOperationException("Point clounds cannot create index views.");
        }

        public static PointCloud FromRawSoaPositions(int nv, byte[] vx)
        {
            Dictionary<MeshSegmentType, MeshSegment> segs = new Dictionary<MeshSegmentType, MeshSegment>
            {
                { MeshSegmentType.Position, new MeshSegment<Vector3>(0, nv) }
            };
            return new PointCloud(nv, vx, segs);
        }
    }
}
