using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Warp9.Analysis.Meshes
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
        public DcaRigidPreregKind RigidPreregistration { get; set; }
        public DcaNonrigidRegistrationKind NonrigidRegistration { get; set; }
        public DcaSurfaceProjectionKind SurfaceProjection { get; set; }
        public DcaRigidPostRegistrationKind RigidPostRegistration { get; set; }
    }
}
