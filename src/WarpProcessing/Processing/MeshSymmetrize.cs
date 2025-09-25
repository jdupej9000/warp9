using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Warp9.Data;

namespace Warp9.Processing
{
    public static class MeshSymmetrize
    {

        public static PointCloud FlipPosCoord(PointCloud pcl, bool flipX, bool flipY, bool flipZ)
        {
            if(!pcl.TryGetData(MeshSegmentSemantic.Position, out ReadOnlySpan<Vector3> dataRaw))
                throw new ArgumentException(nameof(pcl));

            int nv = pcl.VertexCount;
          
            Vector3 flip = new Vector3(flipX ? -1 : 1, flipY ? -1 : 1, flipZ ? -1 : 1);

            MeshBuilder mb = pcl.ToBuilder();
            List<Vector3> outPos = mb.GetSegmentForEditing<Vector3>(MeshSegmentSemantic.Position, false).Data;

            for (int i = 0; i < nv; i++)
            {
                outPos[i] = flip * dataRaw[i];
            }

            return mb.ToPointCloud();
        }

        public static PointCloud MakeSymmetricRigid(PointCloud pcl, PointCloud lms)
        {
            PointCloud pclMirror = FlipPosCoord(pcl, true, false, false);
            PointCloud lmsMirror = FlipPosCoord(lms, true, false, false);
            
            throw new NotImplementedException();
        }

    }
}
