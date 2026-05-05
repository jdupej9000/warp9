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

        public static void TrigridNnSearch(int bitmapSize, int gridCells)
        {
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

            Console.WriteLine($"trigrid-nn: size={bitmapSize}, cells={gridCells}, time={sw.Elapsed.TotalSeconds}s");

            ctx.Dispose();
        }
    }
}
