using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Xml.Serialization;
using Warp9.Data;
using Warp9.Model;

namespace Warp9.Processing
{    
    public static class HomoMeshRepeated
    {
        public static void AverageSlope(Span<Vector3> slope, SpecimenTableSeriesSelection series, float[] x, PointCloud[] pcls)
        {
            int n = pcls[0].VertexCount;
            if (n > slope.Length) throw new InvalidOperationException("Not enough elements in 'slope'.");

            for (int i = 0; i < n; i++)
                slope[i] = Vector3.Zero;

            Vector3[] intercept = ArrayPool<Vector3>.Shared.Rent(n);
            Vector3[] seriesSlope = ArrayPool<Vector3>.Shared.Rent(n);

            foreach (SpecimenTableSeries ser in series.Series)
            {
                SeriesLm(intercept, seriesSlope, 
                    ser.Indices.Select((t) => (x[t], pcls[t])));

                for (int i = 0; i < n; i++)
                    slope[i] += seriesSlope[i];
            }

            int nser = series.Series.Count;
            for (int i = 0; i < n; i++)
                slope[i] /= nser;

            ArrayPool<Vector3>.Shared.Return(intercept);
            ArrayPool<Vector3>.Shared.Return(seriesSlope);
        }

        public static void AverageDifference(Span<Vector3> diff, SpecimenTableSeriesSelection series, PointCloud[] pcls)
        {
            int n = pcls[0].VertexCount;
            if (n > diff.Length) throw new InvalidOperationException("Not enough elements in 'diff'.");

            for (int i = 0; i < n; i++)
                diff[i] = Vector3.Zero;

            foreach (SpecimenTableSeries ser in series.Series)
            {
                pcls[ser.Indices[0]].TryGetData(MeshSegmentSemantic.Position, out ReadOnlySpan<Vector3> a);
                pcls[ser.Indices[1]].TryGetData(MeshSegmentSemantic.Position, out ReadOnlySpan<Vector3> b);

                for (int i = 0; i < n; i++)
                    diff[i] += b[i] - a[i];
            }

            int nser = series.Series.Count;
            for (int i = 0; i < n; i++)
                diff[i] /= nser;
        }

        public static void SeriesLm(Span<Vector3> intercept, Span<Vector3> slope, IEnumerable<(float X, PointCloud Pcl)> data)
        {
            (float X, BufferSegment<Vector3> Y)[] y = ExtractBufferSegments<float, Vector3>(data, MeshSegmentSemantic.Position);

            int n = y.Length;
            int m = Math.Min(y[0].Y.Data.Length, slope.Length);
            
            float xmean = 0;
            for (int i = 0; i < n; i++)
                xmean += y[i].X;
            xmean /= n;

            for (int j = 0; j < m; j++)
            {
                Vector3 rxy = Vector3.Zero;
                float sx = 0;
                Vector3 sy = Vector3.Zero;

                Vector3 ymean = Vector3.Zero;
                for (int i = 0; i < n; i++)
                    ymean += y[i].Y.Data[j];
                ymean /= n;

                for (int i = 0; i < n; i++)
                {
                    float xim = y[i].X - xmean;
                    Vector3 yim = y[i].Y[j] - ymean;

                    rxy += xim * yim;
                    sx += xim * xim;
                    sy += yim * yim;
                }

                rxy /= n;
                sx = MathF.Sqrt(sx);
                sy = Vector3.SquareRoot(sy);
                rxy /= sx * sy;

                slope[j] = rxy * sy / sx;
                intercept[j] = ymean - slope[j] * xmean;
            }
        }

        public static (TX X, BufferSegment<TY> Y)[] ExtractBufferSegments<TX, TY>(IEnumerable<(TX X, PointCloud Pcl)> data, MeshSegmentSemantic sem)
            where TX : unmanaged
            where TY : struct
        {
            return data.Select(
                (t) =>
                {
                    t.Pcl.TryGetData(sem, out BufferSegment<TY> seg);
                    return (t.X, seg);
                })
                .ToArray();
        }
    }
}
