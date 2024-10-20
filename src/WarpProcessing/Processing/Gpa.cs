using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Warp9.Data;
using Warp9.Jobs;
using Warp9.Native;

namespace Warp9.Processing
{
    public class GpaConfiguration
    {
    }

    public class Gpa
    {
        private Gpa(PointCloud[] pcls, Rigid3[] xforms, PointCloud mean)
        {
            if (pcls.Length != xforms.Length)
                throw new ArgumentException();

            Mean = mean;
            pointClouds = pcls;
            transforms = xforms;
        }

        private PointCloud[] pointClouds;
        private Rigid3[] transforms;

        public PointCloud Mean { get; private init; }
        public int NumData => pointClouds.Length;
        public int NumVertices => Mean.VertexCount;

        public PointCloud GetTransformed(int idx)
        {
            if (idx < 0 || idx >= pointClouds.Length)
                throw new ArgumentOutOfRangeException();

            PointCloud? transformed = RigidTransform.TransformPosition(
                pointClouds[idx], transforms[idx]);

            if (transformed is null)
                throw new InvalidOperationException();

            return transformed;
        }
        

        public static Gpa Fit(PointCloud[] data, GpaConfiguration? cfg = null)
        {
            WarpCoreStatus s = (WarpCoreStatus)RigidTransform.FitGpa(data, 
                out PointCloud mean, out Rigid3[] xforms, out GpaResult res);

            return new Gpa(data, xforms, mean);
        }
    }
}
