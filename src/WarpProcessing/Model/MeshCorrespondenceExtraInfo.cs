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

        [JsonPropertyName("dca-base-corr")]
        public long BaseMeshCorrKey { get; set; }

        [JsonPropertyName("dca-mean-lms")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public long MeanLandmarksKey { get; set; }
    }
}
