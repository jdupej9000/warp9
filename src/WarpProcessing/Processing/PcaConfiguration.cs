using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Warp9.Model;

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

        [JsonPropertyName("parent-key")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public long ParentEntityKey { get; set; }

        [JsonPropertyName("parent-column")]
        public string ParentColumnName { get; set; } = ModelConstants.CorrespondencePclColumnName;

        [JsonPropertyName("restore-size")]
        public bool RestoreSize { get; set; } = false;

        [JsonPropertyName("size-column")]
        public string? ParentSizeColumn { get; set; } = ModelConstants.CentroidSizeColumnName;

        [JsonPropertyName("use-cor")]
        public bool NormalizeScale { get; set; } = true;

        [JsonPropertyName("rejection-mode")]
        public PcaRejectionMode RejectionMode { get; set; } = PcaRejectionMode.AsParent;

        [JsonPropertyName("rejection-thresh")]
        public float RejectionThreshold { get; set; } = 0.05f;

    }
}
