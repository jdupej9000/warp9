using SharpDX.Direct3D11;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Warp9.Utils;
using Warp9.Viewer;

namespace Warp9.Data
{
    public interface IMeshSegmentBuilder : IMeshSegment
    {
        ReadOnlySpan<byte> GetRawData();
    };

    public static class MeshSegmentBuilder
    {
        public static MeshSegmentBuilder<T> FromReadOnlySegment<T>(byte[] data, ReadOnlyMeshSegment seg)
            where T : struct
        {
            if (!seg.CanCastTo<T>())
                throw new InvalidCastException();

            MeshSegmentBuilder<T> ret = new MeshSegmentBuilder<T>();
            List<T> buffer = ret.Data;
            CollectionsMarshal.SetCount(buffer, seg.NumItems);

            Span<T> bufferSpan = CollectionsMarshal.AsSpan(buffer);
            data.AsSpan(seg.Offset, seg.Length).CopyTo(MemoryMarshal.Cast<T, byte>(bufferSpan));

            return ret;
        }
    }

    public class MeshSegmentBuilder<T> : IMeshSegmentBuilder
        where T : struct
    {
        public MeshSegmentBuilder()
        {
            Format = MiscUtils.TypeComposition<T>();
        }

        List<T> data = new List<T>();

        public int NumItems => data.Count;
        public int NumStructElems => MiscUtils.GetNumStructElems(Format);
        public int StructElemSize => MiscUtils.GetStructElemSize(Format);
        public int Length => NumItems * NumStructElems * StructElemSize;
        public MeshSegmentFormat Format { get; private init; }

        public List<T> Data 
        { 
            get { return data; } 
            set { data = value; } 
        }

        public ReadOnlySpan<byte> GetRawData()
        {
            Span<T> bufferSpan = CollectionsMarshal.AsSpan(data);
            return MemoryMarshal.Cast<T, byte>(bufferSpan);
        }
    }

    public class MeshBuilder
    {
        public MeshBuilder()
        {
            data = Array.Empty<byte>();
        }

        public MeshBuilder(byte[] vertData, Dictionary<MeshSegmentSemantic, ReadOnlyMeshSegment> vertSegments, FaceIndices[]? faces)
        {
            data = vertData;
            foreach (var kvp in vertSegments)
                meshSegments.Add(kvp.Key, kvp.Value);

            indexData = faces;
        }


        byte[] data;
        FaceIndices[]? indexData = null;
        List<FaceIndices>? indexDataEdit = null;

        internal readonly Dictionary<MeshSegmentSemantic, IMeshSegment> meshSegments =
           new Dictionary<MeshSegmentSemantic, IMeshSegment>();

        public List<FaceIndices> GetIndexSegmentForEditing()
        {
            if (indexDataEdit is not null)
            {
                return indexDataEdit;
            }

            indexDataEdit = new List<FaceIndices>();
            if (indexData is not null)
            {
                indexDataEdit.AddRange(indexData);
                indexData = null;
            }

            return indexDataEdit;
        }

        public void RemoveIndexSegment()
        {
            indexData = null;
            indexDataEdit = null;
        }

        public MeshSegmentBuilder<T> GetSegmentForEditing<T>(MeshSegmentSemantic segmentSemantic, bool tryPreserve)
            where T : struct
        {
            if (tryPreserve &&
                meshSegments.TryGetValue(segmentSemantic, out IMeshSegment? seg))
            {
                if (seg is MeshSegmentBuilder<T> segt)
                {
                    return segt;
                }
                else if (seg is ReadOnlyMeshSegment segr && segr.CanCastTo<T>())
                {
                    MeshSegmentBuilder<T> msb = MeshSegmentBuilder.FromReadOnlySegment<T>(data, segr);
                    meshSegments[segmentSemantic] = msb;
                    return msb;
                }
            }

            MeshSegmentBuilder<T> ret = new MeshSegmentBuilder<T>();
            meshSegments[segmentSemantic] = ret;
            return ret;
        }

        private void ComposeVertices(out byte[] vertData, out Dictionary<MeshSegmentSemantic, ReadOnlyMeshSegment> vertSegs)
        {
            vertSegs = new Dictionary<MeshSegmentSemantic, ReadOnlyMeshSegment>();

            // Build segment descriptors and find the required array size;
            int bufferSize = 0;
            foreach (var kvp in meshSegments)
            {
                vertSegs.Add(kvp.Key, ReadOnlyMeshSegment.CloneWithOffset(kvp.Value, bufferSize));
                bufferSize += kvp.Value.Length;
            }

            // Compose the buffer from original or edited sources.
            vertData = new byte[bufferSize];
            int ptr = 0;
            foreach (var kvp in meshSegments)
            {
                ReadOnlySpan<byte> src;
                if (kvp.Value is IMeshSegmentBuilder bld)
                    src = bld.GetRawData();
                else if (kvp.Value is ReadOnlyMeshSegment roms)
                    src = data.AsSpan(roms.Offset, roms.Length);
                else
                    throw new InvalidOperationException();

                src.CopyTo(vertData.AsSpan(ptr, src.Length));
                ptr += src.Length;
            }
        }

        private FaceIndices[]? ComposeIndices()
        {
            if (indexDataEdit is not null)
                return indexDataEdit.ToArray();
            else if (indexData is not null)
                return indexData;

            return null;
        }

        private void ValidateSegments(bool isPcl, out int nv, out int nt)
        {
            if (meshSegments.Count == 0)
            {
                nv = 0;
                nt = 0;
                return;
            }

            int nv0 = int.MaxValue, nv1 = int.MinValue;
            foreach (var kvp in meshSegments)
            {
                if (nv0 > kvp.Value.NumItems) nv0 = kvp.Value.NumItems;
                if (nv1 < kvp.Value.NumItems) nv1 = kvp.Value.NumItems;
            }

            if (nv0 != nv1)
                throw new InvalidDataException("Vertex counts not identical in all channels.");

            nv = nv0;

            if (indexDataEdit is not null)
            {
                nt = indexDataEdit.Count;
            }
            else if (indexData is not null && indexData.Length > 0)
            {
                nt = indexData.Length;
            }
            else
            {
                if (!isPcl && nv % 3 != 0)
                    throw new InvalidDataException("Vertex count in nonindexed meshes must be divisible by 3.");

                nt = nv / 3;
            }
        }

        public Mesh ToMesh()
        {
            ValidateSegments(true, out int nv, out int nt);
            ComposeVertices(out byte[] vertData, out Dictionary<MeshSegmentSemantic, ReadOnlyMeshSegment> vertSegs);
            FaceIndices[] faces = ComposeIndices() ?? Array.Empty<FaceIndices>();
            return new Mesh(nv, nt, vertData, vertSegs, faces);
        }

        public PointCloud ToPointCloud()
        {
            ValidateSegments(true, out int nv, out int _);
            ComposeVertices(out byte[] vertData, out Dictionary<MeshSegmentSemantic, ReadOnlyMeshSegment> vertSegs);

            return new PointCloud(nv, vertData, vertSegs);
        }
    }
}
