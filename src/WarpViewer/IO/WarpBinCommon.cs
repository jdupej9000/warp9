using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Warp9.IO
{
    public enum ChunkNativeFormat
    {
        Float
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
        Int16 = 1,
        Int32 = 2,
        Float32 = 3,
        Fixed16 = 4,
        Normalized16 = 5,

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
