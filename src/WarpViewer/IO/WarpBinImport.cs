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
        public MeshSegmentSemantic SegmentType;
        public ReadOnlyMeshSegment Segment;
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

                MeshSegmentSemantic segType = chunk.Semantic switch
                {
                    ChunkSemantic.Position => MeshSegmentSemantic.Position,
                    ChunkSemantic.Normal => MeshSegmentSemantic.Normal,
                    ChunkSemantic.TexCoord => MeshSegmentSemantic.Tex0,
                    ChunkSemantic.AttribScalar => MeshSegmentSemantic.AttribScalar,
                    _ => MeshSegmentSemantic.Invalid
                };

                if (segType == MeshSegmentSemantic.Invalid)
                    continue;

                ReadOnlyMeshSegment? seg = chunk.Columns switch
                {
                    1 => ReadOnlyMeshSegment.Create<float>(offset, chunk.Rows),
                    2 => ReadOnlyMeshSegment.Create<Vector2>(offset, chunk.Rows),
                    3 => ReadOnlyMeshSegment.Create<Vector3>(offset, chunk.Rows),
                    4 => ReadOnlyMeshSegment.Create<Vector4>(offset, chunk.Rows),
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

                offset += seg.Length;
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
                    ReadOnlyMeshSegment seg = ReadOnlyMeshSegment.Create<FaceIndices>(0, chunk.Rows);
                    parsedIndexChunk = new WarpBinImportChunk()
                    {
                        Chunk = chunk,
                        SegmentType = MeshSegmentSemantic.Invalid,
                        Segment = seg
                    };

                    return true;
                }
            }

            return false;
        }

        private void ReadNorm16AsFloat32(Span<byte> buffer, int count)
        {
            float d = 1.0f / 65535.0f;
            Span<float> dest = MemoryMarshal.Cast<byte, float>(buffer.Slice(0, count * 4));
            
            for (int i = 0; i < count; i++)
            {
                ushort x = reader.ReadUInt16();
                dest[i] = x * d;
            }
        }

        private void ReadFixed16AsFloat32(Span<byte> buffer, int cols, int rows)
        {
            if (cols > 16) 
                throw new InvalidOperationException("Do not stackalloc in ReadFixed16AsFloat32.");

            Span<float> limits = stackalloc float[cols * 2];
            reader.Read(MemoryMarshal.Cast<float, byte>(limits));
            Span<float> min = stackalloc float[cols];
            Span<float> norm = stackalloc float[cols];

            for (int i = 0; i < cols; i++)
            {
                min[i] = limits[2 * i];
                norm[i] = (limits[2 * i + 1] - limits[2 * i]) / 65535.0f;
            }

            Span<float> f = MemoryMarshal.Cast<byte, float>(buffer);
            for (int j = 0; j < rows; j++)
            {
                int i0 = j * cols;
                for (int i = 0; i < cols; i++)
                    f[i0 + i] = reader.ReadUInt16() * norm[i] + min[i];
            }
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
                            reader.Read(m.GetRawData());
                            break;

                        default:
                            throw new InvalidDataException("An unsupported encoding for a native-float matrix is declared.");
                    }

                    ret[matKey] = m;
                }
                else if (nativeFmt == ChunkNativeFormat.Int32)
                {
                    Matrix<int> m = new Matrix<int>(cols, rows);
                    switch (encodedFmt)
                    {
                        case ChunkEncoding.Int32:
                            reader.Read(m.GetRawData());
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
            Dictionary<MeshSegmentSemantic, ReadOnlyMeshSegment> vertSegments = new Dictionary<MeshSegmentSemantic, ReadOnlyMeshSegment>();

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
                    case ChunkEncoding.Int32:
                        reader.Read(vertData, chunk.Segment.Offset, chunk.Segment.Length);
                        break;

                    case ChunkEncoding.Fixed16:
                        ReadFixed16AsFloat32(vertData.AsSpan(chunk.Segment.Offset, chunk.Segment.Length),
                           chunk.Chunk.Columns, chunk.Chunk.Rows);
                        break;

                    case ChunkEncoding.Normalized16:
                        ReadNorm16AsFloat32(vertData.AsSpan(chunk.Segment.Offset, chunk.Segment.Length),
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
