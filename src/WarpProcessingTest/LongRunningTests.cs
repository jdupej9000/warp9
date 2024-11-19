using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Warp9.Jobs;
using Warp9.Model;
using Warp9.Processing;

namespace Warp9.Test
{
    [TestClass]
    public class LongRunningTests
    {
        private static string GetExternalDependency(string fileName)
        {
            string path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string ret = Path.Combine(path, fileName);

            if (!File.Exists(ret))
                Assert.Inconclusive("External dependency could not be found.");

            return ret;
        }

        [TestMethod]
        public void SkullsCpdDca()
        {
            string skullsFile = GetExternalDependency("skulls.w9");

            using Warp9ProjectArchive archive = new Warp9ProjectArchive(skullsFile, false);
            using Project project = Project.Load(archive);

            DcaConfiguration cfg = new DcaConfiguration();
            cfg.SpecimenTableKey = 81;
            cfg.LandmarkColumnName = "Landmarks";
            cfg.MeshColumnName = "Mesh";
            cfg.RigidPreregistration = DcaRigidPreregKind.LandmarkFittedGpa;
            cfg.NonrigidRegistration = DcaNonrigidRegistrationKind.LowRankCpd;
            cfg.SurfaceProjection = DcaSurfaceProjectionKind.ClosestPoint;
            cfg.RigidPostRegistration = DcaRigidPostRegistrationKind.GpaOnWhitelisted;
            cfg.BaseMeshIndex = 0;

            IEnumerable<ProjectJobItem> jobs = DcaJob.Create(cfg);
            // TODO: bring job, job context and engine into WarpProcessing
        }
    }
}
