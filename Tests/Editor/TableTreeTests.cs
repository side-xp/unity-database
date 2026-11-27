using System;
using System.Collections.Generic;
using System.Linq;

using NUnit.Framework;

using SideXP.Database.EditorOnly;

namespace SideXP.Database.Tests
{

    /// <summary>
    /// Tests for the tree of the table list, which groups the tables by the <c>/</c> in their names.
    /// </summary>
    public class TableTreeTests
    {

        #region Name parts

        /// <summary>
        /// The parts of a name are trimmed, and the empty parts are skipped.
        /// </summary>
        /// <param name="name">The name of the table.</param>
        /// <param name="expectedParts">The expected parts, joined with <c>|</c>.</param>
        [TestCase("Weapons", "Weapons")]
        [TestCase("Items/Weapons", "Items|Weapons")]
        [TestCase(" Items / /Weapons/ ", "Items|Weapons")]
        [TestCase("/Items", "Items")]
        public void GetNameParts_Separators_TrimmedPartsWithoutEmptyOnes(string name, string expectedParts)
        {
            CollectionAssert.AreEqual(expectedParts.Split('|'), TableTree.GetNameParts(name));
        }

        /// <summary>
        /// A name with no part left is kept as a single part, so the table still shows.
        /// </summary>
        /// <param name="name">The name of the table.</param>
        [TestCase("/")]
        [TestCase(" / ")]
        [TestCase("")]
        public void GetNameParts_NoPartLeft_WholeName(string name)
        {
            CollectionAssert.AreEqual(new[] { name }, TableTree.GetNameParts(name));
        }

        #endregion


        #region Build

        /// <summary>
        /// No table gives no item.
        /// </summary>
        [Test]
        public void Build_NoTable_NoItem()
        {
            CollectionAssert.IsEmpty(TableTree.Build(new TableDefinition[0]));
        }

        /// <summary>
        /// Tables without group are at the root, sorted by name ignoring case, whatever the order they are given in.
        /// </summary>
        [Test]
        public void Build_NoGroup_TablesAtRootSortedByName()
        {
            List<TableTreeNode> items = TableTree.Build(CreateTables("Weapons", "armors", "Consumables"));

            CollectionAssert.AreEqual(new[] { "armors", "Consumables", "Weapons" }, items.Select(i => i.Name));
            Assert.IsTrue(items.All(i => !i.IsGroup));
            CollectionAssert.AreEqual(new[] { "armors", "Consumables", "Weapons" }, items.Select(i => i.Table.Name));
        }

        /// <summary>
        /// Tables are shown in their groups, with the last part of their name, and the groups have the path of their parents.
        /// </summary>
        [Test]
        public void Build_NestedGroups_TablesInTheirGroups()
        {
            List<TableTreeNode> items = TableTree.Build(CreateTables("Items/Equipment/Weapons", "Items/Equipment/Shields", "Items/All"));

            Assert.AreEqual(1, items.Count);
            TableTreeNode itemsGroup = items[0];
            Assert.IsTrue(itemsGroup.IsGroup);
            Assert.AreEqual("Items", itemsGroup.Name);
            Assert.AreEqual("Items", itemsGroup.GroupPath);
            Assert.IsNull(itemsGroup.Table);
            CollectionAssert.AreEqual(new[] { "All", "Equipment" }, itemsGroup.Children.Select(i => i.Name));

            TableTreeNode equipmentGroup = itemsGroup.Children[1];
            Assert.IsTrue(equipmentGroup.IsGroup);
            Assert.AreEqual("Items/Equipment", equipmentGroup.GroupPath);
            CollectionAssert.AreEqual(new[] { "Shields", "Weapons" }, equipmentGroup.Children.Select(i => i.Name));
            CollectionAssert.AreEqual(
                new[] { "Items/Equipment/Shields", "Items/Equipment/Weapons" },
                equipmentGroup.Children.Select(i => i.Table.Name));
        }

        /// <summary>
        /// Groups and tables of a same group are sorted together by name, and a group comes before a table of the same name.
        /// </summary>
        [Test]
        public void Build_GroupsAndTables_SortedTogetherByName()
        {
            List<TableTreeNode> items = TableTree.Build(CreateTables("Consumables", "Items", "Items/Weapons", "Armors/Helmets"));

            CollectionAssert.AreEqual(new[] { "Armors", "Consumables", "Items", "Items" }, items.Select(i => i.Name));
            CollectionAssert.AreEqual(new[] { true, false, true, false }, items.Select(i => i.IsGroup));
        }

        /// <summary>
        /// Group names are compared ignoring case: the group is named after the first of its tables in the order of the names.
        /// </summary>
        [Test]
        public void Build_GroupsDifferingByCase_SameGroup()
        {
            List<TableTreeNode> items = TableTree.Build(CreateTables("Items/Weapons", "items/Armors"));

            Assert.AreEqual(1, items.Count);
            Assert.AreEqual("items", items[0].Name);
            Assert.AreEqual("items", items[0].GroupPath);
            CollectionAssert.AreEqual(new[] { "Armors", "Weapons" }, items[0].Children.Select(i => i.Name));
        }

        /// <summary>
        /// The spaces and the empty parts of the names don't create groups of their own.
        /// </summary>
        [Test]
        public void Build_SpacesAndEmptyParts_SameGroup()
        {
            List<TableTreeNode> items = TableTree.Build(CreateTables("Items/Weapons", " Items //Armors"));

            Assert.AreEqual(1, items.Count);
            Assert.AreEqual("Items", items[0].GroupPath);
            CollectionAssert.AreEqual(new[] { "Armors", "Weapons" }, items[0].Children.Select(i => i.Name));
        }

        /// <summary>
        /// Two tables of the same name are both shown, in the order they are given in.
        /// </summary>
        [Test]
        public void Build_SameName_BothTablesShown()
        {
            List<TableDefinition> tables = CreateTables("Items/Weapons", "Items/Weapons");

            List<TableTreeNode> items = TableTree.Build(tables);

            CollectionAssert.AreEqual(tables, items[0].Children.Select(i => i.Table));
        }

        /// <summary>
        /// Built from the tables that match a search, the tree only has the groups of these tables.
        /// </summary>
        [Test]
        public void Build_SearchedTables_OnlyTheirGroups()
        {
            List<TableDefinition> tables = CreateTables("Items/Equipment/Weapons", "Items/Consumables", "Characters/Heroes");

            List<TableTreeNode> items = TableTree.Build(TableListPage.FilterTables(tables, "weap"));

            Assert.AreEqual(1, items.Count);
            Assert.AreEqual("Items", items[0].GroupPath);
            Assert.AreEqual(1, items[0].Children.Count);
            Assert.AreEqual("Items/Equipment", items[0].Children[0].GroupPath);
            CollectionAssert.AreEqual(new[] { "Weapons" }, items[0].Children[0].Children.Select(i => i.Name));
        }

        #endregion


        #region Private API

        /// <summary>
        /// Creates tables with given names, without columns.
        /// </summary>
        /// <param name="names">The names of the tables.</param>
        /// <returns>Returns the created tables, in the order of the names.</returns>
        private static List<TableDefinition> CreateTables(params string[] names)
        {
            return names
                .Select((name, index) => new TableDefinition(
                    $"Table{index}",
                    typeof(ItemSO),
                    name,
                    false,
                    false,
                    true,
                    Array.Empty<ColumnDefinition>()))
                .ToList();
        }

        #endregion

    }

}
