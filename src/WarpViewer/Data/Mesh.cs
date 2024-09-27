using SharpDX.D3DCompiler;
using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Warp9.Data
{
    public class Mesh : PointCloud
    {
        internal Mesh(int nv, int nt, byte[] vx, Dictionary<MeshSegmentType, MeshSegment> segs, byte[] ix, MeshSegment? idxSeg) :
            base(nv, vx, segs)
        {
            indexData = Array.Empty<byte>();
            FaceCount = nt;
            indexData = ix;
            indexSegment = idxSeg;
        }

        readonly MeshSegment? indexSegment;

        // IndexData, if present is a typical array of structures with triplets of integers grouped together,
        // that describe one face.
        readonly byte [] indexData;

        public static new readonly Mesh Empty = new Mesh(0, 0, Array.Empty<byte>(), new Dictionary<MeshSegmentType, MeshSegment>(), Array.Empty<byte>(), null);
       
        public int FaceCount { get; private init; }
        public bool IsIndexed => indexSegment is not null;

        public MeshBuilder ToBuilder()
        {
            MeshBuilder ret = new MeshBuilder(vertexData, meshSegments, indexData, indexSegment);
            return ret;
        }

        protected override MeshView? MakeIndexView()
        {
            if (!IsIndexed) 
                return null;

            return new MeshView(MeshViewKind.Indices3i, indexData, typeof(FaceIndices));
        }
    }
}
