using System.Buffers;
using System.Drawing;
using System.Numerics;
using Warp9.Data;
using Warp9.JobItems;
using Warp9.Jobs;
using Warp9.Model;
using Warp9.Native;
using Warp9.Processing;
using Warp9.Utils;
using Warp9.Viewer;

namespace Warp9.Test
{
    [TestClass]
    public class LongRunningTests
    {
       
        [TestMethod]
        public void FacesCpdDcaTest()
        {
            string facesFile = ProcessingTestUtils.GetExternalDependency("faces.w9");

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
            cfg.RejectImputation = DcaImputationKind.Tps;
            cfg.RejectExpandedHighThreshold = 5.0f;
            cfg.RejectExpandedLowThreshold = 0.2f;
            cfg.RejectDistanceThreshold = 1.5f;
            cfg.RejectDistant = false;
            cfg.RejectExpanded = true;
            cfg.BaseMeshIndex = 0;
            cfg.BaseMeshOptimize = false;
            cfg.CpdConfig.UseGpu = true;
            cfg.CpdConfig.Beta = 2;
            cfg.CpdConfig.Lambda = 2;


            IEnumerable<ProjectJobItem> jobItems = DcaJob.Create(cfg, project, true);
            ProjectJobContext jobCtx = new ProjectJobContext(project);
            Job job = Job.Create(jobItems, jobCtx);

            IJobContext ctx = JobEngine.RunImmediately(job);

            Console.WriteLine("Workspace contents: ");
            foreach (var kvp in ctx.Workspace.Items)
                Console.WriteLine("   " + kvp.Key + " = " + kvp.Value.ToString());

            if (!ctx.Workspace.TryGet("corr.reg", out List<PointCloud>? corrPcls) ||
                corrPcls is null)
                Assert.Fail("corr.reg is not present in the workspace");

            if (!ctx.Workspace.TryGet("rigid.reg", out List<Mesh>? rigidPcls) ||
                rigidPcls is null)
                Assert.Fail("rigid.reg is not present in the workspace");

            if (!ctx.Workspace.TryGet("corr.reject", out DcaVertexRejection? rej) ||
              corrPcls is null)
                Assert.Fail("corr.reject is not present in the workspace");

            Console.WriteLine("Rejections: " + string.Join(", ", rej.MeshRejections.Select((i) => i.ToString())));

            if (!ctx.Workspace.TryGet("base", out Mesh? baseMesh) || baseMesh is null)
                Assert.Fail("Cannot get base mesh.");

            HeadlessRenderer rend = TestUtils.CreateRenderer();
            rend.RasterFormat = new RasterInfo(1024, 1024);
            Matrix4x4 modelMat = Matrix4x4.CreateTranslation(-0.75f, -1.0f, -1.0f);
            for (int i = 0; i < corrPcls.Count; i++)
            {
                MeshBuilder mb = Mesh.FromPointCloud(corrPcls[i], baseMesh).ToBuilder();
                MeshSegmentBuilder<uint> colorSeg = mb.GetSegmentForEditing<uint>(MeshSegmentSemantic.Color, false);
                uint[] colors = BitMask.Expand(rej.ModelRejectionMask(i), baseMesh.VertexCount,
                    0xff808080, 0xff0000ff);
                    //0xff808080, 0xff808080);
                colorSeg.Data.Clear();
                colorSeg.Data.AddRange(colors);

                TestUtils.Render(rend, $"FacesCpdDcaTest_{i}.png", modelMat,
                    new TestRenderItem(TriStyle.MeshFilled, rigidPcls[i], Color.DodgerBlue),
                    new TestRenderItem(TriStyle.MeshFilledVertexColor, mb.ToMesh()));
            }

            TestUtils.Render(rend, $"FacesCpdDcaTest_base.png", modelMat,
                new TestRenderItem(TriStyle.MeshFilled, baseMesh, col: Color.Gray));

            if (!ctx.Workspace.TryGet("rigid", out Gpa? gpa) ||
                gpa is null)
                Assert.Fail("corr.reg is not present in the workspace");
            List<TestRenderItem> lmrend = new List<TestRenderItem>();
            for (int i = 0; i < corrPcls.Count; i++)            
                lmrend.Add(new TestRenderItem(TriStyle.Landmarks, gpa.GetTransformed(i), col: Color.Yellow, lmScale: 0.01f));
            lmrend.Add(new TestRenderItem(TriStyle.Landmarks, gpa.Mean, col: Color.Red, lmScale: 0.01f));
            TestUtils.Render(rend, $"FacesCpdDcaTest_lmsgpa.png", modelMat, lmrend.ToArray());

            Assert.AreEqual(0, job.NumItemsFailed);
            Assert.AreEqual(job.NumItemsDone, job.NumItems);
        }

