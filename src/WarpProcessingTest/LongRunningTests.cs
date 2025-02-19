using System.Drawing;
using System.Numerics;
using Warp9.Data;
using Warp9.JobItems;
using Warp9.Jobs;
using Warp9.Model;
using Warp9.Processing;
using Warp9.Viewer;

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

        private static Mesh GetMeshFromProject(Project proj, long specTableKey, string columnName, int index)
        {
            SpecimenTableColumn<ProjectReferenceLink>? column = ModelUtils.TryGetSpecimenTableColumn<ProjectReferenceLink>(
               proj, specTableKey, columnName);

            if (column is null)
                Assert.Fail("Column is missing in project.");

            if (column.ColumnType != SpecimenTableColumnType.Mesh)
                Assert.Fail("Column does not contain meshes.");

            Mesh? ret = ModelUtils.LoadSpecimenTableRef<Mesh>(proj, column, index);
            Assert.IsNotNull(ret);

            return ret;
        }

        [TestMethod]
        public void FacesCpdDcaTest()
        {
            string facesFile = GetExternalDependency("faces.w9");

            using Warp9ProjectArchive archive = new Warp9ProjectArchive(facesFile, false);
            using Project project = Project.Load(archive);

            DcaConfiguration cfg = new DcaConfiguration();
            cfg.SpecimenTableKey = 21;
            cfg.LandmarkColumnName = "Landmarks";
            cfg.MeshColumnName = "Model";
            cfg.RigidPreregistration = DcaRigidPreregKind.LandmarkFittedGpa;
            cfg.NonrigidRegistration = DcaNonrigidRegistrationKind.LowRankCpd;
            cfg.SurfaceProjection = DcaSurfaceProjectionKind.RaycastWithFallback;
            cfg.RigidPostRegistration = DcaRigidPostRegistrationKind.Gpa;
            cfg.BaseMeshIndex = 0;
            cfg.CpdConfig.UseGpu = true;

            IEnumerable<ProjectJobItem> jobItems = DcaJob.Create(cfg, project, true);
            ProjectJobContext jobCtx = new ProjectJobContext(project);
            Job job = Job.Create(jobItems, jobCtx);

            IJobContext ctx = JobEngine.RunImmediately(job);

            //Assert.AreEqual(0, job.NumItemsFailed);
            Assert.AreEqual(job.NumItemsDone, job.NumItems);

            Console.WriteLine("Workspace contents: ");
            foreach (var kvp in ctx.Workspace.Items)
                Console.WriteLine("   " + kvp.Key + " = " + kvp.Value.ToString());

            if (!ctx.Workspace.TryGet("corr.reg", out List<PointCloud>? corrPcls) ||
                corrPcls is null)
                Assert.Fail("corr.reg is not present in the workspace");

            if (!ctx.Workspace.TryGet("rigid.reg", out List<Mesh>? rigidPcls) ||
               corrPcls is null)
                Assert.Fail("corr.reg is not present in the workspace");

            if (!ctx.Workspace.TryGet("corr.reject", out DcaVertexRejection? rej) ||
              corrPcls is null)
                Assert.Fail("corr.reject is not present in the workspace");

            Console.WriteLine("Rejections: " + string.Join(", ", rej.MeshRejections.Select((i) => i.ToString())));

            Mesh baseMesh = GetMeshFromProject(project, cfg.SpecimenTableKey, cfg.MeshColumnName, cfg.BaseMeshIndex);
            HeadlessRenderer rend = TestUtils.CreateRenderer();
            rend.RasterFormat = new RasterInfo(1024, 1024);
            Matrix4x4 modelMat = Matrix4x4.CreateTranslation(-0.75f, -1.0f, -1.0f);
            for (int i = 0; i < corrPcls.Count; i++)
            {
                TestUtils.Render(rend, $"FacesCpdDcaTest_{i}.png", modelMat,
                    //new TestRenderItem(TriStyle.MeshFilled, Mesh.FromPointCloud(corrPcls[i], baseMesh), col:Color.Gray),
                    new TestRenderItem(TriStyle.MeshWire, rigidPcls[i], wireCol:Color.DodgerBlue));
            }
           
        }
    }
}
