using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Warp9.Model
{
    public class ProjectEntryPayload
    {
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Text { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public SpecimenTable? Table { get; set; }

        [JsonIgnore]
        public static readonly ProjectEntryPayload Empty = new ProjectEntryPayload();
    }
}
