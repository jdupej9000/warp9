using Warp9.Model;

namespace Warp9.Test
{
    [TestClass]
    public class SpecimenTableTest
    {
        [TestMethod]
        public void CreateEmptyTest()
        {
            SpecimenTable tab = new SpecimenTable();
            Assert.AreEqual(0, tab.Columns.Count);
        }

        [TestMethod]
        public void AddIntColumnTest()
        {
            SpecimenTable tab = new SpecimenTable();
            SpecimenTableColumn<long> col = tab.AddColumn<long>("id", SpecimenTableColumnType.Integer);
            col.Data.AddRange([1L, 10L, 100L, 1000L]);

            Assert.AreEqual(SpecimenTableColumnType.Integer, tab.Columns["id"].ColumnType);
        }

       
    }
}
