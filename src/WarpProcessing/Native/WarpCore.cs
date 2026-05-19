using System;
using System.Data;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Warp9.Utils;

namespace Warp9.Native
{
    [Flags]
    public enum WarpCoreOptimizationPath : int
    {
        Avx2 = 0x1,
        Avx512 = 0x2,
        Hybrid = 0x10000000,
        Maximum = 0x7fffffff
    }

    public enum WarpCoreInfoIndex : int
    {
        VERSION = 0,
        COMPILER = 1,
        OPT_PATH = 2,
        CPU_NAME = 3,
        BUILD_DATE = 4,
        GIT_HASH = 5,
        OPENMP_THREADS = 6,
        OPENBLAS_VERSION = 1000,
        OPENBLAS_CONFIG = 1001,
        CUDA_DEVICE = 2000,
        CUDA_DEVICE_SM_COUNT = 2001,
        CUDA_DEVICE_MEMORY = 2002,
        CUDA_DEVICE_COMPUTE_CAP = 2003,
        CUDA_RUNTIME_VERSION = 2100,
        CUDA_DRIVER_VERSION = 2101
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

        SEARCH_INVERT_DIRECTION = 0x20000000
    };

    public enum SEARCH_INFO : int
    {
        SEARCHINFO_AABB = 0
    };

    [Flags]
    public enum PCA_FLAGS : int
    {
        PCA_SCALE_TO_UNITY = 1
    };

    public enum PCL_IMPUTE_METHOD : int
    {
        TPS_GRIDSEL = 0,
        LSTPS_GRIDSEL = 1
    };

    [Flags]
    public enum PCL_IMPUTE_FLAGS : int
    {
        PCL_IMPUTE_NEGATE_MASK = 1,
        PCL_IMPUTE_ALL = 2
    };

    public enum TRANSFORM_KIND : int
    {
        TPS = 0,
        LSTPS = 1
    }

    [Flags]
    public enum TRANSFORM_FLAGS : int
    {
        None = 0
    }

    public enum WCORE_CLUST_METHOD : int
    {
        KMEANS = 0,
        GRIDSEL = 1,
        GRIDSEL_CENTRAL = 2
    };

    [StructLayout(LayoutKind.Sequential)]
    public struct CpdInfo
    {
        public int m, n, d;
        public float lambda, beta, w, sigma2init;
        public int maxit, neigen, flags;
        public float tol;
        public int debug_key;
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

