using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Warp9.Data
{
    public enum MeshSegmentType
    {
        Position,
        Normal,
        Tex0
    }

    internal abstract class MeshSegment
    {
        public int NumCoords;
        public int Offset;
        public int ItemLength;
        public int NumItems; // TODO: does not hold with AosData != null

        public bool IsDirty { get; protected set; } = false;
        public int TotalLength => ItemLength * NumCoords * NumItems;

        public abstract void EnsureAosData(ReadOnlySpan<byte> raw);
        public abstract void CopyAsSoa(Span<byte> raw);
        public abstract MeshSegment Clone();
    }

    internal class MeshSegment<T> : MeshSegment where T : struct
    {
        public MeshSegment()
        {
            AosData = new List<T>();
            IsDirty = true;
        }

        public List<T>? AosData { get; set; }

        public override void EnsureAosData(ReadOnlySpan<byte> raw)
        {
            if (AosData is not null) 
                return;

            ReadOnlySpan<byte> seg = raw.Slice(Offset, ItemLength * NumItems * NumCoords);
            AosData = new List<T>(MeshUtils.CopySoaToAos<T>(seg) ?? throw new InvalidOperationException());
            IsDirty = true;
        }

        public override void CopyAsSoa(Span<byte> raw)
        {
            if (AosData is null)
                throw new InvalidOperationException();

            // TODO: can we avoid ToArray() ?
            MeshUtils.CopyAosToSoa<T>(raw, AosData.ToArray().AsSpan());
        }

        public override MeshSegment Clone()
        {
            throw new NotImplementedException();
            //return new MeshSegment<T>
        }
    }
}
