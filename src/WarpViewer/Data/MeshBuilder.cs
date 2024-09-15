using SharpDX.D3DCompiler;
using System;
using System.Collections.Generic;
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

        internal MeshBuilder(byte[] d, Dictionary<MeshSegmentType, MeshSegment> segs)
        {
            data = d;

            foreach (var kvp in segs)
                segments.Add(kvp.Key, kvp.Value.Clone());
        }

        byte[] data;
        bool segmentsDirty = false;
        Dictionary<MeshSegmentType, MeshSegment> segments = new Dictionary<MeshSegmentType, MeshSegment>();

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

        public void RemoveSegment(MeshSegmentType segType)
        {
            segmentsDirty = true;
            segments.Remove(segType);
        }

        private void ValidateSegments(out int nv)
        {
            if (segments.Count == 0)
            {
                nv = 0;
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

        public Mesh ToMesh()
        {
            ValidateSegments(out int nv);
            ComposeSoa(out byte[] dataRaw, out Dictionary<MeshSegmentType, MeshSegment> newSegments);

            return new Mesh(nv, dataRaw, newSegments);
        }
    }
}