    [InlineArray(3)]
    public struct BlitVec3
    {
        private float _elem0;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Rigid3
    {
        public Rigid3(Vector3 o, float cs, Vector3 r0, Vector3 r1, Vector3 r2)
        {
            Offset = o;
            this.cs = cs;
            Rot0 = r0;
            Rot1 = r1;
            Rot2 = r2;
        }

        public BlitVec3 _offset;
        public float cs;
        public BlitVec3 _rot0, _rot1, _rot2;

        public Vector3 Offset
        {
            readonly get { return new Vector3(_offset); }
            set { _offset[0] = value.X; _offset[1] = value.Y; _offset[2] = value.Z; }
        }

        public Vector3 Rot0
        {
            readonly get { return new Vector3(_rot0); }
            set { _rot0[0] = value.X; _rot0[1] = value.Y; _rot0[2] = value.Z; }
        }

        public Vector3 Rot1
        {
            readonly get { return new Vector3(_rot1); }
            set { _rot1[0] = value.X; _rot1[1] = value.Y; _rot1[2] = value.Z; }
        }

        public Vector3 Rot2
        {
            readonly get { return new Vector3(_rot2); }
            set { _rot2[0] = value.X; _rot2[1] = value.Y; _rot2[2] = value.Z; }
        }

        public readonly Matrix4x4 Rotation => new Matrix4x4(_rot0[0], _rot0[1], _rot0[2], 0,
                _rot1[0], _rot1[1], _rot1[2], 0,
                _rot2[0], _rot2[1], _rot2[2], 0,
                0, 0, 0, 1);

        public readonly Matrix4x4 ToMatrix()
        {
            float csr = 1.0f / cs;
            return Matrix4x4.CreateScale(csr) * Rotation * Matrix4x4.CreateTranslation(-Offset);
        }
        
        public readonly Rigid3 Invert()
        {
            Vector3 r0 = new Vector3(Rot0.X, Rot1.X, Rot2.X);
            Vector3 r1 = new Vector3(Rot0.Y, Rot1.Y, Rot2.Y);
            Vector3 r2 = new Vector3(Rot0.Z, Rot1.Z, Rot2.Z);
            Vector3 o = new Vector3(
                -cs * Vector3.Dot(r0, Offset),
                -cs * Vector3.Dot(r1, Offset),
                -cs * Vector3.Dot(r2, Offset));

            return new Rigid3(o, 1.0f / cs, r0, r1, r2);
        }

        public readonly Vector3 Unrotate(Vector3 v)
        {
            return new Vector3(Vector3.Dot(Rot0, v), Vector3.Dot(Rot1, v), Vector3.Dot(Rot2, v));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Vector3 Rotate(Vector3 v)
        {
            return Rot0 * v.X + Rot1 * v.Y + Rot2 * v.Z;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Vector3 Transform(Vector3 v)
        {
            return Rotate(v - Offset) / cs;
        }

        public static Rigid3 operator* (float left, Rigid3 right)
        {
            return new Rigid3(right.Offset, right.cs / left,
                right.Rot0, right.Rot1, right.Rot2);
        }

        public static Rigid3 Translation(Vector3 negOffs)
        {
            return new Rigid3(negOffs, 1,
                Vector3.UnitX, Vector3.UnitY, Vector3.UnitZ);
        }

        public static Rigid3 Scale(float scale)
        {
            return new Rigid3(Vector3.Zero, 1 / scale,
               Vector3.UnitX, Vector3.UnitY, Vector3.UnitZ);          
        }

        public static Rigid3 RotateAboutZ(float angleRad)
        {
            (float s, float c) = MathF.SinCos(angleRad);

            return new Rigid3(Vector3.Zero, 1,
               new Vector3(c, -s, 0), new Vector3(s, c, 0), Vector3.UnitZ);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 operator* (Rigid3 left, Vector3 right)
        {
            return left.Transform(right);
        }

        public static Rigid3 operator* (Rigid3 left, Rigid3 right)
        {
            // ret(x) = left(right(x))
            // see rigid_combine in gpa_impl.cpp
            // H,g = left
            // R,f = right
            float csr = left.cs * right.cs;
            return new Rigid3(right.Offset + csr * right.Unrotate(left.Offset), csr,
              left.Rotate(right.Rot0), left.Rotate(right.Rot1), left.Rotate(right.Rot2));
        }

        public override readonly string ToString()
        {
            Matrix4x4.Decompose(Rotation, out Vector3 msc, out Quaternion mrot, out _);
            mrot.Decompose(out Vector3 rotAxis, out float deg);

            return string.Format("offs=({0},{1},{2}), cs={3}, rot=({4} deg about ({5},{6},{7}), scale=({8},{9},{10}))",
                Offset.X, Offset.Y, Offset.Z,
                cs, deg,
                rotAxis.X, rotAxis.Y, rotAxis.Z,
                msc.X, msc.Y, msc.Z);
        }

        public static readonly Rigid3 Identity = new Rigid3(Vector3.Zero, 1, Vector3.UnitX, Vector3.UnitY, Vector3.UnitZ);
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct PclStat3
    {
        public float x0, y0, z0, x1, y1, z1;
        public float xc, yc, zc;
        public float size;

        public Vector3 Min => new Vector3(x0, y0, z0);
        public Vector3 Max => new Vector3(x1, y1, z1);
        public Vector3 Center => new Vector3(xc, yc, zc);
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

    [StructLayout(LayoutKind.Sequential)]
    public struct ImputeInfo
    {
        public PCL_IMPUTE_METHOD method;
        public int d, n;
        public int decim_count;
        public PCL_IMPUTE_FLAGS flags;
    };

    [StructLayout(LayoutKind.Sequential)]
    public struct FitTransformInfo
    {
        public TRANSFORM_KIND kind;
        public TRANSFORM_FLAGS flags;
        public int dimension;
        public int num_ctl_points;
        public nint ctl_idx;
    };

    public static partial class WarpCore
    {
        #region Private
        [LibraryImport("WarpCore")]
        private static partial int set_optpath(int path);

        [LibraryImport("WarpCore")]
        private static partial int wcore_get_info(int index, Span<byte> utf8buffer, int bufferSizeBytes);
        #endregion

        [LibraryImport("WarpCore")]
        public static partial int cpd_init(ref CpdInfo info, int method, nint y, nint init);

        [LibraryImport("WarpCore")]
        public static partial int cpd_process(ref CpdInfo cpd, nint x, nint y, nint init, nint t, ref CpdResult result);

        [LibraryImport("WarpCore")]
        public static partial int gpa_fit(nint ppdata, nint pallow, int d, int n, int m, nint xforms, nint mean, ref GpaResult result);

        [LibraryImport("WarpCore")]
        public static partial int rigid_transform(nint data, int d, int m, nint xforms, nint result);

        [LibraryImport("WarpCore")]
        public static partial int pcl_stat(nint x, int d, int m, ref PclStat3 stat);

        [LibraryImport("WarpCore")]
        public static partial int opa_fit(nint t, nint x, nint allow, int d, int m, ref Rigid3 xform);

        [LibraryImport("WarpCore")]
        public static partial int search_build(int structure, nint vert, nint idx, int nv, int nt, nint config, ref nint ctx);

        [LibraryImport("WarpCore")]
        public static partial int search_free(nint ctx);

        [LibraryImport("WarpCore")]
        public static partial int search_direct(int kind, nint orig, nint dir, nint vert, int n);

        [LibraryImport("WarpCore")]
        public static partial int search_query(nint ctx, int kind, ref SearchQueryConfig cfg, nint orig, nint dir, int n, nint hit, nint info);

        [LibraryImport("WarpCore")]
        public static partial int search_info(nint ctx, int kind, int param, nint res, int ressize);

        [LibraryImport("WarpCore")]
        public static partial int clust_fit(nint x, int d, int n, int k, nint cent, nint label, int method);

        [LibraryImport("WarpCore")]
        public static partial int pca_fit(ref PcaInfo pca, nint ppdata, nint allow, nint pcs, nint lambda);

        [LibraryImport("WarpCore")]
        public static partial int pca_data_to_scores(ref PcaInfo pca, nint data, nint pcs, nint allow, nint scores);

        [LibraryImport("WarpCore")]
        public static partial int pca_scores_to_data(ref PcaInfo pca, nint scores, nint pcs, nint data);

        [LibraryImport("WarpCore")]
        public static partial int pcl_impute(ref ImputeInfo info, nint data, nint templ, nint valid_mask);

        [LibraryImport("WarpCore")]
        public static partial int transform_fit(ref FitTransformInfo info, int m, nint src, nint dest, ref nint ctx);

        [LibraryImport("WarpCore")]
        public static partial int transform_apply(nint ctx, int m, nint x, nint y);

        [LibraryImport("WarpCore")]
        public static partial int transform_destroy(nint ctx);


        public static string GetInfoString(WarpCoreInfoIndex wcii)
        {
            const int MaxDataLen = 1024;
            Span<byte> sb = stackalloc byte[MaxDataLen];
            int len = wcore_get_info((int)wcii, sb, MaxDataLen);
            return Encoding.UTF8.GetString(sb.Slice(0, len));
        }

        public static WarpCoreOptimizationPath LimitOptimizationPath(WarpCoreOptimizationPath op = WarpCoreOptimizationPath.Maximum)
        {
            return (WarpCoreOptimizationPath)set_optpath((int)op);
        }
    }
}
