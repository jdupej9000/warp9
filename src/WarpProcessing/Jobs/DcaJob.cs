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
            switch (cfg.RigidPreregistration)
            {
                case DcaRigidPreregKind.None:
                    break;

                case DcaRigidPreregKind.LandmarkFittedGpa:
                    yield return new LandmarkGpaJobItem(cfg.SpecimenTableKey, 
                        cfg.LandmarkColumnName ?? throw new InvalidOperationException(), 
                        GpaPreregKey, JobItemFlags.FailuesAreFatal | JobItemFlags.BlocksNext,
                        null);
                    break;

                default:
                    throw new NotImplementedException();
            }

            // TODO
        }
    }
}
