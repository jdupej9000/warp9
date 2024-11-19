using System.Text.Json.Serialization;

namespace Warp9.Model
{
    public class ProjectEntryPayload
    {
        [JsonPropertyName("text")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Text { get; set; }

        [JsonPropertyName("table")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public SpecimenTable? Table { get; set; }

        [JsonIgnore]
        public static readonly ProjectEntryPayload Empty = new ProjectEntryPayload();
    }
}
