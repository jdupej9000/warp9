using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;
using Warp9.Model;

namespace Warp9.Processing
{
    public class DiffMatrixConfiguration
    {
        [JsonPropertyName("parent-key")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public long ParentEntityKey { get; set; }

        [JsonPropertyName("parent-column")]
        public string ParentColumnName { get; set; } = ModelConstants.CorrespondencePclColumnName;

        [JsonPropertyName("restore-size")]
        public bool RestoreSize { get; set; } = false;

        [JsonPropertyName("size-column")]
        public string? ParentSizeColumn { get; set; } = null;

        [JsonPropertyName("rejection-mode")]
        public PcaRejectionMode RejectionMode { get; set; } = PcaRejectionMode.None;

        [JsonPropertyName("rejection-thresh")]
        public float RejectionThreshold { get; set; } = 0.05f;

        [JsonPropertyName("methods")]
        public int[] Methods { get; set; } = Array.Empty<int>();

        [JsonIgnore]
        public string ResultEntityName { get; set; } = string.Empty;

    }
}
