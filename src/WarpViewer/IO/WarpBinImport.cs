using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
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

        private bool TryParsePointCloudChunks(List<WarpBinImportChunk> parsedChunks, out int bufferSize)
        {
            int nv = 0;
            int offset = 0;
            bufferSize = 0;

            foreach (WarpBinChunkInfo chunk in chunks)
            {
                if (chunk.Semantic == ChunkSemantic.Indices)
                    continue;

                if (nv == 0)
                    nv = chunk.Rows;
                else if (nv != chunk.Rows)
                    return false;

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

            bufferSize = offset;
            return true;
        }

        private bool TryParseMeshIndexChunks(out WarpBinImportChunk? parsedIndexChunk, out int bufferSize)
        {
            bufferSize = 0;
            parsedIndexChunk = null;

            foreach (WarpBinChunkInfo chunk in chunks)
            {
                if (chunk.Semantic == ChunkSemantic.Indices)
                {
                    bufferSize = chunk.Rows;
                    MeshSegment seg = new MeshSegment<FaceIndices>(0, chunk.Rows);
                    parsedIndexChunk = new WarpBinImportChunk()
                    {
                        Chunk = chunk,
                        SegmentType = MeshSegmentType.Invalid,
                        Segment = seg
                    };

                    return true;
                }
            }

            return false;
        }

        private void ReadInt16AsFloat32(Span<byte> buffer, int count, float min, float max)
        {
            float d = (max - min) / 65535.0f;
            Span<float> dest = MemoryMarshal.Cast<byte, float>(buffer.Slice(0, count * 4));
            
            for (int i = 0; i < count; i++)
            {
                ushort x = reader.ReadUInt16();
                dest[i] = min + x * d;
            }
        }

        private void ReadFixed16AsFloat32(Span<byte> buffer, int cols, int rows)
        {
            float[] limits = new float[cols * 2];
            reader.Read(MemoryMarshal.Cast<float, byte>(limits));

            for (int i = 0; i < cols; i++)
                ReadInt16AsFloat32(buffer.Slice(i * rows * 4), rows, limits[2 * i], limits[2 * i + 1]);
        }

        private void ReadNormalized16AsFloat32(Span<byte> buffer, int count)
        {
            ReadInt16AsFloat32(buffer, count, 0, 1);
        }

        private MatrixCollection ReadMatrix()
        {
            if (chunks.Count == 0)
                return MatrixCollection.Empty;

            MatrixCollection ret = new MatrixCollection();
            for (int chunkId = 0; chunkId < chunks.Count; chunkId++)
            {
                int cols = chunks[chunkId].Columns;
                int rows = chunks[chunkId].Rows;
                WarpBinCommon.DecodeMatrixSemantic((short)chunks[chunkId].Semantic, 
                    out ChunkNativeFormat nativeFmt, out int matKey);
                ChunkEncoding encodedFmt = chunks[chunkId].Encoding;

                if (nativeFmt == ChunkNativeFormat.Float)
                {
                    Matrix<float> m = new Matrix<float>(cols, rows);                   
                    switch (encodedFmt)
                    {
                        case ChunkEncoding.Float32:
                            reader.Read(MemoryMarshal.Cast<float, byte>(m.Data.AsSpan()));
                            break;

                        case ChunkEncoding.Fixed16:
                            ReadFixed16AsFloat32(m.GetRawData(), cols, rows);
                            break;

                        case ChunkEncoding.Normalized16:
                            ReadNormalized16AsFloat32(m.GetRawData(), cols * rows);
                            break;

                        default:
                           throw new InvalidDataException("An unsupported encoding for a native-float matrix is declared.");
                    }
                   
                    ret[matKey] = m;
                }
                else
                {
                    throw new InvalidDataException("An unsupported native matrix format is declared.");
                }
                
            }

            return ret;
        }

        private Mesh? ReadMesh()
        {
            List<WarpBinImportChunk> parsedChunks = new List<WarpBinImportChunk>();
            if(!TryParsePointCloudChunks(parsedChunks, out int vertDataSize))
                return null;
           
            byte[] vertData = new byte[vertDataSize];
            Dictionary<MeshSegmentType, MeshSegment> vertSegments = new Dictionary<MeshSegmentType, MeshSegment>();

            TryParseMeshIndexChunks(out WarpBinImportChunk? parsedIndexChunk, out int idxDataSize);
            FaceIndices[] idxData = new FaceIndices[idxDataSize];

            int nv = 0, nt = 0;
            foreach (WarpBinImportChunk chunk in parsedChunks)
            {
                if (chunk.Chunk.Semantic == ChunkSemantic.Indices)
                    continue;

                if (nv == 0) 
                    nv = chunk.Chunk.Rows;

                //reader.BaseStream.Seek(chunk.Chunk.StreamPos, SeekOrigin.Begin);

                switch (chunk.Chunk.Encoding)
                {
                    case ChunkEncoding.Float32:
                        reader.Read(vertData, chunk.Segment.Offset, chunk.Segment.TotalLength);
                        break;

                    case ChunkEncoding.Fixed16:
                        ReadFixed16AsFloat32(vertData.AsSpan(chunk.Segment.Offset, chunk.Segment.TotalLength),
                           chunk.Chunk.Columns, chunk.Chunk.Rows);
                        break;

                    case ChunkEncoding.Normalized16:
                        ReadNormalized16AsFloat32(vertData.AsSpan(chunk.Segment.Offset, chunk.Segment.TotalLength),
                           chunk.Chunk.Columns * chunk.Chunk.Rows);
                        break;

                    default:
                        throw new NotSupportedException();
                }

                vertSegments.Add(chunk.SegmentType, chunk.Segment);
            }

            if (parsedIndexChunk.HasValue)
            {
                //reader.BaseStream.Seek(parsedIndexChunk.Value.Chunk.StreamPos, SeekOrigin.Begin);
                nt = parsedIndexChunk.Value.Chunk.Rows;

                if(parsedIndexChunk.Value.Chunk.Encoding != ChunkEncoding.Int32x3)
                    throw new NotSupportedException();

                reader.Read(MemoryMarshal.Cast<FaceIndices, byte>(idxData.AsSpan()));
            }

            return new Mesh(nv, nt, vertData, vertSegments, idxData);
        }

        public static bool TryImport(Stream s, [MaybeNullWhen(false)] out Mesh pcl)
        {
            using WarpBinImport import = new WarpBinImport(s);
            if (!import.ReadHeaders())
            {
                pcl = null;
                return false;
            }

            pcl = import.ReadMesh();
            return pcl is not null;
        }

        public static bool TryImport(Stream s, [MaybeNullWhen(false)] out MatrixCollection? mats)
        {
            using WarpBinImport import = new WarpBinImport(s);
            if (!import.ReadHeaders())
            {
                mats = null;
                return false;
            }

            mats = import.ReadMatrix();
            return true;
        }
    }
}
