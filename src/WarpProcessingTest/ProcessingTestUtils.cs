using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Warp9.Data;
using Warp9.Model;
using Warp9.Native;

namespace Warp9.Test
{
    public static class ProcessingTestUtils
    {
        public static void AssertEqual(Rigid3 want, Rigid3 got)
        {
            bool fail = false;

            fail |= (MathF.Abs(want.cs - got.cs) > 1e-4f);
            fail |= (Vector3.Distance(want.offset, got.offset) > 1e-4f);
            fail |= (Vector3.Dot(want.rot0, got.rot0) < 0.99f);
            fail |= (Vector3.Dot(want.rot1, got.rot1) < 0.99f);
            fail |= (Vector3.Dot(want.rot2, got.rot2) < 0.99f);

            if (fail)
            {
                Console.WriteLine("Wanted: " + want.ToString());
                Console.WriteLine("Got   : " + got.ToString());
                Assert.Fail();
            }
        }

        public static void AssertEqual(Vector3 want, Vector3 got)
        {
            if (Vector3.Distance(want, got) > 1e-5f)
            {
                Console.WriteLine("Wanted: " + want.ToString());
                Console.WriteLine("Got   : " + got.ToString());
                Assert.Fail();
            }
        }
        public static string GetExternalDependency(string fileName)
        {
            string path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string ret = Path.Combine(path, fileName);

            if (!File.Exists(ret))
                Assert.Inconclusive("External dependency could not be found.");

            return ret;
        }

        public static Mesh GetMeshFromProject(Project proj, long specTableKey, string columnName, int index)
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

        public static PointCloud GetPointCloudFromProject(Project proj, long specTableKey, string columnName, int index)
        {
            SpecimenTableColumn<ProjectReferenceLink>? column = ModelUtils.TryGetSpecimenTableColumn<ProjectReferenceLink>(
               proj, specTableKey, columnName);

            if (column is null)
                Assert.Fail("Column is missing in project.");

            if (column.ColumnType != SpecimenTableColumnType.PointCloud)
                Assert.Fail("Column does not contain point clouds.");

            PointCloud? ret = ModelUtils.LoadSpecimenTableRef<PointCloud>(proj, column, index);
            Assert.IsNotNull(ret);

            return ret;
        }

    }
}
