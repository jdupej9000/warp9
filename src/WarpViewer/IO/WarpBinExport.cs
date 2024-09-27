﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Warp9.Data;

namespace Warp9.IO
{
    internal class WarpBinExportTask
    {
        internal Mesh? Mesh { get; init; }
        internal PointCloud? PointCloud { get; init; }
        internal MeshSegmentType MeshSegment { get; init; }
        internal int MeshSegmentDimension { get; init; }
        internal ChunkSemantic Semantic { get; init; }
        internal ChunkEncoding Encoding { get; init; }

        internal void GetRawData(out ChunkNativeFormat fmt, out ReadOnlySpan<byte> data)
        {
            fmt = ChunkNativeFormat.Float;

            if (Mesh is not null && Semantic == ChunkSemantic.Indices)
            {
                throw new NotImplementedException();
            }
            else if (Mesh is not null && Semantic != ChunkSemantic.Indices)
            {
                if (!Mesh.TryGetRawData(MeshSegment, -1, out data))
                    throw new InvalidOperationException();
            }
            else if (PointCloud is not null)
            {
                if (!PointCloud.TryGetRawData(MeshSegment, -1, out data))
                    throw new InvalidOperationException();
            }
            else
            {
                throw new InvalidOperationException();
            }
        }
    }

    public class WarpBinExportSettings
    {
        public ChunkEncoding PositionFormat { get; set; } = ChunkEncoding.Float32;
        public int PositionDimension { get; set; } = 3;
        public ChunkEncoding NormalFormat { get; set; } = ChunkEncoding.Float32;
        public int NormalDimension { get; set; } = 3;
        public ChunkEncoding Tex0Format { get; set; } = ChunkEncoding.Normalized16;
        public int Tex0Dimension { get; set; } = 2;
        public ChunkEncoding IndexFormat { get; set; } = ChunkEncoding.Int32;
    }

    public class WarpBinExport
    {
        private WarpBinExport(Stream dest)
        {
            stream = dest;
        }

        Stream stream;
        List<WarpBinExportTask> tasks = new List<WarpBinExportTask>();

        private void AddChunk(WarpBinExportTask task)
        {
            tasks.Add(task);
        }

        private void Compose()
        {
            using BinaryWriter wr = new BinaryWriter(stream, Encoding.ASCII, true);

            WarpBinHeader hdr = new WarpBinHeader(1, (ushort)tasks.Count);
            Write(wr, ref hdr);

            long dataPos = Marshal.SizeOf<WarpBinHeader>() + tasks.Count * Marshal.SizeOf<WarpBinChunkInfo>();
            foreach (WarpBinExportTask t in tasks)
            {
                t.GetRawData(out _, out ReadOnlySpan<byte> data);
                int numElements = data.Length / 4;

                if (numElements % t.MeshSegmentDimension != 0)
                    throw new InvalidOperationException();

                WarpBinChunkInfo chunk = new WarpBinChunkInfo()
                {
                    StreamPos = dataPos,
                    Size = data.Length,
                    Columns = numElements / t.MeshSegmentDimension,
                    Rows = numElements,
                    Semantic = t.Semantic,
                    Encoding = t.Encoding
                };

                Write(wr, ref hdr);
            }

            foreach (WarpBinExportTask t in tasks)
            {
                t.GetRawData(out ChunkNativeFormat fmtFrom, out ReadOnlySpan<byte> data);
                int numElements = data.Length / t.MeshSegmentDimension / 4; // TODO

                Write(wr, data, t.MeshSegmentDimension, numElements, fmtFrom, t.Encoding);
            }
        }

        private static void FindExtremes(Span<float> ext, ReadOnlySpan<float> data, int dim, int numElems)
        {
            for (int i = 0; i < dim; i++)
            {
                ReadOnlySpan<float> channel = data.Slice(i * numElems);
                float min = float.MaxValue, max = float.MinValue;

                for (int j = 0; j < numElems; j++)
                {
                    if(channel[j] < min) min = channel[j];
                    if(channel[j] > max) max = channel[j];
                }

                ext[2 * i] = min;
                ext[2 * i + 1] = max;
            }
        }

        private static void WriteFloat32AsInt16(BinaryWriter wr, ReadOnlySpan<float> data, float min, float max)
        {
            float norm = 65535.0f / (max - min);
            for (int i = 0; i < data.Length; i++)
                wr.Write((ushort)((data[i] - min) * norm));
        }

