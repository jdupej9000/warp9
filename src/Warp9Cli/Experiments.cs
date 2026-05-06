using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Numerics;
using System.Text;
using Warp9.Data;
using Warp9.IO;
using Warp9.Native;

namespace Warp9Cli
{
    public static class Experiments
    {
        // TODO: the utility functions here are duplicates from TestUtils et al.
        public static string AssetsPath = @"../../test/data/";

        public static Stream OpenAsset(string name)
        {
            string path = Path.Combine(AssetsPath, name);

            return new FileStream(path, FileMode.Open, FileAccess.Read);
        }

        static Mesh LoadObjAsset(string name, ObjImportMode mode)
        {
            using Stream s = OpenAsset(name);
            if (!ObjImport.TryImport(s, mode, out Mesh m, out string errMsg))
                throw new FileNotFoundException("Failed to load OBJ asset: " + errMsg);

            return m;
        }

        static void GenerateGrid(int nx, int ny, Vector3 p00, Vector3 p01, Vector3 p10, out Vector3[] p)
        {
            p = new Vector3[nx * ny];

            Vector3 dx = (p01 - p00) / nx;
            Vector3 dy = (p10 - p00) / nx;

            for (int j = 0; j < ny; j++)
            {
                for (int i = 0; i < nx; i++)
                {
                    p[i + nx * j] = p00 + (float)i * dx + (float)j * dy;
                }
            }
        }

        public static void GenerateRays(Vector3 camera, int nx, int ny, out Vector3[] p0, out Vector3[] d)
        {
            // TODO: make the constants more global
            // https://www.mvps.org/directx/articles/rayproj.htm
            p0 = new Vector3[nx * ny];
            d = new Vector3[nx * ny];

            const float Fov = MathF.PI * 0.95f;
            const float Aspect = 1;
            const float Far = 100.0f;
            const float Near = 0.01f;
            //Vector3 camera = new Vector3(0.75f, 2.0f, 2.5f);
            //Vector3 camera = new Vector3(1.0f, 2.0f, -3.0f);
            Vector3 at = new Vector3(0, 0, 0);
            Vector3 up = new Vector3(0, 1, 0);

            Matrix4x4 viewProj = Matrix4x4.CreateLookAtLeftHanded(camera, at, up) *
               Matrix4x4.CreatePerspectiveFieldOfViewLeftHanded(Fov, Aspect, Near, Far);

            //Matrix4x4 viewProj = Matrix4x4.CreatePerspectiveFieldOfViewLeftHanded(Fov, Aspect, Near, Far) * Matrix4x4.CreateLookAtLeftHanded(camera, at, up);

            Matrix4x4.Invert(viewProj, out Matrix4x4 viewProjInv);

            float wd2r = 1.0f / (nx / 2.0f);
            float hd2r = 1.0f / (ny / 2.0f);

            for (int j = 0; j < ny; j++)
            {
                float dy = MathF.Tan(Fov * 0.5f) * (1.0f - j * hd2r);

                for (int i = 0; i < nx; i++)
                {
                    float dx = MathF.Tan(Fov * 0.5f) * (i * wd2r - 1.0f) / Aspect;

                    Vector3 pp1 = Vector3.Transform(new Vector3(dx * Near, dy * Near, Near), viewProjInv);
                    Vector3 pp2 = Vector3.Transform(new Vector3(dx * Far, dy * Far, Far), viewProjInv);

                    int idx = j * nx + i;
                    p0[idx] = camera;
                    d[idx] = Vector3.Normalize(pp2 - pp1);
                }
            }
        }

        private static PointCloud DistortPcl(PointCloud pcl, Vector3 t, float scale, float noise)
        {
            MeshBuilder mb = pcl.ToBuilder();
            List<Vector3> pos = mb.GetSegmentForEditing<Vector3>(MeshSegmentSemantic.Position, true).Data;

            Random rand = new Random(74656);
            for (int i = 0; i < pos.Count; i++)
            {
                Vector3 gn = new Vector3(rand.NextSingle() * noise, rand.NextSingle() * noise, rand.NextSingle() * noise);
                pos[i] = scale * pos[i] + t + gn;
            }

            return mb.ToPointCloud();
        }

