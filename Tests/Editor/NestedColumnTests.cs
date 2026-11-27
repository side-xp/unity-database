using System.Collections.Generic;
using System.Linq;

using NUnit.Framework;

using UnityEngine;

using SideXP.Database.EditorOnly;

using Object = UnityEngine.Object;

namespace SideXP.Database.Tests
{

    /// <summary>
    /// Tests for the columns of nested fields (<see cref="ColumnAttribute.Path"/>): collection, invalid paths, validation, reads and
    /// writes.
    /// </summary>
    public class NestedColumnTests
    {

        #region Fields

        /// <summary>
        /// The warnings reported by the tested operation.
        /// </summary>
        private List<string> _warnings = null;

        /// <summary>
        /// The in-memory asset of the test, created for each test.
        /// </summary>
        private NestedColumnsSO _asset = null;

        #endregion


        #region Lifecycle

        /// <summary>
        /// Clears the warnings and creates the asset of the test.
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            _warnings = new List<string>();
            _asset = ScriptableObject.CreateInstance<NestedColumnsSO>();
        }

        /// <summary>
        /// Destroys the asset of the test.
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            if (_asset != null)
                Object.DestroyImmediate(_asset);
        }

        #endregion


        #region Collection

        /// <summary>
        /// A field declares a column per attribute, in the order of the attributes, and the id of each column is its property path.
        /// </summary>
        [Test]
        public void CollectColumns_Paths_ColumnPerAttributeInAttributeOrder()
        {
            List<ColumnDefinition> columns = CodeTables.CollectColumns(typeof(NestedColumnsSO), _warnings);

            string[] expectedIds =
            {
                "_stats.defense",
                "_stats.attack",
                "_stats.bonus.amount",
                "_stats.<Luck>k__BackingField",
                "_stats.speed",
                "_data",
                "_data._baseValue",
                "_data.label",
            };
            CollectionAssert.AreEqual(expectedIds, columns.Select(i => i.Id));
            CollectionAssert.AreEqual(expectedIds, columns.Select(i => i.PropertyPath));
            Assert.IsEmpty(_warnings);
        }

        /// <summary>
        /// Nested columns use their declared name, or the nicified name of the last field of their path (the property name for a
        /// backing field).
        /// </summary>
        [Test]
        public void CollectColumns_Paths_NamedAfterNestedField()
        {
            List<ColumnDefinition> columns = CodeTables.CollectColumns(typeof(NestedColumnsSO), _warnings);

            string[] expectedNames = { "Defense", "Attack", "Bonus", "Luck", "Speed", "Data", "Base Value", "Label" };
            CollectionAssert.AreEqual(expectedNames, columns.Select(i => i.Name));
        }

        /// <summary>
        /// The fields of a nested column go from the marked field to the displayed value, the names of the path trimmed.
        /// </summary>
        [Test]
        public void CollectColumns_PathOfSeveralFields_ChainOfFields()
        {
            ColumnDefinition column = GetColumn("_stats.bonus.amount");

            CollectionAssert.AreEqual(new[] { "_stats", "bonus", "amount" }, column.Fields.Select(i => i.Name));
            Assert.AreEqual(typeof(int), column.Fields[column.Fields.Count - 1].FieldType);
            Assert.AreEqual(60f, column.Width);
        }

        /// <summary>
        /// A nested column has the tooltip of its nested field, or the tooltip of the field that contains it.
        /// </summary>
        [Test]
        public void CollectColumns_Paths_TooltipOfNearestField()
        {
            Assert.AreEqual("The defense of the item.", GetColumn("_stats.defense").Tooltip);
            Assert.AreEqual("The stats of the item.", GetColumn("_stats.attack").Tooltip);
            Assert.IsNull(GetColumn("_data.label").Tooltip);
        }

        /// <summary>
        /// The columns of invalid paths are skipped with a warning each: unknown and non-serialized fields, empty names, a second
        /// column with the same path, and paths through an object reference, a managed reference or an array.
        /// </summary>
        [Test]
        public void CollectColumns_InvalidPaths_SkippedWithWarnings()
        {
            List<ColumnDefinition> columns = CodeTables.CollectColumns(typeof(InvalidPathColumnsSO), _warnings);

            CollectionAssert.AreEqual(new[] { "_stats.attack" }, columns.Select(i => i.Id));
            Assert.AreEqual("Attack", columns[0].Name, "The first column of the path is kept.");
            Assert.AreEqual(7, _warnings.Count);
        }

        /// <summary>
        /// The cells of a nested column read the attributes of the nested field, such as <see cref="RangeAttribute"/>.
        /// </summary>
        [Test]
        public void FromDefinition_NestedColumn_FieldIsTheNestedField()
        {
            TableViewColumn column = TableViewColumn.FromDefinition(GetColumn("_stats.speed"));

            Assert.AreEqual(nameof(NestedStats.speed), column.Field.Name);
            Assert.AreEqual("_stats.speed", column.PropertyPath);
        }

        #endregion


        #region Validation, reads and writes

        /// <summary>
        /// The nested columns have a serialized property on an asset, so they are all kept by the validation.
        /// </summary>
        [Test]
        public void GetValidColumns_NestedColumns_AllKept()
        {
            List<ColumnDefinition> columns = CodeTables.CollectColumns(typeof(NestedColumnsSO), _warnings);

            List<ColumnDefinition> validColumns = ColumnValidation.GetValidColumns(columns, _asset, _warnings);

            CollectionAssert.AreEqual(columns, validColumns);
            Assert.IsEmpty(_warnings);
        }

        /// <summary>
        /// The nested columns are read with reflection, through structs, classes, base classes and backing fields.
        /// </summary>
        [Test]
        public void Read_NestedColumns_ReadsNestedValues()
        {
            _asset.Stats = new NestedStats { attack = 3, bonus = new NestedBonus { amount = 5 }, Luck = 7 };
            _asset.Data.BaseValue = 11;
            _asset.Data.label = "Label";

            Assert.AreEqual(3, Read("_stats.attack"));
            Assert.AreEqual(5, Read("_stats.bonus.amount"));
            Assert.AreEqual(7, Read("_stats.<Luck>k__BackingField"));
            Assert.AreEqual(11, Read("_data._baseValue"));
            Assert.AreEqual("Label", Read("_data.label"));
        }

        /// <summary>
        /// The nested columns write through their nested serialized property.
        /// </summary>
        [Test]
        public void WriteValue_NestedColumns_WritesNestedValues()
        {
            Assert.IsTrue(CellEditSession.WriteValue(_asset, GetColumn("_stats.bonus.amount").PropertyPath, 4));
            Assert.IsTrue(CellEditSession.WriteValue(_asset, GetColumn("_stats.<Luck>k__BackingField").PropertyPath, 8));
            Assert.IsTrue(CellEditSession.WriteValue(_asset, GetColumn("_data._baseValue").PropertyPath, 12));

            Assert.AreEqual(4, _asset.Stats.bonus.amount);
            Assert.AreEqual(8, _asset.Stats.Luck);
            Assert.AreEqual(12, _asset.Data.BaseValue);
        }

        #endregion


        #region Private API

        /// <summary>
        /// Gets a column of <see cref="NestedColumnsSO"/>.
        /// </summary>
        /// <param name="id">The identifier of the column.</param>
        /// <returns>Returns the found column.</returns>
        private ColumnDefinition GetColumn(string id)
        {
            return CodeTables.CollectColumns(typeof(NestedColumnsSO), _warnings).Single(i => i.Id == id);
        }

        /// <summary>
        /// Reads the value of a column on the asset of the test, with the accessor chosen for the column.
        /// </summary>
        /// <param name="id">The identifier of the column.</param>
        /// <returns>Returns the read value.</returns>
        private object Read(string id)
        {
            IValueAccessor accessor = ValueAccessors.Create(GetColumn(id));
            Assert.IsInstanceOf<ReflectionAccessor>(accessor);

            using (ValueReadContext context = new ValueReadContext(_asset))
                return accessor.Read(context);
        }

        #endregion

    }

}
