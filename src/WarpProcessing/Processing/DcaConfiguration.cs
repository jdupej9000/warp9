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
        public int NumSpecimens { get; set; }

        public DcaRigidPreregKind RigidPreregistration { get; set; }
        public DcaNonrigidRegistrationKind NonrigidRegistration { get; set; }
        public DcaSurfaceProjectionKind SurfaceProjection { get; set; }
        public DcaRigidPostRegistrationKind RigidPostRegistration { get; set; }
    }
}
