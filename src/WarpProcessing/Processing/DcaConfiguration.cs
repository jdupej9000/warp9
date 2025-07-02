using System.Text.Json.Serialization;
using Warp9.Native;

namespace Warp9.Processing
{
    public enum DcaRigidPreregKind
    {
        None = 0,
        LandmarkFittedGpa
    }

    public enum DcaNonrigidRegistrationKind
    {
        None = 0,
        LandmarkFittedTps,
        LowRankCpd
    }

    public enum DcaSurfaceProjectionKind
    {
        None = 0,
        ClosestPoint,
        RaycastWithFallback
    }

    public enum DcaRigidPostRegistrationKind
    {
        None = 0,
        Gpa,
        GpaOnWhitelisted
    }

    public class DcaConfiguration
    {
        [JsonPropertyName("spec-table-entry")]
        public long SpecimenTableKey { get; set; }

        [JsonPropertyName("col-lms")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? LandmarkColumnName { get; set; }

        [JsonPropertyName("col-mesh")]
        public string? MeshColumnName { get; set; }

        [JsonPropertyName("base-idx")]
        public int BaseMeshIndex { get; set; }

        [JsonPropertyName("base-optimize")]
        public bool BaseMeshOptimize {get; set; }

        [JsonIgnore]
        public string ResultEntryName { get; set; } = "DCA";

        [JsonPropertyName("rigid-prereg")]
        public DcaRigidPreregKind RigidPreregistration { get; set; } = DcaRigidPreregKind.LandmarkFittedGpa;

        [JsonPropertyName("nonrigid")]
        public DcaNonrigidRegistrationKind NonrigidRegistration { get; set; } = DcaNonrigidRegistrationKind.LowRankCpd;

        [JsonPropertyName("projection")]
        public DcaSurfaceProjectionKind SurfaceProjection { get; set; } = DcaSurfaceProjectionKind.RaycastWithFallback;

        [JsonPropertyName("rigid-postreg")]
        public DcaRigidPostRegistrationKind RigidPostRegistration { get; set; } = DcaRigidPostRegistrationKind.Gpa;

        [JsonPropertyName("reject-dist")]
        public bool RejectDistant { get; set; } = true;

        [JsonPropertyName("reject-exp")]
        public bool RejectExpanded { get; set; } = true;

        [JsonPropertyName("reject-dist-thresh")]
        public float RejectDistanceThreshold { get; set; } = 0.1f;

        [JsonPropertyName("reject-exp-low-thresh")]
        public float RejectExpandedLowThreshold { get; set; } = 0.1f;

        [JsonPropertyName("reject-exp-high-thresh")]
        public float RejectExpandedHighThreshold { get; set; } = 10f;

        [JsonPropertyName("reject-count-percent")]
        public float RejectCountPercent { get; set; } = 5;

        [JsonPropertyName("cpd-config")]
        public CpdConfiguration CpdConfig { get; set; } = new CpdConfiguration();
    }
}
