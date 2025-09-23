using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;

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

        internal PointCloud(int nv, byte[] vx, Dictionary<MeshSegmentSemantic, ReadOnlyMeshSegment> segs)
        {
            meshSegments = segs;
            vertexData = vx;
            VertexCount = nv;
        }

        internal readonly Dictionary<MeshSegmentSemantic, ReadOnlyMeshSegment> meshSegments = 
            new Dictionary<MeshSegmentSemantic, ReadOnlyMeshSegment>();

        protected readonly byte[] vertexData;

        public const int AllCoords = -1;

        public static readonly PointCloud Empty = new PointCloud(0, Array.Empty<byte>(), new Dictionary<MeshSegmentSemantic, ReadOnlyMeshSegment>());

        public int VertexCount { get; private init; }
        public byte[] RawData => vertexData;


        public bool HasSegment(MeshSegmentSemantic kind)
        {
            return meshSegments.ContainsKey(kind);
        }

        public bool TryGetRawData(MeshSegmentSemantic kind, out ReadOnlySpan<byte> data)
        {
            if (TryGetRawDataSegment(kind, out int offset, out int length))
            {
                data = new ReadOnlySpan<byte>(vertexData, offset, length);
                return true;
            }

            data = ReadOnlySpan<byte>.Empty;
            return false;
        }

        public bool TryGetData<T>(MeshSegmentSemantic kind, out ReadOnlySpan<T> data)
            where T : struct
        {
            if (meshSegments.TryGetValue(kind, out ReadOnlyMeshSegment? seg) &&
                seg is not null &&
                seg.CanCastTo<T>())
            {
                data = MemoryMarshal.Cast<byte, T>(new ReadOnlySpan<byte>(vertexData, seg.Offset, seg.Length));
                return true;
            }

            data = ReadOnlySpan<T>.Empty;
            return false;
        }

        public bool TryGetRawDataSegment(MeshSegmentSemantic kind, out int offset, out int length)
        {
            if (meshSegments.TryGetValue(kind, out ReadOnlyMeshSegment? seg))
            {
                offset = seg.Offset;
                length = seg.Length;
                return true;
            }

            offset = 0;
            length = 0;
            return false;
        }

        public virtual MeshBuilder ToBuilder()
        {
            return new MeshBuilder(vertexData, meshSegments, null);
        }

        public override string ToString()
        {
            return string.Format("Vertex data ({0} Bytes): ", vertexData.Length) +
                string.Join(", ", meshSegments.Select(
                    (t) => string.Format("{0}: {1}", t.Key, t.Value.ToString())).ToArray());
        }

        public static PointCloud FromRawSoaPositions(int nv, byte[] vx)
        {
            Dictionary<MeshSegmentSemantic, ReadOnlyMeshSegment> segs = new Dictionary<MeshSegmentSemantic, ReadOnlyMeshSegment>
            {
                { MeshSegmentSemantic.Position, ReadOnlyMeshSegment.Create<Vector3>(0, nv) }
            };
            return new PointCloud(nv, vx, segs);
        }
    }
}
