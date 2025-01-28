using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Warp9.IO
{
    public enum ChunkNativeFormat
    {
        Float = 0,
        Int32 = 1,
        Int32x3 = 16
    }

    public enum ChunkSemantic : short
    {
        None = 0,
        Position = 1,
        Normal = 2,
        TexCoord = 3,
        Indices = 4
    }

    public enum ChunkEncoding : short
    {
        Raw = 0,
        Int32x3 = 1,
        Float32 = 2,
        Fixed16 = 3,
        Normalized16 = 4,
        Int32 = 5,

        Ignore = 0x7fff
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct WarpBinHeader(ushort ver, ushort nch)
    {
        public uint Magic = WarpBinCommon.WarpBinMagic;
        public ushort Version = ver;
        public ushort NumChunks = nch;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct WarpBinChunkInfo
    {
        public long StreamPos;
        public int Size;
        public int Columns;
        public int Rows;
        public ChunkSemantic Semantic;
        public ChunkEncoding Encoding;
    }

    internal static class WarpBinCommon
    {
        public const uint WarpBinMagic = 0xbadcaca0;

        public static int GetEncodingStride(ChunkEncoding enc)
        {
            return enc switch
            {
                ChunkEncoding.Raw => 1,
                ChunkEncoding.Int32x3 => 12,
                ChunkEncoding.Int32 => 4,
                ChunkEncoding.Float32 => 4,
                ChunkEncoding.Fixed16 => 2,
                ChunkEncoding.Normalized16 => 2,
                ChunkEncoding.Ignore => 0,
                _ => throw new ArgumentException()
            };
        }

        public static short MakeMatrixSemantic(ChunkNativeFormat fmt, int index)
        {
            if ((int)fmt > 15 || index > 4095)
                throw new ArgumentException();

            return (short)((index & 0xfff) | ((int)fmt) << 12);
        }

        public static void DecodeMatrixSemantic(short sem, out ChunkNativeFormat fmt, out int index)
        {
            index = sem & 0xfff;
            fmt = (ChunkNativeFormat)(sem >> 12);
        }
    }
}
