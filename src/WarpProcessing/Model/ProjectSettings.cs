using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Warp9.Model
{
    public enum ProjectExternalReferencePolicy
    {
        KeepExternalAbsolutePaths = 0,
        KeepExternalRelativePaths,
        ConvertToInternal
    }

    public class ProjectSettings
    {
        [JsonPropertyName("comment"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Comment { get; set; } = "Write comment here.";

        [JsonPropertyName("ext-reference-policy")]
        public ProjectExternalReferencePolicy ExternalReferencePolicy { get; set; } = ProjectExternalReferencePolicy.KeepExternalRelativePaths;

    }
}
