using System.Text.Json.Serialization;

namespace Warp9.Model
{
    public readonly struct ProjectReferenceLink(long idx)
    {
        [JsonPropertyName("ref")]
        public long ReferenceIndex { get; private init; } = idx;
    }
}
