using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Warp9.Data
{
    public interface IBufferSegment
    {
        public ReadOnlySpan<byte> RawData { get; }
        public int Length { get; }

        nint Lock();
        void Unlock();
    }

    // This is basically a cut-down ArraySegment with the backing array typed as byte[]
    public class BufferSegment<T> : IBufferSegment 
        where T : struct
    {
        public BufferSegment(T[] data)
        {
            typedData = data;
            this.data = null;
            this.length = data.Length * Marshal.SizeOf<T>();
        }

        // Initializes a BufferSegment. offset is in Bytes, while length in units of T
        public BufferSegment(byte[] data, int offset, int length)
        {
            if (data.Length < (offset + length * Marshal.SizeOf<T>()))
                throw new ArgumentOutOfRangeException();

            typedData = null;
            this.data = data;
            this.offset = offset;
            this.length = length * Marshal.SizeOf<T>();
        }

        T[]? typedData;
        readonly byte[]? data;
        readonly int offset, length;
        GCHandle? pin;

        [JsonIgnore]
        public ReadOnlySpan<byte> RawData => (data is not null) ? data.AsSpan(offset, length) : MemoryMarshal.Cast<T, byte>(typedData!.AsSpan());

        [JsonIgnore]
        public ReadOnlySpan<T> Data => MemoryMarshal.Cast<byte, T>(RawData);

        [JsonPropertyName("data")]
        public T[] DataArray
        {
            get { return Data.ToArray(); }
            set { typedData = value; }
        }

        [JsonIgnore]
        public T this[int i] => Data[i];

        [JsonIgnore]
        public int Count => length / Marshal.SizeOf<T>();

        [JsonIgnore]
        public int Length => length;

        [JsonIgnore]
        public static BufferSegment<T> Empty => new BufferSegment<T>(Array.Empty<byte>(), 0, 0);

        public nint Lock()
        {
            if (pin.HasValue)
                throw new InvalidOperationException("Recursive locking is not supported.");

            if (data is not null)
            {
                pin = GCHandle.Alloc(data, GCHandleType.Pinned);
                return pin.Value.AddrOfPinnedObject() + offset;
            }
            else if (typedData is not null)
            {
                pin = GCHandle.Alloc(typedData, GCHandleType.Pinned);
                return pin.Value.AddrOfPinnedObject();
            }

            throw new InvalidOperationException();
        }

        public void Unlock()
        {
            if (!pin.HasValue) 
                throw new InvalidOperationException("Not locked.");

            pin.Value.Free();
            pin = null;
        }
    }
}
