﻿using SharpDX.D3DCompiler;
using System;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Warp9.Data
{
    public class MeshBuilder
    {
        public MeshBuilder()
        {
            data = Array.Empty<byte>();
        }

        internal MeshBuilder(byte[] vxd, Dictionary<MeshSegmentType, MeshSegment> segs, byte[] ixd, MeshSegment? idxSeg)
        {
            data = vxd;
            indexData = ixd;

            foreach (var kvp in segs)
                segments.Add(kvp.Key, kvp.Value.Clone());

            if (idxSeg is not null)
                indexSegment = idxSeg.Clone();
            else
                indexSegment = null;
        }

        byte[] data, indexData;
        bool segmentsDirty = false, idxSegmentDirty = false;
        Dictionary<MeshSegmentType, MeshSegment> segments = new Dictionary<MeshSegmentType, MeshSegment>();
        MeshSegment? indexSegment;

        public bool IsDirty => segments.Any((t) => t.Value.IsDirty);
        public int BufferSizeNeeded => segments.Sum((t) => t.Value.TotalLength);

        public List<T> GetSegmentForEditing<T>(MeshSegmentType segType) where T : struct
        {
            if (segments.TryGetValue(segType, out MeshSegment? seg) && 
                seg is MeshSegment<T> segTyped)
            {
                segTyped.EnsureAosData(data.AsSpan());
                if (segTyped.AosData is null) 
                    throw new InvalidDataException();

                return segTyped.AosData;
            }

            MeshSegment<T> ret = new MeshSegment<T>();
            if (ret.AosData is null)
                throw new InvalidDataException();

            segments[segType] = ret;
            segmentsDirty = true;

            return ret.AosData;
        }

        public void SetSegment<T>(MeshSegmentType segType, IEnumerable<T> d) where T : struct
        {
            List<T> seg = GetSegmentForEditing<T>(segType);
            seg.Clear();
            seg.AddRange(d);
        }

        public void RemoveSegment(MeshSegmentType segType)
        {
            segmentsDirty = true;
            segments.Remove(segType);
        }

        public List<T> GetIndexSegmentForEditing<T>() where T : struct
        {
            if (indexSegment is MeshSegment<T> iseg)
            {
                iseg.EnsureAosData(indexData.AsSpan());
                if(iseg.AosData is null) 
                    throw new InvalidDataException();

                return iseg.AosData;
            }

            MeshSegment<T> ret = new MeshSegment<T>();
            if(ret.AosData is null)
                throw new InvalidDataException();

            indexSegment = ret;
            idxSegmentDirty = true;

            return ret.AosData;
        }

        public void RemoveIndexSegment()
        {
            idxSegmentDirty = true;
            indexSegment = null;
        }

        private void ValidateSegments(out int nv, out int nt)
        {
            if (segments.Count == 0)
            {
                nv = 0;
                nt = 0;
                return;
            }

            int nv0 = int.MaxValue, nv1 = int.MinValue;
            foreach (var kvp in segments)
            {
                if (nv0 > kvp.Value.NumItems) nv0 = kvp.Value.NumItems;
                if (nv1 < kvp.Value.NumItems) nv1 = kvp.Value.NumItems;
            }

            if (nv0 != nv1)
                throw new InvalidDataException("Vertex counts not identical in all channels.");

            nv = nv0;

            if (indexSegment is not null)
            {
                nt = indexSegment.NumItems;
            }
            else
            {
                nt = 0;
            }
        }

        private void ComposeSoa(out byte[] dataRaw, out Dictionary<MeshSegmentType, MeshSegment> newSegments)
        {
            newSegments = new Dictionary<MeshSegmentType, MeshSegment>();

            if (!segmentsDirty && !IsDirty)
            {   
                foreach (var seg in segments)
                    newSegments.Add(seg.Key, seg.Value.Clone());

                dataRaw = data;
            }
            else
            {
                byte[] ret = new byte[BufferSizeNeeded];

                int offs = 0;
                foreach (var seg in segments)
                {
                    int segSize = seg.Value.TotalLength;
                    newSegments.Add(seg.Key, seg.Value.CloneWith(offs));
                    if (seg.Value.IsDirty)
                    {
                        seg.Value.CopyAsSoa(ret.AsSpan().Slice(offs, segSize));
                    }
                    else
                    {  
                        data.AsSpan(seg.Value.Offset, segSize).CopyTo(ret.AsSpan(offs, segSize));
                    }

                    offs += segSize;
                }

                dataRaw = ret;
            }
        }

        private void ComposeIndices(out byte[] dataRaw, out MeshSegment? seg)
        {
            if (indexSegment is null)
            {
                dataRaw = Array.Empty<byte>();
                seg = null;
                return;
            }

            if (idxSegmentDirty)
            {
                dataRaw = new byte[indexSegment.TotalLength];
                indexSegment.Copy(dataRaw);
                seg = indexSegment.Clone();
            }
            else
            {
                dataRaw = indexData;
                seg = indexSegment.Clone();
            }
        }

        public Mesh ToMesh()
        {
            ValidateSegments(out int nv, out int nt);
            ComposeSoa(out byte[] dataRaw, out Dictionary<MeshSegmentType, MeshSegment> newSegments);
            ComposeIndices(out byte[] indicesRaw, out MeshSegment? newIdxSeg);

            return new Mesh(nv, nt, dataRaw, newSegments, indicesRaw, newIdxSeg);
        }
    }
}
