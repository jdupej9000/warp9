using System;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using Warp9.Utils;

namespace Warp9.Native
{
    public enum WarpCoreOptimizationPath : int
    {
        Avx2 = 0,
        Avx512 = 1,
        Maximum = 0x7fffffff
    }

    public enum WarpCoreInfoIndex : int
    {
        VERSION = 0,
        COMPILER = 1,
        OPT_PATH = 2,
        CPU_NAME = 3,
        OPENBLAS_VERSION = 1000,
        OPENBLAS_CONFIG = 1001,
        CUDA_DEVICE = 2000,
        CUDA_RUNTIME_VERSION = 2001,
        CUDA_DRIVER_VERSION = 2002
    }

    public enum CpdInitMethod : int
    {
        CPD_INIT_EIGENVECTORS = 0,
        CPD_INIT_CLUSTERED = 1
    }

    public enum WarpCoreStatus : int
    {
        WCORE_OK = 0,
        WCORE_INVALID_ARGUMENT = -1,
        WCORE_INVALID_DIMENSION = -2,
        WCORE_NONCONVERGENCE = -3
    }

    [Flags]
    public enum CpdFlags : int
    {
        CPD_NONE = 0,
        CPD_USE_GPU = 1
    }

    [Flags]
    public enum CPD_CONV
    {
        CPD_CONV_ITER = 1,
        CPD_CONV_TOL = 2,
        CPD_CONV_SIGMA = 4,
        CPD_CONV_DSIGMA = 8,
        CPD_CONV_RTOL = 16,
        CPD_CONV_NUMERIC_ERROR = 32,
        CPD_CONV_INTERNAL_ERROR = 1048576
    }

    public enum SEARCH_STRUCTURE : int
    {
        SEARCH_TRIGRID3 = 0
    };

    public enum SEARCHD_KIND : int
    {
        SEARCHD_NN_PCL_3 = 0,
        SEARCHD_RAYCAST_TRISOUP_3 = 1,
        SEARCHD_NN_TRISOUP_3 = 2
    };

    [Flags]
    public enum SEARCH_KIND : int
    {
        SEARCH_NN_DPTBARY = 0,
        SEARCH_RAYCAST_T = 1,
        SEARCH_RAYCAST_TBARY = 2,

        SEARCH_SOURCE_IS_AOS = 0x10000000,
        SEARCH_INVERT_DIRECTION = 0x20000000
    };

    [Flags]
    public enum PCA_FLAGS : int
    {
        PCA_SCALE_TO_UNITY = 1
    };


