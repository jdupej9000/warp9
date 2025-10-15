using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using Warp9.Utils;
using Warp9.Viewer;

namespace Warp9.Data
{
    public enum MeshSegmentSemantic
    {
        Position,
        Normal,
        Tex0,
        AttribScalar,

        Invalid
    }

    public enum MeshSegmentFormat
    {
        Float32,
        Float32x2,
        Float32x3,
        Float32x4,
        Float32x16,

        Unknown
    }

    public interface IMeshSegment
    {
       public int NumItems { get; }
       public int NumStructElems { get; }
       public int StructElemSize { get; }
       public int Length { get; }
       public MeshSegmentFormat Format { get; }
    }

    public class ReadOnlyMeshSegment : IMeshSegment
    {
        public int NumItems { get; protected init; }
        public int NumStructElems => MiscUtils.GetNumStructElems(Format);
        public int StructElemSize => MiscUtils.GetStructElemSize(Format);
        public int Offset { get; protected init; }
        public MeshSegmentFormat Format { get; protected init; }
        public int Length => NumItems * NumStructElems * StructElemSize;

        public bool CanCastTo<T>() where T : struct
        {
            return Marshal.SizeOf<T>() == NumStructElems * StructElemSize;
        }

        public override string ToString()
        {
            return string.Format("{0}x {1}x{2}b", NumItems, NumStructElems, StructElemSize * 8);
        }

        public static ReadOnlyMeshSegment CloneWithOffset(IMeshSegment seg, int offset)
        {
            return new ReadOnlyMeshSegment { 
                NumItems = seg.NumItems,
                Format = seg.Format,
                Offset = offset
            };
        }
        public static ReadOnlyMeshSegment Create<T>(int offs, int numItems)
            where T : struct
        {
            return new ReadOnlyMeshSegment
            {
                Offset = offs,
                NumItems = numItems,
                Format = MiscUtils.TypeComposition<T>()
            };
        }
       
    }

   


   /* internal abstract class MeshSegment
    {
        protected int numItems;
        protected int structSize;
        protected int structElemCount;
        
        public int Offset { get; set; }
        public bool IsDirty { get; protected set; } = false;
        public int TotalLength => GetNumItems() * structSize;
        public int ChannelLength => TotalLength / structElemCount;
        public int StructLength => structSize;
        public int StructElemCount => structElemCount;
        public int NumItems => GetNumItems();

        public abstract void EnsureAosData(ReadOnlySpan<byte> raw);
        public abstract void CopyAsSoa(Span<byte> raw, byte[]? soaSource = null);
        public abstract void RemoveAosData();
        public abstract void Copy(Span<byte> raw, byte[]? soaSource = null);
        public abstract MeshSegment Clone();
        public abstract MeshSegment CloneWith(int offset);
        public abstract Type GetElementType();

        protected abstract int GetNumItems();
    }

    internal class MeshSegment<T> : MeshSegment 
        where T : struct
    {
        public MeshSegment()
        {
            AosData = new List<T>();
            IsDirty = true;
            structSize = Marshal.SizeOf(typeof(T));

            if (typeof(T) == typeof(float)) structElemCount = 1;
            else if (typeof(T) == typeof(Vector2)) structElemCount = 2;
            else if (typeof(T) == typeof(Vector3)) structElemCount = 3;
            else if (typeof(T) == typeof(Vector4)) structElemCount = 4;
            else if (typeof(T) == typeof(FaceIndices)) structElemCount = 3;
            else throw new InvalidOperationException();
        }

        internal MeshSegment(int offs, int count)
        {
            Offset = offs;
            numItems = count;
            structSize = Marshal.SizeOf(typeof(T));

            if (typeof(T) == typeof(float)) structElemCount = 1;
            else if (typeof(T) == typeof(Vector2)) structElemCount = 2;
            else if (typeof(T) == typeof(Vector3)) structElemCount = 3;
            else if (typeof(T) == typeof(Vector4)) structElemCount = 4;
            else if (typeof(T) == typeof(FaceIndices)) structElemCount = 3;
            else throw new InvalidOperationException();
        }

        public List<T>? AosData { get; set; }

        public override void EnsureAosData(ReadOnlySpan<byte> raw)
        {
            if (AosData is not null) 
                return;

            ReadOnlySpan<byte> seg = raw.Slice(Offset, TotalLength);
            AosData = new List<T>(MeshUtils.CopySoaToAos<T>(seg) ?? throw new InvalidOperationException());
            IsDirty = true;
        }

        public override void RemoveAosData()
        {
            AosData = null;
        }

        public override void CopyAsSoa(Span<byte> raw, byte[]? soaSource = null)
        {
            if (AosData is not null)
            {
                MeshUtils.CopyAosToSoa<T>(raw, CollectionsMarshal.AsSpan(AosData));
            }
            else if (soaSource is not null)
            {
                soaSource.AsSpan().Slice(Offset, TotalLength).CopyTo(raw);
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        public override void Copy(Span<byte> raw, byte[]? soaSource = null)
        {
            if (AosData is not null)
            {
                MemoryMarshal.Cast<T, byte>(CollectionsMarshal.AsSpan(AosData)).CopyTo(raw);
            }
            else if (soaSource is not null)
            {
                MeshUtils.CopySoaToAos<T>(raw, soaSource.AsSpan(Offset, TotalLength));
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        public override Type GetElementType()
        {
            return typeof(T);
        }

        public override MeshSegment Clone()
        {
            MeshSegment<T> ret = new MeshSegment<T>();
            ret.Offset = Offset;
            ret.numItems = GetNumItems();
            ret.AosData = null;
            return ret;
        }

        public override MeshSegment CloneWith(int offset)
        {
            MeshSegment<T> ret = new MeshSegment<T>();
            ret.Offset = offset;
            ret.numItems = GetNumItems();
            ret.AosData = null;

            return ret;
        }
        
        protected override int GetNumItems()
        {
            if (AosData is not null)
                return AosData.Count;

            return numItems;
        }

        public override string ToString()
        {
            return string.Format("{0}x {1}", numItems, typeof(T).Name);
        }
    }*/
}
