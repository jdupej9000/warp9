using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Warp9.Data
{
    public enum MeshSegmentType
    {
        Position,
        Normal,
        Tex0,

        Invalid
    }

    internal abstract class MeshSegment
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
        public abstract void CopyAsSoa(Span<byte> raw);
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

        public override void CopyAsSoa(Span<byte> raw)
        {
            if (AosData is not null)
            {
                MeshUtils.CopyAosToSoa<T>(raw, CollectionsMarshal.AsSpan(AosData));
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
            ret.numItems = numItems;

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
    }
}
