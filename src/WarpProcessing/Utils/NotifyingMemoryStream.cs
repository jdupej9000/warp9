using System;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Warp9.Utils
{
    public interface IMemoryStreamNotificationSink
    {
        public void OnStreamDisposing(string key);
    }

    public class NotifyingMemoryStream : MemoryStream
    {
        public NotifyingMemoryStream(string key, byte[] data, IMemoryStreamNotificationSink? sink) :
            base(data)
        {
            Key = key;
            this.sink = sink;
        }

        public string Key { get; init; }
        private IMemoryStreamNotificationSink? sink;

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                sink?.OnStreamDisposing(Key);
            }
        }
    }
}
