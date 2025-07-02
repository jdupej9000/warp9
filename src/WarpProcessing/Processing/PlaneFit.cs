using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace Warp9.Processing
{
    public static class PlaneFit
    {
        public static Vector3 Centroid(IReadOnlyList<Vector3> pts)
        {
            Vector3 sum = Vector3.Zero;
            for (int i = 0; i < pts.Count; i++)
                sum += pts[i];

            return sum / pts.Count;
        }

        public static Plane FitLsOrtho(IReadOnlyList<Vector3> pts)
        {
            Vector3 center = Centroid(pts);

            float t0 = 0, t1 = 0, t2 = 0, t3 = 0, t4 = 0, t5 = 0;
            for (int i = 0; i < pts.Count; i++)
            {
                Vector3 pt = pts[i] - center;

                t0 += pt.X * pt.X;
                t1 += pt.X * pt.Y;
                t2 += pt.X * pt.Z;
                t3 += pt.Y * pt.X;
                t4 += pt.Y * pt.Z;
                t5 += pt.Z * pt.Z;
            }
            
            return new Plane();
        }

        // TODO: need eigenvectors + eigenvalues of a 3x3 symmetric matrix
        // e.g.: https://hal.science/hal-01501221/document
        // or  http://www.mpi-hd.mpg.de/personalhomes/globes/3x3/index.html
        // or! https://www.geometrictools.com/GTE/Mathematics/SymmetricEigensolver3x3.h
    }
}
