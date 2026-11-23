using NUnit.Framework;

using UnityEditor;

using SideXP.Database.EditorOnly;

namespace SideXP.Database.Tests
{

    /// <summary>
    /// Tests for the stable identifiers of the tables declared in code, and for finding tables in the <see cref="TableRegistry"/>.
    /// </summary>
    public class TableIdentifierTests
    {

        #region Identifiers

        /// <summary>
        /// A type with its own script file is identified by the GUID of that file.
        /// </summary>
        [Test]
        public void GetId_TypeWithOwnScript_ReturnsScriptGuid()
        {
            string id = CodeTables.GetId(typeof(ItemSO));
            MonoScript script = AssetDatabase.LoadAssetAtPath<MonoScript>(AssetDatabase.GUIDToAssetPath(id));

            Assert.IsNotNull(script);
            Assert.AreEqual(typeof(ItemSO), script.GetClass());
        }

        /// <summary>
        /// A nested type has no script file of its own, so it's identified by its type name.
        /// </summary>
        [Test]
        public void GetId_NestedType_ReturnsTypeName()
        {
            string id = CodeTables.GetId(typeof(IdentifierTestTypes.NestedTableSO));

            Assert.AreEqual("SideXP.Database.Tests.IdentifierTestTypes+NestedTableSO, SideXP.Unity.Database.Tests.Editor", id);
        }

        /// <summary>
        /// A type declared in a file named after another type has no script file of its own, so it's identified by its type name.
        /// </summary>
        [Test]
        public void GetId_TypeInMisnamedFile_ReturnsTypeName()
        {
            string id = CodeTables.GetId(typeof(MisnamedTableSO));

            Assert.AreEqual("SideXP.Database.Tests.MisnamedTableSO, SideXP.Unity.Database.Tests.Editor", id);
        }

        #endregion


        #region Registry

        /// <summary>
        /// The registry finds a table by its type and by its identifier.
        /// </summary>
        [Test]
        public void TryGet_Table_FoundByTypeAndId()
        {
            Assert.IsTrue(TableRegistry.TryGet(typeof(ItemSO), out TableDefinition tableByType));
            Assert.IsTrue(TableRegistry.TryGet(tableByType.Id, out TableDefinition tableById));
            Assert.AreSame(tableByType, tableById);
        }

        /// <summary>
        /// The registry doesn't return the table of a base type for a type that is not a table.
        /// </summary>
        [Test]
        public void TryGet_NotATable_ReturnsFalse()
        {
            Assert.IsFalse(TableRegistry.TryGet(typeof(WeaponSO), out _));
        }

        #endregion

    }

}
