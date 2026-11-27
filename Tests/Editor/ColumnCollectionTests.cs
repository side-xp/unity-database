using System.Collections.Generic;
using System.Linq;

using NUnit.Framework;

using SideXP.Database.EditorOnly;

namespace SideXP.Database.Tests
{

    /// <summary>
    /// Tests for the collection of the columns declared with <see cref="ColumnAttribute"/>: order, names, widths, tooltips and invalid
    /// columns.
    /// </summary>
    public class ColumnCollectionTests
    {

        #region Fields

        /// <summary>
        /// The warnings reported by the tested operation.
        /// </summary>
        private List<string> _warnings = null;

        #endregion


        #region Lifecycle

        /// <summary>
        /// Clears the warnings before each test.
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            _warnings = new List<string>();
        }

        #endregion


        #region Order and names

        /// <summary>
        /// Columns are ordered from the root base type down to the table type, then in declaration order, and the public and protected
        /// fields of base types are collected once.
        /// </summary>
        [Test]
        public void CollectColumns_Hierarchy_OrdersFromRootBaseType()
        {
            List<ColumnDefinition> columns = CodeTables.CollectColumns(typeof(ColumnsSO), _warnings);

            string[] expectedIds = { "baseFirst", "_baseSecond", "_first", "second", "blankName", "<AutoProperty>k__BackingField" };
            CollectionAssert.AreEqual(expectedIds, columns.Select(i => i.Id));
            Assert.IsEmpty(_warnings);
        }

        /// <summary>
        /// Columns use their declared name, or the nicified field name (or property name for a backing field) if it's null or blank.
        /// </summary>
        [Test]
        public void CollectColumns_Names_UseDeclaredOrNicifiedName()
        {
            List<ColumnDefinition> columns = CodeTables.CollectColumns(typeof(ColumnsSO), _warnings);

            string[] expectedNames = { "Base First", "Base Second", "First", "Custom Name", "Blank Name", "Auto Property" };
            CollectionAssert.AreEqual(expectedNames, columns.Select(i => i.Name));
        }

        /// <summary>
        /// A table inherited from a base type includes the columns of the base type.
        /// </summary>
        [Test]
        public void Build_InheritedTable_IncludesBaseColumns()
        {
            TableDefinition table = CodeTables.Build(typeof(ConsumableSO), _warnings);

            CollectionAssert.AreEqual(new[] { "_displayName", "_amount" }, table.Columns.Select(i => i.Id));
        }

        #endregion


        #region Column properties

        /// <summary>
        /// Columns have the declared width and the tooltip of their field, and are flexible without a width.
        /// </summary>
        [Test]
        public void CollectColumns_WidthAndTooltip_ReadFromAttributes()
        {
            List<ColumnDefinition> columns = CodeTables.CollectColumns(typeof(ColumnsSO), _warnings);
            ColumnDefinition fixedColumn = columns.Single(i => i.Id == "second");
            ColumnDefinition flexibleColumn = columns.Single(i => i.Id == "_first");

            Assert.AreEqual(120f, fixedColumn.Width);
            Assert.IsFalse(fixedColumn.IsFlexible);
            Assert.AreEqual("The tooltip of the second column.", fixedColumn.Tooltip);
            Assert.IsTrue(flexibleColumn.IsFlexible);
            Assert.IsNull(flexibleColumn.Tooltip);
        }

        /// <summary>
        /// The property path of a column is the name of its field, and its field path holds that field only.
        /// </summary>
        [Test]
        public void CollectColumns_FieldColumn_PathIsTheField()
        {
            List<ColumnDefinition> columns = CodeTables.CollectColumns(typeof(ColumnsSO), _warnings);
            ColumnDefinition column = columns.Single(i => i.Id == "_first");

            Assert.AreEqual("_first", column.PropertyPath);
            Assert.AreEqual(1, column.Fields.Count);
            Assert.AreEqual("_first", column.Fields[0].Name);
        }

        /// <summary>
        /// A column declared in an open generic base type has the field type of the closed type.
        /// </summary>
        [Test]
        public void CollectColumns_ClosedGenericBase_ResolvesFieldType()
        {
            List<ColumnDefinition> columns = CodeTables.CollectColumns(typeof(ClosedGenericTableSO), _warnings);

            Assert.AreEqual(1, columns.Count);
            Assert.AreEqual(typeof(int), columns[0].Fields[0].FieldType);
        }

        #endregion


        #region Invalid columns

        /// <summary>
        /// Columns on fields that are not serialized are skipped with a warning each, and a second column without path on a field is
        /// skipped with a warning, since it would display the same value.
        /// </summary>
        [Test]
        public void CollectColumns_InvalidFields_SkippedWithWarnings()
        {
            List<ColumnDefinition> columns = CodeTables.CollectColumns(typeof(InvalidColumnsSO), _warnings);

            CollectionAssert.AreEqual(new[] { "_severalColumns", "_validField" }, columns.Select(i => i.Id));
            Assert.AreEqual("Several Columns", columns[0].Name, "The first column of the field is kept.");
            // Static, const, readonly, non-serialized and private fields, plus the second column of the field with several columns
            Assert.AreEqual(6, _warnings.Count);
        }

        #endregion

    }

}
