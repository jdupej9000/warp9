using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Warp9.Model
{
    public class ProjectSettings
    {
        [JsonPropertyName("comment"), JsonIgnore(Condition=JsonIgnoreCondition.WhenWritingNull)]
        public string? Comment { get; set; }
    }
}
