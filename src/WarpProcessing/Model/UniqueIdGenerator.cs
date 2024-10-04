using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Warp9.Model
{
    public class UniqueIdGenerator
    {
        public UniqueIdGenerator()
        {
            counter = 0;
        }

        [JsonInclude, JsonPropertyName("counter")]
        public long counter;

        public long Next()
        {
            return Interlocked.Increment(ref counter);
        }
    }
}
