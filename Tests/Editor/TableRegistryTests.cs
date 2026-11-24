using System;
using System.Collections.Generic;

using NUnit.Framework;

using SideXP.Database.EditorOnly;

namespace SideXP.Database.Tests
{

    /// <summary>
    /// Tests for the lookup of the tables that list the assets of a type (<see cref="TableRegistry.GetTablesListing(Type)"/>), used
    /// by the <i>Open In Database</i> menu.
    /// </summary>
    public class TableRegistryTests
    {

        #region Tables listing a type

        /// <summary>
        /// The type of a table that has no base table is listed by its own table only.
        /// </summary>
        [Test]
        public void GetTablesListing_RootTableType_ReturnsOwnTable()
        {
            AssertTableTypes(typeof(ItemSO), typeof(ItemSO));
        }

        /// <summary>
        /// The type of an inherited table is listed by its own table first, then by the table of its base type.
        /// </summary>
        [Test]
        public void GetTablesListing_InheritedTableType_ReturnsOwnTableFirst()
        {
            AssertTableTypes(typeof(ConsumableSO), typeof(ConsumableSO), typeof(ItemSO));
        }

        /// <summary>
        /// A type that is not a table (its base table is not inherited) is listed by the tables of its base types, the nearest first.
        /// </summary>
        [Test]
        public void GetTablesListing_TypeWithoutOwnTable_ReturnsBaseTables()
        {
            AssertTableTypes(typeof(WeaponSO), typeof(EquipmentSO), typeof(ItemSO));
        }

        /// <summary>
        /// A type derived from an abstract type that is not a table is listed by the tables of the base types of the abstract type.
        /// </summary>
        [Test]
        public void GetTablesListing_TypeDerivedFromAbstractType_ReturnsBaseTables()
        {
            AssertTableTypes(typeof(HelmetSO), typeof(EquipmentSO), typeof(ItemSO));
        }

        /// <summary>
        /// A type that no table lists gives no table.
        /// </summary>
        [Test]
        public void GetTablesListing_TypeWithoutTable_ReturnsEmpty()
        {
            Assert.IsEmpty(TableRegistry.GetTablesListing(typeof(EditTestSO)));
        }

        /// <summary>
        /// A null type gives no table.
        /// </summary>
        [Test]
        public void GetTablesListing_Null_ReturnsEmpty()
        {
            Assert.IsEmpty(TableRegistry.GetTablesListing(null));
        }

        #endregion


        #region Private API

        /// <summary>
        /// Checks the types of the tables that list the assets of a type, in order.
        /// </summary>
        /// <param name="assetType">The type of the assets.</param>
        /// <param name="expectedTableTypes">The expected types of the tables, the nearest first.</param>
        private static void AssertTableTypes(Type assetType, params Type[] expectedTableTypes)
        {
            List<TableDefinition> tables = TableRegistry.GetTablesListing(assetType);
            List<Type> tableTypes = tables.ConvertAll(table => table.Type);
            CollectionAssert.AreEqual(expectedTableTypes, tableTypes);
        }

        #endregion

    }

}
