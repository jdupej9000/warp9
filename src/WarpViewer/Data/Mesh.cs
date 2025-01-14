using System;
using System.Collections.Generic;

namespace Warp9.Data
{
    public class Mesh : PointCloud, IFaceCollection
    {
        internal Mesh(int nv, int nt, byte[] vx, Dictionary<MeshSegmentType, MeshSegment> segs, FaceIndices[] ix) :
            base(nv, vx, segs)
        {
            FaceCount = nt;
            indexData = ix;
        }

        internal Mesh(PointCloud pcl, int nt, FaceIndices[] ix) :
            base(pcl)
        {
            indexData = ix;
            FaceCount = nt;
        }

        readonly FaceIndices[]? indexData;

        public static new readonly Mesh Empty = new Mesh(0, 0, Array.Empty<byte>(), new Dictionary<MeshSegmentType, MeshSegment>(), Array.Empty<FaceIndices>());
       
        public int FaceCount { get; private init; }
        public bool IsIndexed => indexData is not null && indexData.Length > 0;


        public bool TryGetIndexData(out ReadOnlySpan<FaceIndices> data)
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
            MeshBuilder ret = new MeshBuilder(vertexData, meshSegments, indexData);
            return ret;
        }

        public PointCloud ToPointCloud()
        {
            return new PointCloud(this);
        }


        public static Mesh FromPointCloud(PointCloud pcl)
        {
            return new Mesh(pcl, 0, Array.Empty<FaceIndices>());
        }

        public static Mesh FromPointCloud(PointCloud pcl, Mesh facesSource)
        {
            if (facesSource.IsIndexed)
                return new Mesh(pcl, facesSource.FaceCount, facesSource.indexData);
            else
                return FromPointCloud(pcl);
        }
    }
}
