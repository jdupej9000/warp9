using System.Collections;
using System.Collections.Generic;
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

        [JsonPropertyName("mesh-corr-extra")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public MeshCorrespondenceExtraInfo? MeshCorrExtra { get; set; }

        [JsonPropertyName("pca")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public PcaExtraInfo? PcaExtra { get; set; }

        [JsonPropertyName("diff-matrtix")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public DiffMatrixExtraInfo? DiffMatrixExtra { get; set; }

        [JsonIgnore]
        public static readonly ProjectEntryPayload Empty = new ProjectEntryPayload();

        public IEnumerable<long> GetParentSpecimenTables()
        {
            HashSet<long> ret = new HashSet<long>();

            if (MeshCorrExtra is not null)
                ret.Add(MeshCorrExtra.DcaConfig.SpecimenTableKey);

            return ret;
        }
    }
}
