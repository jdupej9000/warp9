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
        public void AddGetRemoveDirectReferenceTest()
        {
            using Project project = Project.CreateEmpty();

            using Bitmap bmp = new Bitmap(16, 16);
            int bitmapIndex = project.AddReferenceDirect("bitmap.png", ProjectReferenceFormat.PngImage, bmp);
            Assert.AreEqual(0, bitmapIndex);

            Assert.IsTrue(project.TryGetReference(bitmapIndex, out Bitmap? bmp2));
            Assert.IsNotNull(bmp2);
            Assert.AreEqual(bmp, bmp2);

            Assert.IsTrue(project.RemoveReference(bitmapIndex));
            Assert.IsFalse(project.TryGetReference(bitmapIndex, out Bitmap? _));
        }

        [TestMethod]
        public void SaveEmptyProjectTest()
        {
            using Project project = Project.CreateEmpty();

            using InMemoryProjectArchive archive = new InMemoryProjectArchive();
            project.Save(archive);

            string manifestContents = archive.ReadFileAsString("manifest.json");
            Assert.IsTrue(manifestContents.Length > 0);
            Assert.IsTrue(manifestContents.Contains("version"));
            Assert.IsTrue(manifestContents.Contains("settings"));
            Assert.IsTrue(manifestContents.Contains("entries"));
            Assert.IsTrue(manifestContents.Contains("refs"));
        }

        [TestMethod]
        public void SaveProjectCommentTest()
        {
            const string Comment = "nuqneH";

            using Project project = Project.CreateEmpty();
            project.Settings.Comment = Comment;

            using InMemoryProjectArchive archive = new InMemoryProjectArchive();
            project.Save(archive);

            string manifestContents = archive.ReadFileAsString("manifest.json");
            Assert.IsTrue(manifestContents.Contains(Comment));
        }

        [TestMethod]
        public void SaveProject1Test()
        {
            using Project project = Project.CreateEmpty();

            using Bitmap bmp = new Bitmap(16, 16);
            int bitmapIndex = project.AddReferenceDirect("bitmap.png", ProjectReferenceFormat.PngImage, bmp);

            using InMemoryProjectArchive archive = new InMemoryProjectArchive();
            project.Save(archive);

            string manifestContents = archive.ReadFileAsString("manifest.json");
            Assert.IsTrue(manifestContents.Length > 0);

            Assert.IsTrue(archive.ContainsFile("bitmap.png"));
        }

    }
}