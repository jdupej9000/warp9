using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Warp9.Processing
{
    public enum PcaKind
    {
        DcaVertexPositions = 0
    };

    public enum PcaRejectionMode
    {
        None = 0,
        AsParent = 1,
        CustomThreshold = 2
    };

    public class PcaConfiguration
    {
        [JsonPropertyName("kind")]
        public PcaKind Kind { get; set; }

        [JsonPropertyName("parent-dca")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public long ParentDcaKey { get; set; }

        [JsonPropertyName("restore-size")]
        public bool RestoreSize { get; set; } = false;

        [JsonPropertyName("use-cor")]
        public bool NormalizeScale { get; set; } = true;

        [JsonPropertyName("rejection-mode")]
        public PcaRejectionMode RejectionMode { get; set; } = PcaRejectionMode.AsParent;

        [JsonPropertyName("rejection-thresh")]
        public float RejectionThreshold { get; set; } = 0.05f;

    }
}
