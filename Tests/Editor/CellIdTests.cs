using NUnit.Framework;

using UnityEngine;

using SideXP.Database.EditorOnly;

using Object = UnityEngine.Object;

namespace SideXP.Database.Tests
{

    /// <summary>
    /// Tests for <see cref="CellId"/>: the identification of cells by asset and column.
    /// </summary>
    public class CellIdTests
    {

        #region Fields

        /// <summary>
        /// An in-memory asset, created for each test.
        /// </summary>
        private PipelineTestSO _asset = null;

        /// <summary>
        /// Another in-memory asset, created for each test.
        /// </summary>
        private PipelineTestSO _otherAsset = null;

        #endregion


        #region Lifecycle

        /// <summary>
        /// Creates the assets of the test.
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            _asset = ScriptableObject.CreateInstance<PipelineTestSO>();
            _otherAsset = ScriptableObject.CreateInstance<PipelineTestSO>();
        }

        /// <summary>
        /// Destroys the assets of the test.
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            if (_asset != null)
                Object.DestroyImmediate(_asset);

            if (_otherAsset != null)
                Object.DestroyImmediate(_otherAsset);
        }

        #endregion


        #region Tests

        /// <summary>
        /// Two identifiers are equal if they have the same asset and the same column.
        /// </summary>
        [Test]
        public void Equals_SameAssetAndColumn_ReturnsTrue()
        {
            CellId cell = new CellId(_asset, "_number");

            Assert.AreEqual(cell, new CellId(_asset, "_number"));
            Assert.AreEqual(cell.GetHashCode(), new CellId(_asset, "_number").GetHashCode());
            Assert.AreNotEqual(cell, new CellId(_asset, "_label"));
            Assert.AreNotEqual(cell, new CellId(_otherAsset, "_number"));
            Assert.IsTrue(cell.Is(_asset, "_number"));
            Assert.IsFalse(cell.Is(_otherAsset, "_number"));
        }

        /// <summary>
        /// The default identifier designates no cell.
        /// </summary>
        [Test]
        public void None_Default_IsNone()
        {
            Assert.IsTrue(CellId.None.IsNone);
            Assert.IsTrue(default(CellId).IsNone);
            Assert.IsFalse(new CellId(_asset, "_number").IsNone);
        }

        /// <summary>
        /// An identifier keeps designating its cell after its asset is destroyed, so the table can tell that the cell is gone.
        /// </summary>
        [Test]
        public void IsNone_DestroyedAsset_ReturnsFalse()
        {
            CellId cell = new CellId(_asset, "_number");
            int hashCode = cell.GetHashCode();
            Object.DestroyImmediate(_asset);

            Assert.IsFalse(cell.IsNone);
            Assert.IsTrue(cell.Asset == null);
            Assert.AreEqual(hashCode, cell.GetHashCode());
            Assert.DoesNotThrow(() => cell.ToString());
        }

        #endregion

    }

}
