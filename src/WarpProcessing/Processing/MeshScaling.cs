using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Warp9.Data;

namespace Warp9.Processing
{
    public static class MeshScaling
    {
        public static MeshBuilder ScalePosition(PointCloud pcl, float factor)
        {
            if(!pcl.TryGetData(MeshSegmentSemantic.Position, out ReadOnlySpan<Vector3> posData))
                throw new InvalidOperationException();

            MeshBuilder mb = pcl.ToBuilder();
            int nv = pcl.VertexCount;

            List<Vector3> posSeg = mb.GetSegmentForEditing<Vector3>(MeshSegmentSemantic.Position, false).Data;
            CollectionsMarshal.SetCount(posSeg, nv);

            for (int i = 0; i < nv; i++)
                posSeg[i] = posData[i] * factor;

            return mb;
        }
    }
}
