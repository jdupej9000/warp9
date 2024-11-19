using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Warp9.Model
{
    public class ProjectManifest
    {
        public const int CurrentVersion = 1;

        [JsonPropertyName("version")]
        public int Version { get; set; } = CurrentVersion;

        [JsonPropertyName("settings")]
        public ProjectSettings Settings { get; set; } = new ProjectSettings();

        [JsonPropertyName("entries")]
        public Dictionary<long, ProjectEntry> Entries { get; set; } = new Dictionary<long, ProjectEntry>();

        [JsonPropertyName("refs")]
        public Dictionary<long, ProjectReferenceInfo> References { get; set; } = new Dictionary<long, ProjectReferenceInfo>();

        [JsonPropertyName("counters")]
        public Dictionary<string, UniqueIdGenerator> Counters { get; set; } = new Dictionary<string, UniqueIdGenerator>();
    }
}
