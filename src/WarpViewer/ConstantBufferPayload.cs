using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Warp9.Viewer
{
    public class ConstantBufferPayload
    {
        protected int structSize;
        protected byte[]? rawData;

        public int StructSize => structSize;
        public byte[] RawData => rawData!;

        public void Set<T>(T value) where T : struct
        {
            if (this is ConstantBufferPayload<T>)
            {
                ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref value, 1);
                MemoryMarshal.Cast<T, byte>(span).CopyTo(rawData.AsSpan());
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        public T Get<T>() where T : struct
        {
            if (this is ConstantBufferPayload<T>)
            {
                return MemoryMarshal.Cast<byte, T>(rawData.AsSpan())[0];
            }

            throw new InvalidOperationException();
        }
    }

    public class ConstantBufferPayload<T> : ConstantBufferPayload
        where T : struct
    {
        public ConstantBufferPayload()
        {
            structSize = Marshal.SizeOf<T>();
            rawData = new byte[structSize];
        }

        public ConstantBufferPayload(T value)
        {
            structSize = Marshal.SizeOf<T>();
            rawData = new byte[structSize];
            ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref value, 1);
            MemoryMarshal.Cast<T, byte>(span).CopyTo(rawData.AsSpan());
        }
    };
}
