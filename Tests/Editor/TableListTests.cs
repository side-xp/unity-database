using System.Collections.Generic;
using System.Linq;

using NUnit.Framework;

using SideXP.Database.EditorOnly;

namespace SideXP.Database.Tests
{

    /// <summary>
    /// Tests for the search of the table list.
    /// </summary>
    public class TableListTests
    {

        #region Fields

        /// <summary>
        /// The tables searched by the tests: <i>Tests/Items/All</i>, <i>Tests/Items/Consumable SO</i>, <i>Tests/Items/Equipment</i> and
        /// <i>Tests/Columns</i>, in that order.
        /// </summary>
        private List<TableDefinition> _tables = null;

        #endregion


        #region Lifecycle

        /// <summary>
        /// Builds the tables searched by the tests.
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            List<string> warnings = new List<string>();
            _tables = new List<TableDefinition>
            {
                CodeTables.Build(typeof(ItemSO), warnings),
                CodeTables.Build(typeof(ConsumableSO), warnings),
                CodeTables.Build(typeof(EquipmentSO), warnings),
                CodeTables.Build(typeof(ColumnsSO), warnings),
            };
        }

        #endregion


        #region Search

        /// <summary>
        /// An empty or blank search matches all the tables, in their original order.
        /// </summary>
        /// <param name="search">The search text.</param>
        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void FilterTables_EmptySearch_ReturnsAllTables(string search)
        {
            CollectionAssert.AreEqual(_tables, TableListPage.FilterTables(_tables, search));
        }

        /// <summary>
        /// A search matches the tables whose name contains the text, ignoring case and surrounding spaces.
        /// </summary>
        [Test]
        public void FilterTables_PartOfName_IgnoresCase()
        {
            List<TableDefinition> tables = TableListPage.FilterTables(_tables, " items/E ");

            CollectionAssert.AreEqual(new[] { "Tests/Items/Equipment" }, tables.Select(i => i.Name));
        }

        /// <summary>
        /// A search matches the groups in table names.
        /// </summary>
        [Test]
        public void FilterTables_Group_MatchesAllTablesOfGroup()
        {
            List<TableDefinition> tables = TableListPage.FilterTables(_tables, "items/");

            Assert.AreEqual(3, tables.Count);
        }

        /// <summary>
        /// A search that matches no table name returns no table.
        /// </summary>
        [Test]
        public void FilterTables_NoMatch_ReturnsNoTable()
        {
            CollectionAssert.IsEmpty(TableListPage.FilterTables(_tables, "unknown"));
        }

        #endregion

    }

}
