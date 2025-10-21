using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Warp9.Data;
using Warp9.Native;

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
                outPos.Add(flip * dataRaw[i]);
            }

            return mb.ToPointCloud();
        }

        public static PointCloud MakeBilateralFlippedLandmarks(PointCloud lms)
        {
            if (!lms.TryGetData(MeshSegmentSemantic.Position, out BufferSegment<Vector3> lmsPos))
                throw new InvalidOperationException();

            int[] lmsRevOrder = LandmarkUtils.ReverseBilateralLandmarkIndices(lmsPos.Data);
            return MeshUtils.PermuteSegment<Vector3>(lms, MeshSegmentSemantic.Position, lmsRevOrder.AsSpan());
        }

        public static PointCloud MakeSymmetricRigid(Mesh pcl, PointCloud lms)
        {           
            PointCloud pclMirror = FlipPosCoord(pcl, true, false, false);
            PointCloud lmsRevMirror = FlipPosCoord(MakeBilateralFlippedLandmarks(lms), true, false, false);

            Gpa gpa = Gpa.Fit(new PointCloud[2] { lms, lmsRevMirror });
            PointCloud pclReg = RigidTransform.TransformPosition(pcl, gpa.GetTransform(0)) ??
                throw new InvalidOperationException();
            PointCloud pclMirrorReg = RigidTransform.TransformPosition(pclMirror, gpa.GetTransform(1)) ??
                throw new InvalidOperationException();

            PointCloud symmSnap = MeshSnap.SymmetricSnap(pclReg, pclMirrorReg, pcl) ?? 
                throw new InvalidOperationException();

            return RigidTransform.TransformPosition(symmSnap, gpa.GetTransform(0).Invert()) ??
            //return symmSnap ??
                throw new InvalidOperationException();
        }

    }
}
