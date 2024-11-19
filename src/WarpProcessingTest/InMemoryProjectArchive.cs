using System.Text;
using Warp9.Model;

namespace Warp9.Test
{
    public class NotifyingStream : MemoryStream
    {
        public NotifyingStream(Action<byte[]>? onClose = null)
        {
            this.onClose = onClose;
        }

        Action<byte[]>? onClose;

        public override void Close()
        {
            if (onClose is not null) onClose(ToArray());

            base.Close();
        }
    }

    public class InMemoryProjectArchive : IProjectArchive
    {
        Dictionary<string, byte[]> files = new Dictionary<string, byte[]>();

        public bool IsOpen { get; private set; } = true;

        public string WorkingDirectory => string.Empty;

        public void Close()
        {
            IsOpen = false;
        }

        public bool ContainsFile(string name)
        {
            return files.ContainsKey(name);
        }

        public void CopyFileFrom(string name, IProjectArchive other)
        {
            using Stream srcStream = other.OpenFile(name);

            byte[] data = new byte[srcStream.Length];
            srcStream.Read(data.AsSpan());
            files[name] = data;
        }

        public Stream CreateFile(string name)
        {
            return new NotifyingStream((data) => files[name] = data);
        }

        public void Dispose()
        {
            Close();
        }

        public Stream OpenFile(string name)
        {
            if(files.TryGetValue(name, out byte[]? data))
                return new MemoryStream(data);

            throw new InvalidOperationException();
        }

        public string ReadFileAsString(string name)
        {
            if (files.TryGetValue(name, out byte[]? data))
                return Encoding.ASCII.GetString(data);

            throw new IndexOutOfRangeException();
        }
    }
}
