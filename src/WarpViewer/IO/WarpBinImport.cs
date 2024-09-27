using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Warp9.Data;

namespace Warp9.IO
{
    internal struct WarpBinImportChunk
    {
        public WarpBinChunkInfo Chunk;
        public MeshSegmentType SegmentType;
        public MeshSegment Segment;
    }

    public class WarpBinImport : IDisposable
    {
        private WarpBinImport(Stream s)
        {
            reader = new BinaryReader(s);
        }

        BinaryReader reader;
        List<WarpBinChunkInfo> chunks = new List<WarpBinChunkInfo>();

        public void Dispose()
        {
            reader.Dispose();
        }

        private bool ReadHeaders()
        {
            if (!TryRead(out WarpBinHeader header))
                return false;

            if (header.Magic != WarpBinCommon.WarpBinMagic)
                return false;

            int numChunks = header.NumChunks;
            for (int i = 0; i < numChunks; i++)
            {
                if (!TryRead(out WarpBinChunkInfo chunk))
                    return false;

                chunks.Add(chunk);
            }

            return true;
        }

        private bool TryRead<T>(out T value) where T : struct
        {
            T buff = default(T);
            Span<T> buffSpan = new Span<T>(ref buff);
            int numRead = reader.Read(MemoryMarshal.Cast<T, byte>(buffSpan));
            value = buff;

            return numRead == Marshal.SizeOf<T>();
        }

        private int ParsePointCloudChunks(List<WarpBinImportChunk> parsedChunks)
        {
            int nv = 0;
            int offset = 0;
            foreach (WarpBinChunkInfo chunk in chunks)
            {
                if (nv == 0)
                    nv = chunk.Rows;
                else if (nv != chunk.Rows)
                    throw new InvalidDataException();

                MeshSegmentType segType = chunk.Semantic switch
                {
                    ChunkSemantic.Position => MeshSegmentType.Position,
                    ChunkSemantic.Normal => MeshSegmentType.Normal,
                    ChunkSemantic.TexCoord => MeshSegmentType.Tex0,
                    _ => MeshSegmentType.Invalid
                };

                if (segType == MeshSegmentType.Invalid)
                    continue;

                MeshSegment? seg = chunk.Columns switch
                {
                    1 => new MeshSegment<float>(offset, chunk.Rows),
                    2 => new MeshSegment<Vector2>(offset, chunk.Rows),
                    3 => new MeshSegment<Vector3>(offset, chunk.Rows),
                    4 => new MeshSegment<Vector4>(offset, chunk.Rows),
                    _ => null
                };

                if (seg is null)
                    continue;

                parsedChunks.Add(new WarpBinImportChunk()
                {
                    Chunk = chunk,
                    SegmentType = segType,
                    Segment = seg
                });

                offset += seg.TotalLength;
            }

            return offset;
        }

        private PointCloud ReadPointCloud()
        {
            List<WarpBinImportChunk> parsedChunks = new List<WarpBinImportChunk>();
            int vertDataSize = ParsePointCloudChunks(parsedChunks);
           
            byte[] vertData = new byte[vertDataSize];
            Dictionary<MeshSegmentType, MeshSegment> vertSegments = new Dictionary<MeshSegmentType, MeshSegment>();

            int nv = 0;
            foreach (WarpBinImportChunk chunk in parsedChunks)
            {
                if (nv == 0) nv = chunk.Chunk.Rows;

                throw new NotImplementedException();

                vertSegments.Add(chunk.SegmentType, chunk.Segment);
            }

            return new PointCloud(nv, vertData, vertSegments);
        }

        public static bool TryImport(Stream s, [MaybeNullWhen(false)] out PointCloud pcl)
        {
            using WarpBinImport import = new WarpBinImport(s);
            if (!import.ReadHeaders())
            {
                pcl = null;
                return false;
            }

            

           

            throw new NotImplementedException();
        }
    }
}
