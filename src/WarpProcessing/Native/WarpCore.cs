﻿using System;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;

namespace Warp9.Native
{
    public enum WarpCoreInfoIndex : int
    {
        WCINFO_VERSION = 0,
        WCINFO_COMPILER = 1,
        WCINFO_OPT_PATH = 2,
        WCINFO_MKL_VERSION = 1000,
        WCINFO_MKL_ISA = 1001
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
        CPD_USE_GPU = 1
    }

    [Flags]
    public enum CPD_CONV
    {
        CPD_CONV_ITER = 1,
        CPD_CONV_TOL = 2,
        CPD_CONV_SIGMA = 4,
        CPD_CONV_DSIGMA = 8
    }


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
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Rigid3
    {
        public Vector3 offset;
        public float cs;
        public Vector3 rot0, rot1, rot2;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct PclStat3
    {
        public Vector3 x0, x1, center;
        public float size;
    }

    public static class WarpCore
    {
        [DllImport("WarpCore.dll", CharSet = CharSet.Ansi)]
        public static extern int wcore_get_info(int index, StringBuilder buffer, int bufferSize);

        [DllImport("WarpCore.dll")]
        public static extern int cpd_init(ref CpdInfo info, int method, nint y, nint init);

        [DllImport("WarpCore.dll")]
        public static extern int cpd_process(ref CpdInfo cpd, nint x, nint y, nint init, nint t, ref CpdResult result);

        [DllImport("WarpCore.dll")]
        public static extern int gpa_fit(nint ppdata, int d, int n, int m, nint xforms, nint mean);

        [DllImport("WarpCore.dll")]
        public static extern int rigid_transform(nint data, int d, int m, int xforms, nint result);

        [DllImport("WarpCore.dll")]
        public static extern int pcl_stat(nint x, int d, int m, ref PclStat3 stat);
    }
}