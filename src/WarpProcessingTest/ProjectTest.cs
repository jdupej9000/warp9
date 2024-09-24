using Warp9.Model;

namespace Warp9.Test
{
    [TestClass]
    public class ProjectTest
    {
        [TestMethod]
        public void CreateEmptyTest()
        {
            Project project = Project.CreateEmpty();
        }
    }
}