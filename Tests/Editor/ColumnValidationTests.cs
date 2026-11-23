using System.Collections.Generic;
using System.Linq;

using NUnit.Framework;

using UnityEngine;

using SideXP.Database.EditorOnly;

using Object = UnityEngine.Object;

namespace SideXP.Database.Tests
{

    /// <summary>
    /// Tests for the validation of the columns against an asset, which catches the fields Unity can't serialize.
    /// </summary>
    public class ColumnValidationTests
    {

        #region Fields

        /// <summary>
        /// The warnings reported by the tested operation.
        /// </summary>
        private List<string> _warnings = null;

        /// <summary>
        /// The in-memory assets created by the test, destroyed after it.
        /// </summary>
        private List<Object> _assets = null;

        #endregion


        #region Lifecycle

        /// <summary>
        /// Clears the warnings and the assets before each test.
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            _warnings = new List<string>();
            _assets = new List<Object>();
        }

        /// <summary>
        /// Destroys the in-memory assets.
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            foreach (Object asset in _assets)
                Object.DestroyImmediate(asset);
        }

        #endregion


        #region Validation

        /// <summary>
        /// Columns on fields of types Unity can't serialize pass the checks of the definition, but are skipped with a warning when
        /// validated against an asset.
        /// </summary>
        [Test]
        public void GetValidColumns_UnserializableTypes_SkippedWithWarnings()
        {
            List<ColumnDefinition> columns = CodeTables.CollectColumns(typeof(UnserializableColumnsSO), _warnings);
            Assert.AreEqual(3, columns.Count, "Precondition: the field modifiers and attributes are valid.");

            UnserializableColumnsSO asset = CreateAsset<UnserializableColumnsSO>();
            List<ColumnDefinition> validColumns = ColumnValidation.GetValidColumns(columns, asset, _warnings);

            CollectionAssert.AreEqual(new[] { "_validField" }, validColumns.Select(i => i.Id));
            Assert.AreEqual(2, _warnings.Count);
        }

        /// <summary>
        /// Valid columns are all kept, including the backing field of an auto-property.
        /// </summary>
        [Test]
        public void GetValidColumns_ValidColumns_AllKept()
        {
            List<ColumnDefinition> columns = CodeTables.CollectColumns(typeof(ColumnsSO), _warnings);

            List<ColumnDefinition> validColumns = ColumnValidation.GetValidColumns(columns, CreateAsset<ColumnsSO>(), _warnings);

            CollectionAssert.AreEqual(columns, validColumns);
            Assert.IsEmpty(_warnings);
        }

        #endregion


        #region Private API

        /// <summary>
        /// Creates an in-memory asset, destroyed after the test.
        /// </summary>
        /// <typeparam name="T">The type of the asset.</typeparam>
        /// <returns>Returns the created asset.</returns>
        private T CreateAsset<T>()
            where T : ScriptableObject
        {
            T asset = ScriptableObject.CreateInstance<T>();
            _assets.Add(asset);
            return asset;
        }

        #endregion

    }

}