    [StructLayout(LayoutKind.Sequential)]
    public struct CpdInfo
    {
        public int m, n, d;
        public float lambda, beta, w, sigma2init;
        public int maxit, neigen, flags;
        public float tol;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct CpdResult
    {
        public int iter;
        public int conv;
        public float err, sigma2;
        public float time, time_e;
        public int debug;

        public readonly override string ToString()
        {
            if (((CPD_CONV)conv).HasFlag(CPD_CONV.CPD_CONV_NUMERIC_ERROR))
            {
                return string.Format("it={0}, err={1}, s2={2}, t={3:F3}s, te={4:F3}s, pe={5:F1}%, conv={6}, debug={7}",
                 iter, err, sigma2, time, time_e, 100.0 * time_e / time, (CPD_CONV)conv, debug);
            }
            else
            {
                return string.Format("it={0}, err={1}, s2={2}, t={3:F3}s, te={4:F3}s, pe={5:F1}%, conv={6}",
                    iter, err, sigma2, time, time_e, 100.0 * time_e / time, (CPD_CONV)conv);
            }
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Rigid3
    {
        public Vector3 offset;
        public float cs;
        public Vector3 rot0, rot1, rot2;

        public Matrix4x4 ToMatrix()
        {
            float csr = 1.0f / cs;
            return new Matrix4x4(csr * rot0.X, csr * rot0.Y, csr * rot0.Z, 0,
               csr * rot1.X, csr * rot1.Y, csr * rot1.Z, 0,
               csr * rot2.X, csr * rot2.Y, csr * rot2.Z, 0,
               0, 0, 0, 1) * Matrix4x4.CreateTranslation(-offset);
        }

        public override readonly string ToString()
        {
            Matrix4x4 m = new Matrix4x4(rot0.X, rot0.Y, rot0.Z, 0,
                rot1.X, rot1.Y, rot1.Z, 0,
                rot2.X, rot2.Y, rot2.Z, 0,
                0, 0, 0, 1);

            Matrix4x4.Decompose(m, out Vector3 msc, out Quaternion mrot, out _);
            mrot.Decompose(out Vector3 rotAxis, out float deg);

            return string.Format("offs=({0},{1},{2}), cs={3}, rot=({4} deg about ({5},{6},{7}), scale=({8},{9},{10}))",
                offset.X, offset.Y, offset.Z,
                cs, deg,
                rotAxis.X, rotAxis.Y, rotAxis.Z,
                msc.X, msc.Y, msc.Z);
        }

        public static Rigid3 Identity = new Rigid3() { offset = Vector3.Zero, cs = 1, rot0 = Vector3.UnitX, rot1 = Vector3.UnitY, rot2 = Vector3.UnitZ };
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct PclStat3
    {
        public Vector3 x0, x1, center;
        public float size;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct GpaResult
    {
        public int iter;
        public float err;

        public readonly override string ToString()
        {
            return string.Format("it={0}, err={1}",
                iter, err);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct TriGridConfig
    {
        public int num_cells;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SearchQueryConfig
    {
        public float max_dist;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct PcaInfo
    {
        public int n, m, npcs, flags;
    }

    public static class WarpCore
    {

        [DllImport("WarpCore.dll")]
        public static extern int set_optpath(int path);

        [DllImport("WarpCore.dll", CharSet = CharSet.Ansi)]
        public static extern int wcore_get_info(int index, StringBuilder buffer, int bufferSize);

        [DllImport("WarpCore.dll")]
        public static extern int cpd_init(ref CpdInfo info, int method, nint y, nint init);

        [DllImport("WarpCore.dll")]
        public static extern int cpd_process(ref CpdInfo cpd, nint x, nint y, nint init, nint t, ref CpdResult result);

        [DllImport("WarpCore.dll")]
        public static extern int gpa_fit(nint ppdata, int d, int n, int m, nint xforms, nint mean, ref GpaResult result);

        [DllImport("WarpCore.dll")]
        public static extern int rigid_transform(nint data, int d, int m, nint xforms, nint result);

        [DllImport("WarpCore.dll")]
        public static extern int pcl_stat(nint x, int d, int m, ref PclStat3 stat);

        [DllImport("WarpCore.dll")]
        public static extern int search_build(int structure, nint vert, nint idx, int nv, int nt, nint config, ref nint ctx);

        [DllImport("WarpCore.dll")]
        public static extern int search_free(nint ctx);

        [DllImport("WarpCore.dll")]
        public static extern int search_direct(int kind, nint orig, nint dir, nint vert, int n);

        [DllImport("WarpCore.dll")]
        public static extern int search_query(nint ctx, int kind, ref SearchQueryConfig cfg, nint orig, nint dir, int n, nint hit, nint info);

        [DllImport("WarpCore.dll")]
        public static extern int clust_kmeans(nint x, int d, int n, int k, nint cent, nint label);

        [DllImport("WarpCore.dll")]
        public static extern int pca_fit(ref PcaInfo pca, nint ppdata, nint allow, nint pcs, nint lambda);

        [DllImport("WarpCore.dll")]
        public static extern int pca_data_to_scores(ref PcaInfo pca, nint data, nint pcs, nint allow, nint scores);

        [DllImport("WarpCore.dll")]
        public static extern int pca_scores_to_data(ref PcaInfo pca, nint scores, nint pcs, nint data);
    }
}
