using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;
using Warp9.Processing;

namespace Warp9.Model
{
    public class DiffMatrixExtraInfo
    {
        [JsonPropertyName("diff-data")]
        public long DataKey { get; set; }

        [JsonPropertyName("config")]
        public DiffMatrixConfiguration Config { get; set; } = new DiffMatrixConfiguration();
    }
}
