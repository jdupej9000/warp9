using System;
using System.Collections.Generic;

namespace Warp9.Data
{
    public class Mesh : PointCloud, IFaceCollection
    {
        internal Mesh(int nv, int nt, byte[] vx, Dictionary<MeshSegmentType, MeshSegment> segs, byte[] ix, MeshSegment? idxSeg) :
            base(nv, vx, segs)
        {
            indexData = Array.Empty<byte>();
            FaceCount = nt;
            indexData = ix;
            indexSegment = idxSeg;
        }

        internal Mesh(PointCloud pcl, int nt, byte[] ix, MeshSegment? idxSeg) :
            base(pcl)
        {
            indexData = Array.Empty<byte>();
            FaceCount = nt;
            indexData = ix;
            indexSegment = idxSeg;
        }

        readonly MeshSegment? indexSegment;

        // IndexData, if present is a typical array of structures with triplets of integers grouped together,
        // that describe one face.
        readonly byte[] indexData;

        public static new readonly Mesh Empty = new Mesh(0, 0, Array.Empty<byte>(), new Dictionary<MeshSegmentType, MeshSegment>(), Array.Empty<byte>(), null);
       
        public int FaceCount { get; private init; }
        public bool IsIndexed => indexSegment is not null;


        public bool TryGetIndexData(out ReadOnlySpan<byte> data)
        {
            if (!IsIndexed)
            {
                data = default;
                return false;
            }

            data = indexData.AsSpan();
            return true;
        }

        public override MeshBuilder ToBuilder()
        {
            MeshBuilder ret = new MeshBuilder(vertexData, meshSegments, indexData, indexSegment);
            return ret;
        }

        public PointCloud ToPointCloud()
        {
            return new PointCloud(this);
        }

        protected override MeshView? MakeIndexView()
        {
            if (!IsIndexed) 
                return null;

            return new MeshView(MeshViewKind.Indices3i, indexData, typeof(FaceIndices));
        }

        public static Mesh FromPointCloud(PointCloud pcl)
        {
            return new Mesh(pcl, 0, Array.Empty<byte>(), null);
        }

        public static Mesh FromPointCloud(PointCloud pcl, Mesh facesSource)
        {
            if (facesSource.IsIndexed)
                return new Mesh(pcl, facesSource.FaceCount, facesSource.indexData, facesSource.indexSegment);
            else
                return FromPointCloud(pcl);
        }
    }
}
