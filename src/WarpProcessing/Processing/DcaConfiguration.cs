using Warp9.Native;

namespace Warp9.Processing
{
    public enum DcaRigidPreregKind
    {
        None,
        LandmarkFittedGpa
    }

    public enum DcaNonrigidRegistrationKind
    {
        None,
        LandmarkFittedTps,
        LowRankCpd
    }

    public enum DcaSurfaceProjectionKind
    {
        None,
        ClosestPoint,
        RaycastWithFallback
    }

    public enum DcaRigidPostRegistrationKind
    {
        None,
        Gpa,
        GpaOnWhitelisted
    }

    public class DcaConfiguration
    {
        public long SpecimenTableKey { get; set; }
        public string? LandmarkColumnName { get; set; }
        public string? MeshColumnName { get; set; }
        public int BaseMeshIndex { get; set; }
        public string ResultEntryName { get; set; } = "DCA";

        public DcaRigidPreregKind RigidPreregistration { get; set; } = DcaRigidPreregKind.LandmarkFittedGpa;
        public DcaNonrigidRegistrationKind NonrigidRegistration { get; set; } = DcaNonrigidRegistrationKind.LowRankCpd;
        public DcaSurfaceProjectionKind SurfaceProjection { get; set; } = DcaSurfaceProjectionKind.ClosestPoint;
        public DcaRigidPostRegistrationKind RigidPostRegistration { get; set; } = DcaRigidPostRegistrationKind.Gpa;

        public bool RejectDistant { get; set; } = true;
        public bool RejectExpanded { get; set; } = true;
        public float RejectDistanceThreshold { get; set; } = 0.1f;
        public float RejectExpandedLowThreshold { get; set; } = 0.1f;
        public float RejectExpandedHighThreshold { get; set; } = 10f;
        public float RejectCountPercent { get; set; } = 5;

        public bool RestoreSize {get; set; } = true;

        public CpdConfiguration CpdConfig { get; set; } = new CpdConfiguration();
    }
}
