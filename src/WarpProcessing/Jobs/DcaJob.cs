using System;
using System.Collections.Generic;
using Warp9.Processing;

namespace Warp9.Jobs
{
    public static class DcaJob
    {
        private static readonly string GpaPreregKey = "rigid";
        private static readonly string NonrigidInitKey = "nonrigid.init";
        private static readonly string NonrigidRegKey = "nonrigid.reg";
        private static readonly string CorrespondenceRegKey = "corr.reg";

        public static IEnumerable<ProjectJobItem> Create(DcaConfiguration cfg)
        {
            string meshColumn = cfg.MeshColumnName ?? throw new InvalidOperationException();
            int baseMeshIndex = cfg.BaseMeshIndex;
            string? gpaRegItem = null;

            switch (cfg.RigidPreregistration)
            {
                case DcaRigidPreregKind.None:
                    break;

                case DcaRigidPreregKind.LandmarkFittedGpa:
                    yield return new LandmarkGpaJobItem(cfg.SpecimenTableKey,
                        cfg.LandmarkColumnName ?? throw new InvalidOperationException(),
                        GpaPreregKey, null);
                    gpaRegItem = GpaPreregKey;
                    break;

                default:
                    throw new NotImplementedException();
            }

            switch (cfg.NonrigidRegistration)
            {
                case DcaNonrigidRegistrationKind.None:
                    for (int i = 0; i < cfg.NumSpecimens; i++)
                        yield return new SingleRigidRegJobItem(cfg.SpecimenTableKey, gpaRegItem, meshColumn, baseMeshIndex, NonrigidRegKey);
                    break;

                case DcaNonrigidRegistrationKind.LandmarkFittedTps:
                    throw new NotImplementedException();

                case DcaNonrigidRegistrationKind.LowRankCpd:
                    yield return new CpdInitJobItem(cfg.SpecimenTableKey, gpaRegItem, cfg.BaseMeshIndex, meshColumn, NonrigidInitKey);
                    yield return new SingleRigidRegJobItem(cfg.SpecimenTableKey, gpaRegItem, meshColumn, baseMeshIndex, NonrigidRegKey);
                    for (int i = 0; i < cfg.NumSpecimens; i++)
                    {
                        if (i != baseMeshIndex)
                            yield return new CpdRegJobItem(cfg.SpecimenTableKey, gpaRegItem, NonrigidInitKey, meshColumn, i, NonrigidRegKey);
                    }
                    break;

                default:
                    throw new NotImplementedException();
            }

            switch (cfg.SurfaceProjection)
            {
                case DcaSurfaceProjectionKind.None:
                    throw new NotImplementedException();

                case DcaSurfaceProjectionKind.ClosestPoint:
                case DcaSurfaceProjectionKind.RaycastWithFallback:
                    yield return new SingleRigidRegJobItem(cfg.SpecimenTableKey, gpaRegItem, meshColumn, baseMeshIndex, CorrespondenceRegKey);
                    for (int i = 0; i < cfg.NumSpecimens; i++)
                    {
                        if (i != baseMeshIndex)
                            yield return new SurfaceProjectionJobItem(cfg.SpecimenTableKey, meshColumn, i, baseMeshIndex, NonrigidRegKey, gpaRegItem, CorrespondenceRegKey);
                    }
                    break;

                default:
                    throw new NotImplementedException();
            }

            yield return new WorkspaceCleanupJobItem(NonrigidInitKey, NonrigidRegKey);

            // TODO
        }
    }
}
