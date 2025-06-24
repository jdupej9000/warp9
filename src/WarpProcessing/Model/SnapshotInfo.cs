using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Warp9.Scene;

namespace Warp9.Model
{
    public class SnapshotInfo
    {
        [JsonIgnore]
        public long Id { get; set; } = -1;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("filter")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Filter { get; set; } = null;

        [JsonPropertyName("filter")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Comment { get; set; } = null;

        [JsonPropertyName("thumbnail")]
        [JsonIgnore(Condition =JsonIgnoreCondition.WhenWritingDefault)]
        public long ThumbnailKey { get; set; } = 0;

        [JsonPropertyName("scene")]
        public ViewerScene Scene { get; set; } = new ViewerScene();
    }
}
