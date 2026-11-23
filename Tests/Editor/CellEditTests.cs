using System;

using NUnit.Framework;

using UnityEngine;
using UnityEngine.UIElements;

using SideXP.Database.EditorOnly;

using Object = UnityEngine.Object;

namespace SideXP.Database.Tests
{

    /// <summary>
    /// Tests for the default edit fields and the write path of the edits (<see cref="CellEditSession"/>).
    /// </summary>
    public class CellEditTests
    {

        #region Fields

        /// <summary>
        /// The path of the serialized property of <see cref="PipelineTestSO.number"/>.
        /// </summary>
        private const string NumberPath = nameof(PipelineTestSO.number);

        /// <summary>
        /// An in-memory asset, created for each test.
        /// </summary>
        private PipelineTestSO _asset = null;

        #endregion


        #region Lifecycle

        /// <summary>
        /// Creates the asset of the test.
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            _asset = ScriptableObject.CreateInstance<PipelineTestSO>();
            _asset.number = 3;
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


        #region Edit fields

        /// <summary>
        /// Strings and numbers have a default edit field, the other types don't.
        /// </summary>
        [Test]
        public void Create_Types_ReturnsMatchingFields()
        {
            Assert.IsInstanceOf<TextField>(EditFields.Create(typeof(string), "text"));
            Assert.IsInstanceOf<IntegerField>(EditFields.Create(typeof(int), 3));
            Assert.IsInstanceOf<IntegerField>(EditFields.Create(typeof(byte), (byte)3));
            Assert.IsInstanceOf<LongField>(EditFields.Create(typeof(long), 3L));
            Assert.IsInstanceOf<FloatField>(EditFields.Create(typeof(float), 1.5f));
            Assert.IsInstanceOf<DoubleField>(EditFields.Create(typeof(double), 1.5));
            Assert.IsNull(EditFields.Create(typeof(bool), true));
            Assert.IsNull(EditFields.Create(typeof(DayOfWeek), DayOfWeek.Friday));
            Assert.IsNull(EditFields.Create(typeof(Vector3), Vector3.zero));
        }

        /// <summary>
        /// The fields start with the value of the cell, and a null string starts empty.
        /// </summary>
        [Test]
        public void Create_Value_SetsFieldValue()
        {
            Assert.AreEqual(42, ((IntegerField)EditFields.Create(typeof(int), 42)).value);
            Assert.AreEqual(string.Empty, ((TextField)EditFields.Create(typeof(string), null)).value);
        }

        /// <summary>
        /// The value of a field is converted to the type of the column.
        /// </summary>
        [Test]
        public void TryGetValue_SmallerType_ConvertsValue()
        {
            IntegerField field = (IntegerField)EditFields.Create(typeof(byte), (byte)3);
            field.value = 200;

            Assert.IsTrue(EditFields.TryGetValue(field, typeof(byte), out object value));
            Assert.AreEqual((byte)200, value);
        }

        /// <summary>
        /// A value that doesn't fit in the type of the column is rejected.
        /// </summary>
        [Test]
        public void TryGetValue_Overflow_ReturnsFalse()
        {
            IntegerField field = (IntegerField)EditFields.Create(typeof(byte), (byte)3);
            field.value = 300;

            Assert.IsFalse(EditFields.TryGetValue(field, typeof(byte), out object value));
            Assert.IsNull(value);
        }

        #endregion


        #region Edit session

        /// <summary>
        /// A confirmed edit keeps the written value.
        /// </summary>
        [Test]
        public void Confirm_WrittenValue_KeepsValue()
        {
            using (CellEditSession session = new CellEditSession(_asset, NumberPath, "Edit Number"))
            {
                Assert.IsTrue(session.Write(10));
                session.Confirm();
            }

            Assert.AreEqual(10, _asset.number);
        }

        /// <summary>
        /// A cancelled edit reverts the written value.
        /// </summary>
        [Test]
        public void Cancel_WrittenValue_RevertsValue()
        {
            using (CellEditSession session = new CellEditSession(_asset, NumberPath, "Edit Number"))
            {
                session.Write(10);
                session.Cancel();
            }

            Assert.AreEqual(3, _asset.number);
        }

        /// <summary>
        /// An edit disposed without being confirmed is cancelled.
        /// </summary>
        [Test]
        public void Dispose_NotConfirmed_RevertsValue()
        {
            using (CellEditSession session = new CellEditSession(_asset, NumberPath, "Edit Number"))
                session.Write(10);

            Assert.AreEqual(3, _asset.number);
        }

        /// <summary>
        /// An edit can't start on a property that doesn't exist.
        /// </summary>
        [Test]
        public void Constructor_UnknownProperty_Throws()
        {
            Assert.Throws<ArgumentException>(() => new CellEditSession(_asset, "_unknown", "Edit"));
        }

        #endregion

    }

}
