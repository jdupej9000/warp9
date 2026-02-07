using System;
using System.Collections.Generic;
using Warp9.JobItems;
using Warp9.Model;
using Warp9.Processing;

namespace Warp9.Jobs
{
    public static class DcaJob
    {
        private static readonly string BaseMeshKey = "base";
        private static readonly string GpaPreregKey = "rigid";
        private static readonly string GpaPreregMeshKey = "rigid.reg";
        private static readonly string NonrigidInitKey = "nonrigid.init";
        private static readonly string NonrigidRegKey = "nonrigid.reg";
        private static readonly string CorrespondenceRegKey = "corr.reg";
        private static readonly string CorrespondenceSizeKey = "corr.size";
        private static readonly string RejectionKey = "corr.reject";
        private static readonly string VertexWhitelistKey = "corr.vxwhlst";
        private static readonly string LogKey = "log";

        public static IEnumerable<ProjectJobItem> Create(DcaConfiguration cfg, Project proj, bool debug=false)
        {
            int index = 0;
            SpecimenTable? specTable = ModelUtils.TryGetSpecimenTable(proj, cfg.SpecimenTableKey);
            if (specTable == null)
                throw new InvalidOperationException("Cannot find specified specimen table.");

            int numSpecs = specTable.Count;

            string meshColumn = cfg.MeshColumnName ?? throw new InvalidOperationException();
            int baseMeshIndex = cfg.BaseMeshIndex;
            string? gpaRegItem = null;

            switch (cfg.RigidPreregistration)
            {
                case DcaRigidPreregKind.None:
                    break;

                case DcaRigidPreregKind.LandmarkFittedGpa:
                    yield return new LandmarkGpaJobItem(index++, cfg.SpecimenTableKey,
                        cfg.LandmarkColumnName ?? throw new InvalidOperationException(),
                        GpaPreregKey, CorrespondenceSizeKey, LogKey, null, cfg.RigidPreregistrationSubset);
                    gpaRegItem = GpaPreregKey;
                    break;

                default:
                    throw new NotImplementedException();
            }

            // TODO: warn if this is used with TPS registration, or move landmarks to the optimized mesh.
            yield return new DcaBaseMeshItem(index++,
                cfg.SpecimenTableKey, meshColumn, baseMeshIndex, 
                gpaRegItem, 
                BaseMeshKey, cfg.BaseMeshOptimize);

            bool baseSpecimenReused = !cfg.BaseMeshOptimize;

            if (debug)
            {
                for (int i = 0; i < numSpecs; i++)
                    yield return new SingleRigidRegJobItem(index++, cfg.SpecimenTableKey, gpaRegItem, meshColumn, i, GpaPreregMeshKey, true);
            }

            switch (cfg.NonrigidRegistration)
            {
                case DcaNonrigidRegistrationKind.None:
                    for (int i = 0; i < numSpecs; i++)
                        yield return new SingleRigidRegJobItem(index++, cfg.SpecimenTableKey, gpaRegItem, meshColumn, i, NonrigidRegKey);
                    break;

                case DcaNonrigidRegistrationKind.LandmarkFittedTps:
                    throw new NotImplementedException();

                case DcaNonrigidRegistrationKind.LowRankCpd:
                    yield return new CpdInitJobItem(index++, cfg.SpecimenTableKey, gpaRegItem, BaseMeshKey, meshColumn, NonrigidInitKey, cfg.CpdConfig);
                    yield return new SingleRigidRegJobItem(index++, cfg.SpecimenTableKey, gpaRegItem, meshColumn, baseMeshIndex, NonrigidRegKey);
                    for (int i = 0; i < numSpecs; i++)
                    {
                        if (i != baseMeshIndex || !baseSpecimenReused)
                            yield return new CpdRegJobItem(index++, cfg.SpecimenTableKey, gpaRegItem, NonrigidInitKey, meshColumn, i, LogKey, NonrigidRegKey);
                    }
                    break;

                default:
                    throw new NotImplementedException();
            }

            yield return new BarrierJobItem(index++);

            switch (cfg.SurfaceProjection)
            {
                case DcaSurfaceProjectionKind.None:
                    yield return new CopyJobItem(index++, NonrigidRegKey, CorrespondenceRegKey);
                    break;

                case DcaSurfaceProjectionKind.ClosestPoint:
                case DcaSurfaceProjectionKind.RaycastWithFallback:

                    if(baseSpecimenReused)
                        yield return new SingleRigidRegJobItem(index++, cfg.SpecimenTableKey, gpaRegItem, meshColumn, baseMeshIndex, CorrespondenceRegKey);

                    for (int i = 0; i < numSpecs; i++)
                    {
                        if (i != baseMeshIndex || !baseSpecimenReused)
                            yield return new SurfaceProjectionJobItem(index++, cfg.SpecimenTableKey, meshColumn, i, baseMeshIndex, NonrigidRegKey, gpaRegItem, cfg.SurfaceProjection== DcaSurfaceProjectionKind.RaycastWithFallback, CorrespondenceRegKey);
                    }
                    break;

                default:
                    throw new NotImplementedException();
            }

            if (cfg.RejectDistant || cfg.RejectExpanded)
            {
                yield return new DcaRejectionJobItem(index++, cfg.SpecimenTableKey, cfg.MeshColumnName, baseMeshIndex,
                    CorrespondenceRegKey, cfg, RejectionKey, VertexWhitelistKey);

            }

            switch (cfg.RigidPostRegistration)
            {
                case DcaRigidPostRegistrationKind.None:
                    break;

                case DcaRigidPostRegistrationKind.Gpa:
                    yield return new PclGpaJobItem(index++, CorrespondenceRegKey, CorrespondenceSizeKey, CorrespondenceRegKey);
                    break;

                case DcaRigidPostRegistrationKind.GpaOnWhitelisted:
                    throw new NotImplementedException();

                default:
                    throw new NotImplementedException();
            }

            if(!debug)
                yield return new WorkspaceCleanupJobItem(index++, NonrigidInitKey, NonrigidRegKey);

            yield return new DcaToProjectJobItem(index++, cfg.SpecimenTableKey, 
                GpaPreregKey, BaseMeshKey, CorrespondenceRegKey, null, CorrespondenceSizeKey, 
                RejectionKey, VertexWhitelistKey, LogKey,
                cfg.ResultEntryName, cfg);
        }
    }
}
