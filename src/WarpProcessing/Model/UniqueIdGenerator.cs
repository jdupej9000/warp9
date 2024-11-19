using System.Text.Json.Serialization;
using System.Threading;

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
