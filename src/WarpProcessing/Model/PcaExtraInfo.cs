using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Warp9.Processing;

namespace Warp9.Model
{
    public class PcaExtraInfo
    {
        [JsonPropertyName("pca-info")]
        public PcaConfiguration Info { get; set; } = new PcaConfiguration();

        [JsonPropertyName("pca-data")]
        public long DataKey { get; set; }
    }
}
