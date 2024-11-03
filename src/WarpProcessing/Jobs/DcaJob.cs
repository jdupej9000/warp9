using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Warp9.Processing;

namespace Warp9.Jobs
{
    public static class DcaJob
    {
        private static readonly string GpaPreregKey = "rigid";
        private static readonly string NonrigidInitKey = "nonrigid.init";

        public static IEnumerable<ProjectJobItem> Create(DcaConfiguration cfg)
        {
            string meshColumn = cfg.MeshColumnName ?? throw new InvalidOperationException();

            switch (cfg.RigidPreregistration)
            {
                case DcaRigidPreregKind.None:
                    break;

                case DcaRigidPreregKind.LandmarkFittedGpa:
                    yield return new LandmarkGpaJobItem(cfg.SpecimenTableKey, 
                        cfg.LandmarkColumnName ?? throw new InvalidOperationException(), 
                        GpaPreregKey, null);
                    break;

                default:
                    throw new NotImplementedException();
            }

            switch (cfg.NonrigidRegistration)
            {
                case DcaNonrigidRegistrationKind.None:
                    break;

                case DcaNonrigidRegistrationKind.LandmarkFittedTps:
                    break;

                case DcaNonrigidRegistrationKind.LowRankCpd 
                when cfg.RigidPreregistration == DcaRigidPreregKind.None:
                    yield return new CpdInitJobItem(cfg.SpecimenTableKey, cfg.BaseMeshIndex, meshColumn, NonrigidInitKey);
                    break;

                case DcaNonrigidRegistrationKind.LowRankCpd
                when cfg.RigidPreregistration == DcaRigidPreregKind.None:
                    yield return new CpdInitJobItem(cfg.SpecimenTableKey, NonrigidInitKey, cfg.BaseMeshIndex, meshColumn, NonrigidInitKey);
                    break;

                default:
                    throw new NotImplementedException();
            }

            // TODO
        }
    }
}