        [TestMethod]
        [DataRow(false, false, false)]
        [DataRow(true, false, false)]
        [DataRow(false, true, false)]
        [DataRow(true, true, false)]
        [DataRow(false, false, true)]
        [DataRow(true, false, true)]
        [DataRow(false, true, true)]
        [DataRow(true, true, true)]
        public void FacesPcaTest(bool restoreSize, bool normalizeScale, bool reject)
        {
            string facesFile = ProcessingTestUtils.GetExternalDependency("faces-dca.w9");

            using Warp9ProjectArchive archive = new Warp9ProjectArchive(facesFile, false);
            using Project project = Project.Load(archive);

            SpecimenTableColumn<ProjectReferenceLink>? corrColumn = ModelUtils.TryGetSpecimenTableColumn<ProjectReferenceLink>(
               project, 35, "corrPcl");
            Assert.IsNotNull(corrColumn);

            List<PointCloud?> dcaCorrPcls = ModelUtils.LoadSpecimenTableRefs<PointCloud>(project, corrColumn).ToList();
            Assert.IsFalse(dcaCorrPcls.Exists((t) => t is null));

            int ns = dcaCorrPcls.Count;
            if (restoreSize)
            {
                SpecimenTableColumn<double>? csColumn = ModelUtils.TryGetSpecimenTableColumn<double>(
                  project, 35, "cs");

                if (csColumn is not null)
                {
                    IReadOnlyList<double> cs = csColumn.GetData<double>();
                    for (int i = 0; i < ns; i++)
                        dcaCorrPcls[i] = MeshScaling.ScalePosition(dcaCorrPcls[i]!, (float)cs[i]).ToPointCloud();
                }
            }

            int nv = dcaCorrPcls[0]!.VertexCount;
            bool[] allow = new bool[nv];
            if (reject)
            {
                float thresh = 0.05f;

                if (!project.TryGetReference(32, out MatrixCollection? rejmc) ||
                    rejmc is null ||
                    !rejmc.TryGetMatrix(ModelConstants.VertexRejectionRatesKey, out Matrix<float>? rejectRates) ||
                    rejectRates is null)
                {
                    Assert.Fail();
                    return;
                }

                MiscUtils.ThresholdBelow(rejectRates.Data.AsSpan(), thresh, allow.AsSpan());
            }
            else
            {
                for (int i = 0; i < nv; i++)
                    allow[i] = true;
            }

            Pca? pca = Pca.Fit(dcaCorrPcls!, allow, normalizeScale);
            Assert.IsNotNull(pca);

            int npcs = Math.Min(50, ns - 1);
            Matrix<float> scoresMat = new Matrix<float>(npcs, ns);

            int npcsall = pca.NumPcs;
            float[] scores = ArrayPool<float>.Shared.Rent(npcsall);
            for (int i = 0; i < ns; i++)
            {
                if (dcaCorrPcls[i] is null || !pca.TryGetScores(dcaCorrPcls[i]!, scores.AsSpan()))                
                    Assert.Fail();

                for (int j = 0; j < npcs; j++)
                    scoresMat[i, j] = scores[j];
            }
            ArrayPool<float>.Shared.Return(scores);

            Console.WriteLine(scoresMat.ToString());

            Console.WriteLine("PC variability (%): " +
                string.Join(", ", pca.PcVariance.Select((t) => (t * 100.0f).ToString("F1"))));
        }

        [TestMethod]
        public void MeshSampleDistanceMatrixTest()
        {
            string facesFile = ProcessingTestUtils.GetExternalDependency("faces-dca.w9");
            MeshDistanceKind[] distanceKinds = Enum.GetValues<MeshDistanceKind>();

            using Warp9ProjectArchive archive = new Warp9ProjectArchive(facesFile, false);
            using Project project = Project.Load(archive);

            SpecimenTableColumn<ProjectReferenceLink>? corrColumn = ModelUtils.TryGetSpecimenTableColumn<ProjectReferenceLink>(
              project, 35, "corrPcl");
            Assert.IsNotNull(corrColumn);

            List<PointCloud?> dcaCorrPcls = ModelUtils.LoadSpecimenTableRefs<PointCloud>(project, corrColumn).ToList();
            Assert.IsFalse(dcaCorrPcls.Exists((t) => t is null));

            MatrixCollection mc = MeshDistance.Compute(dcaCorrPcls, null, null, distanceKinds);

            foreach (MeshDistanceKind k in distanceKinds)
            {
                Console.WriteLine(k.ToString());
                Console.WriteLine(mc[(int)k].ToString());
                Console.WriteLine();
            }
        }
    }
}
