using System.Runtime.InteropServices;

namespace Warp9.IO
{
    public enum ChunkNativeFormat
    {
        Float,
        Int32x3
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
    }
}
