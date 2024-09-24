using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

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
        public Dictionary<int, ProjectEntry> Entries { get; set; } = new Dictionary<int, ProjectEntry>();

        [JsonPropertyName("refs")]
        public Dictionary<int, ProjectReferenceInfo> References { get; set; } = new Dictionary<int, ProjectReferenceInfo>();
    }
}
