using System;
using System.Collections;
using System.Collections.Generic;
using Warp9.Data;
using Warp9.Native;

namespace Warp9.Processing
{
    public class GpaConfiguration
    {
    }

    public class Gpa
    {
        private Gpa(PointCloud[] pcls, Rigid3[] xforms, PointCloud mean, GpaResult res)
        {
            if (pcls.Length != xforms.Length)
                throw new ArgumentException();

            Mean = mean;
            pointClouds = pcls;
            transforms = xforms;
            result = res;
        }

        private PointCloud[] pointClouds;
        private Rigid3[] transforms;
        private GpaResult result;

        public PointCloud Mean { get; private init; }
        public int NumData => pointClouds.Length;
        public int NumVertices => Mean.VertexCount;

        public override string ToString()
        {
            return string.Format("d=3, m={0}, n={1}, e={2}, it={3}",
                NumVertices, NumData, result.err, result.iter);
        }

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

        public IEnumerable<PointCloud> EnumerateTransformed()
        {
            for (int i = 0; i < pointClouds.Length; i++)
                yield return GetTransformed(i);
        }

        public Rigid3 GetTransform(int idx)
        {
            if (idx < 0 || idx >= pointClouds.Length)
                throw new ArgumentOutOfRangeException();

            return transforms[idx];
        }
        

        public static Gpa Fit(PointCloud[] data, GpaConfiguration? cfg = null)
        {
            WarpCoreStatus s = RigidTransform.FitGpa(data, 
                out PointCloud mean, out Rigid3[] xforms, out GpaResult res);

            if (s != WarpCoreStatus.WCORE_OK)
                throw new InvalidOperationException();

            return new Gpa(data, xforms, mean, res);
        }
    }
}