        private static void Write(BinaryWriter wr, ReadOnlySpan<byte> data, int dim, int numElems, ChunkNativeFormat fmtFrom, ChunkEncoding fmtTo)
        {
            switch ((fmtFrom, fmtTo))
            {
                case (_, ChunkEncoding.Raw):
                    wr.Write(data);
                    break;

                case (ChunkNativeFormat.Float, ChunkEncoding.Float32):
                    wr.Write(data.Slice(0, numElems * dim * 4));
                    break;

                case (ChunkNativeFormat.Float, ChunkEncoding.Fixed16): 
                    {
                        ReadOnlySpan<float> dataf = MemoryMarshal.Cast<byte, float>(data);
                        Span<float> extr = stackalloc float[2 * dim];
                        FindExtremes(extr, dataf, dim, numElems);
                        wr.Write(MemoryMarshal.Cast<float, byte>(extr));

                        for (int i = 0; i < dim; i++)
                            WriteFloat32AsInt16(wr, dataf.Slice(i * numElems, numElems), extr[2 * i], extr[2 * i + 1]); 
                    }
                    break;

                case (ChunkNativeFormat.Float, ChunkEncoding.Normalized16): 
                    {
                        ReadOnlySpan<float> dataf = MemoryMarshal.Cast<byte, float>(data);
                        WriteFloat32AsInt16(wr, dataf, 0, 1);
                    }
                    break;

                default:
                    throw new NotImplementedException();
            }
        }

        private static void Write<T>(BinaryWriter wr, ref readonly T data) where T : struct
        {
            ReadOnlySpan<T> dataSpan = new ReadOnlySpan<T>(in data);
            wr.Write(MemoryMarshal.Cast<T, byte>(dataSpan));
        }


        private static void AddPclChunks(WarpBinExport export, PointCloud pcl, WarpBinExportSettings s)
        {
            if (s.PositionFormat != ChunkEncoding.Ignore &&
                s.PositionDimension > 0 &&
                pcl.HasSegment(MeshSegmentType.Position))
            {
                export.AddChunk(new WarpBinExportTask()
                {
                    PointCloud = pcl,
                    MeshSegment = MeshSegmentType.Position,
                    MeshSegmentDimension = s.PositionDimension,
                    Semantic = ChunkSemantic.Position,
                    Encoding = s.PositionFormat
                });
            }

            if (s.NormalFormat != ChunkEncoding.Ignore &&
                s.NormalDimension > 0 &&
                pcl.HasSegment(MeshSegmentType.Normal))
            {
                export.AddChunk(new WarpBinExportTask()
                {
                    PointCloud = pcl,
                    MeshSegment = MeshSegmentType.Normal,
                    MeshSegmentDimension = s.NormalDimension,
                    Semantic = ChunkSemantic.Normal,
                    Encoding = s.NormalFormat
                });
            }

            if (s.Tex0Format != ChunkEncoding.Ignore &&
               s.Tex0Dimension > 0 &&
               pcl.HasSegment(MeshSegmentType.Tex0))
            {
                export.AddChunk(new WarpBinExportTask()
                {
                    PointCloud = pcl,
                    MeshSegment = MeshSegmentType.Tex0,
                    MeshSegmentDimension = s.Tex0Dimension,
                    Semantic = ChunkSemantic.TexCoord,
                    Encoding = s.Tex0Format
                });
            }
        }

        private static void AddMeshIndexChunks(WarpBinExport export, Mesh m, WarpBinExportSettings s)
        {
            if (s.IndexFormat != ChunkEncoding.Ignore &&
                m.IsIndexed)
            {
                export.AddChunk(new WarpBinExportTask()
                {
                    Mesh = m,
                    Semantic = ChunkSemantic.Indices,
                    Encoding = s.IndexFormat
                });
            }
        }

        public static void ExportPcl(Stream stream, PointCloud pcl, WarpBinExportSettings? settings=null)
        {
            WarpBinExportSettings s = settings ?? new WarpBinExportSettings();
            WarpBinExport export = new WarpBinExport(stream);
            AddPclChunks(export, pcl, s);
            export.Compose();
        }

        public static void ExportMesh(Stream stream, Mesh m, WarpBinExportSettings? settings=null)
        {
            WarpBinExportSettings s = settings ?? new WarpBinExportSettings();
            WarpBinExport export = new WarpBinExport(stream);
            AddPclChunks(export, m, s);
            AddMeshIndexChunks(export, m, s);
            export.Compose();
        }
    }
}