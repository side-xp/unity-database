using System;
using System.Collections.Generic;

using NUnit.Framework;

using SideXP.Database.EditorOnly;

namespace SideXP.Database.Tests
{

    /// <summary>
    /// Tests for the resolution of the tables declared with <see cref="TableAttribute"/>: inheritance, names, options and the types
    /// that can't be tables.
    /// </summary>
    public class TableDeclarationTests
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


        #region Inheritance

        /// <summary>
        /// A type with its own declaration uses the declared name and options.
        /// </summary>
        [Test]
        public void Build_DeclaredTable_UsesDeclaredNameAndOptions()
        {
            TableDefinition table = CodeTables.Build(typeof(ItemSO), _warnings);

            Assert.IsNotNull(table);
            Assert.AreEqual(typeof(ItemSO), table.Type);
            Assert.AreEqual("Tests/Items/All", table.Name);
            Assert.IsFalse(table.AllowRenameAsset);
            Assert.IsFalse(table.HideAssetName);
            Assert.IsEmpty(_warnings);
        }

        /// <summary>
        /// A table inherited from a base type is named after its type, in the group of the base type's table.
        /// </summary>
        [Test]
        public void Build_InheritedTable_UsesTypeNameInParentGroup()
        {
            TableDefinition table = CodeTables.Build(typeof(ConsumableSO), _warnings);

            Assert.IsNotNull(table);
            Assert.AreEqual("Tests/Items/Consumable SO", table.Name);
        }

        /// <summary>
        /// A table inherited from a base type copies the options of the base type's declaration.
        /// </summary>
        [Test]
        public void Build_InheritedTable_CopiesParentOptions()
        {
            TableDefinition table = CodeTables.Build(typeof(ConsumableSO), _warnings);

            Assert.IsFalse(table.AllowRenameAsset);
            Assert.IsFalse(table.HideAssetName);
        }

        /// <summary>
        /// A type's own declaration replaces the declaration of its base type, options included.
        /// </summary>
        [Test]
        public void Build_OwnDeclaration_ReplacesParentDeclaration()
        {
            TableDefinition table = CodeTables.Build(typeof(EquipmentSO), _warnings);

            Assert.IsNotNull(table);
            Assert.AreEqual("Tests/Items/Equipment", table.Name);
            Assert.IsTrue(table.AllowRenameAsset);
        }

        /// <summary>
        /// The types derived from a table that is not inherited are not tables, even if an older base type passes its table on.
        /// </summary>
        /// <param name="type">The type to check.</param>
        [TestCase(typeof(WeaponSO))]
        [TestCase(typeof(ArmorSO))]
        [TestCase(typeof(HelmetSO))]
        public void Build_DerivedFromNotInheritedTable_ReturnsNull(Type type)
        {
            TableDefinition table = CodeTables.Build(type, _warnings);

            Assert.IsNull(table);
            Assert.IsEmpty(_warnings);
        }

        /// <summary>
        /// The candidates include the declared types and the types derived from the inherited tables.
        /// </summary>
        [Test]
        public void GetCandidateTypes_IncludesDeclaredAndInheritedTypes()
        {
            HashSet<Type> candidates = CodeTables.GetCandidateTypes();

            Assert.IsTrue(candidates.Contains(typeof(ItemSO)));
            Assert.IsTrue(candidates.Contains(typeof(ConsumableSO)));
            Assert.IsTrue(candidates.Contains(typeof(EquipmentSO)));
            Assert.IsTrue(candidates.Contains(typeof(ColumnsSO)));
        }

        #endregion


        #region Rename permission

        /// <summary>
        /// The nearest declaration of an asset's type decides if it can be renamed: its own, an inherited one, or the declaration of a
        /// table that is not inherited but lists the type.
        /// </summary>
        /// <param name="type">The type of the asset.</param>
        /// <param name="isAllowed">The expected result.</param>
        [TestCase(typeof(ItemSO), false)]
        [TestCase(typeof(ConsumableSO), false)]
        [TestCase(typeof(EquipmentSO), true)]
        [TestCase(typeof(HelmetSO), true)]
        public void IsRenameAllowed_NearestDeclarationDecides(Type type, bool isAllowed)
        {
            Assert.AreEqual(isAllowed, CodeTables.IsRenameAllowed(type));
        }

        /// <summary>
        /// A type without declaration in its hierarchy uses the default of the option.
        /// </summary>
        [Test]
        public void IsRenameAllowed_NoDeclaration_UsesDefault()
        {
            Assert.AreEqual(new TableAttribute().AllowRenameAsset, CodeTables.IsRenameAllowed(typeof(UnityEngine.ScriptableObject)));
        }

        /// <summary>
        /// A null type is a programming error.
        /// </summary>
        [Test]
        public void IsRenameAllowed_NullType_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => CodeTables.IsRenameAllowed(null));
        }

        #endregion


        #region Names

        /// <summary>
        /// A declaration without a name uses the nicified type name.
        /// </summary>
        [Test]
        public void Build_NoName_UsesNicifiedTypeName()
        {
            TableDefinition table = CodeTables.Build(typeof(IdentifierTestTypes.NestedTableSO), _warnings);

            Assert.IsNotNull(table);
            Assert.AreEqual("Nested Table SO", table.Name);
        }

        #endregion


        #region Invalid and special types

        /// <summary>
        /// A type that doesn't derive from <see cref="UnityEngine.ScriptableObject"/> is not a table, and is reported.
        /// </summary>
        [Test]
        public void Build_NotScriptableObject_ReturnsNullWithWarning()
        {
            TableDefinition table = CodeTables.Build(typeof(NotScriptableObjectTable), _warnings);

            Assert.IsNull(table);
            Assert.AreEqual(1, _warnings.Count);
        }

        /// <summary>
        /// An open generic type that doesn't pass its table on is not a table, and is reported.
        /// </summary>
        [Test]
        public void Build_OpenGenericType_ReturnsNullWithWarning()
        {
            TableDefinition table = CodeTables.Build(typeof(OpenGenericTableSO<>), _warnings);

            Assert.IsNull(table);
            Assert.AreEqual(1, _warnings.Count);
        }

        /// <summary>
        /// An open generic type that passes its table on is not a table, but is not reported either.
        /// </summary>
        [Test]
        public void Build_InheritedOpenGenericType_ReturnsNullWithoutWarning()
        {
            TableDefinition table = CodeTables.Build(typeof(GenericBaseTableSO<>), _warnings);

            Assert.IsNull(table);
            Assert.IsEmpty(_warnings);
        }

        /// <summary>
        /// A closed type derived from an open generic type that passes its table on is a table.
        /// </summary>
        [Test]
        public void Build_ClosedTypeDerivedFromGenericTable_IsTable()
        {
            TableDefinition table = CodeTables.Build(typeof(ClosedGenericTableSO), _warnings);

            Assert.IsNotNull(table);
            Assert.AreEqual("Tests/Generic/Closed Generic Table SO", table.Name);
        }

        /// <summary>
        /// An obsolete type is a table, marked as obsolete.
        /// </summary>
        [Test]
        public void Build_ObsoleteType_IsMarkedObsolete()
        {
#pragma warning disable CS0618 // The obsolete type is the subject of the test
            TableDefinition obsoleteTable = CodeTables.Build(typeof(ObsoleteTableSO), _warnings);
#pragma warning restore CS0618
            TableDefinition table = CodeTables.Build(typeof(ItemSO), _warnings);

            Assert.IsNotNull(obsoleteTable);
            Assert.IsTrue(obsoleteTable.IsObsolete);
            Assert.IsFalse(table.IsObsolete);
        }

        #endregion

    }

}