        public static void TrigridNnSearch(int bitmapSize, int gridCells)
        {
            Console.WriteLine($"trigrid-nn");
            Console.WriteLine($"  size   : {bitmapSize}x{bitmapSize}");
            Console.WriteLine($"  cells  : {gridCells}^3");

            Mesh mesh = LoadObjAsset("teapot.obj", Warp9.IO.ObjImportMode.PositionsOnly);
            SearchContext.TryInitTrigrid(mesh, gridCells, out SearchContext? ctx);   
            Aabb bbox = ctx.GetSpan();
          
            // x0=<-3, 0, -2>, x1=<3.434, 3.15, 2>, center=<0.053937342, 1.7241387, -0.00024491842>, cs=2.0256174
            GenerateGrid(bitmapSize, bitmapSize,
                new Vector3(-3.5f, 5.2f, 0f), new Vector3(3.5f, 5.2f, 0f), new Vector3(-3.5f, -1.8f, 0f),
                out Vector3[] pts);

            int[] hit = new int[bitmapSize * bitmapSize];
            ResultInfoDPtBary[] res = new ResultInfoDPtBary[bitmapSize * bitmapSize];

            Stopwatch sw = new Stopwatch();
            sw.Start();
            ctx.Nearest(pts.AsSpan(), bitmapSize * bitmapSize, 1.0f, hit.AsSpan(), res.AsSpan());
            sw.Stop();

            Console.WriteLine($"  time   : {sw.Elapsed.TotalSeconds:F3} s");
            Console.WriteLine($"  score  : {bitmapSize * bitmapSize / sw.Elapsed.TotalSeconds / 1e6f:F3} Mqps");

            ctx.Dispose();
        }

        public static void TrigridRaycast(int bitmapSize, int gridCells)
        {
            Console.WriteLine($"trigrid-raycast");
            Console.WriteLine($"  size   : {bitmapSize}x{bitmapSize}");
            Console.WriteLine($"  cells  : {gridCells}^3");

            Mesh mesh = LoadObjAsset("teapot.obj", Warp9.IO.ObjImportMode.PositionsOnly);
            SearchContext.TryInitTrigrid(mesh, gridCells, out SearchContext? ctx);

            Vector3 camera = new Vector3(2.0f, 3.5f, 0.5f);

            GenerateRays(camera, bitmapSize, bitmapSize, out Vector3[] p0, out Vector3[] d);
            int n = bitmapSize * bitmapSize;
            int[] hit = new int[n];
            float[] t = new float[n];

            for (int i = 0; i < n; i++)
                p0[i] += 1.5f * camera;

            DateTime t0 = DateTime.Now;
            ctx.Raycast(p0.AsSpan(), d.AsSpan(), n, hit.AsSpan(), t.AsSpan());
            DateTime t1 = DateTime.Now;
            double seconds = (t1 - t0).TotalSeconds;

            Console.WriteLine($"  time   : {seconds:F3} s");
            Console.WriteLine($"  score  : {bitmapSize * bitmapSize / seconds / 1e6f:F3} Mqps");

            ctx.Dispose();
        }

        public static void Cpd(bool gpu)
        {
            Console.WriteLine($"cpd");
            Console.WriteLine($"  gpu    : {gpu}");

            Mesh pcl = LoadObjAsset("teapot.obj", Warp9.IO.ObjImportMode.PositionsOnly);
            PointCloud pclTarget = DistortPcl(pcl, Vector3.Zero, 1.10f, 0.25f);

            CpdConfiguration cpdCfg = new CpdConfiguration();
            cpdCfg.UseGpu = gpu;
            WarpCoreStatus stat = CpdContext.TryInitNonrigidCpd(out CpdContext? ctx, pcl, cpdCfg);
            WarpCoreStatus regStat = ctx.Register(pclTarget, out PointCloud? pclBent, out CpdResult result);
            Console.WriteLine($"  result : {result}");
        }
    }
}
