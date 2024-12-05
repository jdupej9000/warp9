using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Warp9.Processing;

namespace Warp9.Model
{
    public class MeshCorrespondenceExtraInfo
    {
        [JsonPropertyName("dca-info")]
        public required DcaConfiguration DcaConfig { get; set; }
    }
}
