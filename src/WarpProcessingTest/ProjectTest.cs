using System.Drawing;
using Warp9.Model;

namespace Warp9.Test
{
    [TestClass]
    public class ProjectTest
    {
        [TestMethod]
        public void CreateEmptyTest()
        {
            using Project project = Project.CreateEmpty();
            Assert.IsFalse(project.IsArchiveOpen);
        }

        [TestMethod]
        public void AddGetDirectReferenceTest()
        {
            using Project project = Project.CreateEmpty();

            using Bitmap bmp = new Bitmap(16, 16);
            int bitmapIndex = project.AddReferenceDirect("bitmap.png", ProjectReferenceFormat.PngImage, bmp);
            Assert.AreEqual(0, bitmapIndex);

            Assert.IsTrue(project.TryGetReference(bitmapIndex, out Bitmap? bmp2));
            Assert.IsNotNull(bmp2);
            Assert.AreEqual(bmp, bmp2);
        }

        [TestMethod]
        public void SaveEmptyProject()
        {
            using Project project = Project.CreateEmpty();

            using InMemoryProjectArchive archive = new InMemoryProjectArchive();
            project.Save(archive);

            string manifestContents = archive.ReadFileAsString("manifest.json");
            Assert.IsTrue(manifestContents.Length > 0);
        }

    }
}