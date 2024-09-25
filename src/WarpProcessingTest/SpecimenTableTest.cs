using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        [TestMethod]
        public void CollectTest()
        {
            SpecimenTable tab1 = new SpecimenTable();
            SpecimenTableColumn<long> col1 = tab1.AddColumn<long>("id", SpecimenTableColumnType.Integer);
            col1.Data.AddRange([1L, 10L, 100L, 1000L]);

            SpecimenTable tab2 = new SpecimenTable();
            SpecimenTableColumn<string> col2 = tab2.AddColumn<string>("name", SpecimenTableColumnType.String);
            col2.Data.AddRange(["one", "two", "three", "four"]);

            SpecimenTable tab = SpecimenTable.Collect(false, tab1, tab2);
            Assert.AreEqual(2, tab.Columns.Count);
            Assert.AreEqual(SpecimenTableColumnType.Integer, tab.Columns["id"].ColumnType);
            Assert.AreEqual(SpecimenTableColumnType.String, tab.Columns["name"].ColumnType);
        }

        [TestMethod]
        public void CollectIrregularTest()
        {
            SpecimenTable tab1 = new SpecimenTable();
            SpecimenTableColumn<long> col1 = tab1.AddColumn<long>("id", SpecimenTableColumnType.Integer);
            col1.Data.AddRange([1L, 10L, 100L, 1000L]);

            SpecimenTable tab2 = new SpecimenTable();
            SpecimenTableColumn<string> col2 = tab2.AddColumn<string>("name", SpecimenTableColumnType.String);
            col2.Data.AddRange(["one", "two", "three"]);

            Assert.ThrowsException<InvalidOperationException>(() => SpecimenTable.Collect(false, tab1, tab2));
        }
    }
}
