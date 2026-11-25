using System;

using NUnit.Framework;

using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.UIElements;

using SideXP.Database.EditorOnly;

using Object = UnityEngine.Object;

namespace SideXP.Database.Tests
{

    /// <summary>
    /// Tests for the default edit fields and the write path of the edits (<see cref="CellEditSession"/>).
    /// </summary>
    public class CellEditTests
    {

        #region Subclasses

        /// <summary>
        /// A handler that converts arrays of <see cref="Vector2"/> to their length and back, to test the edit through the text
        /// conversion. Not registered, so it doesn't change the display of the workspace.
        /// </summary>
        private class ConvertedTextHandler : CellHandler
        {

            /// <inheritdoc/>
            public override string ToText(object value)
            {
                return value is Vector2[] array ? array.Length.ToString() : string.Empty;
            }

            /// <inheritdoc/>
            public override bool TryParse(string text, Type valueType, out object value)
            {
                value = int.TryParse(text, out int length) && length >= 0 ? new Vector2[length] : null;
                return value != null;
            }

        }

        #endregion


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
        /// The built-in types have a matching default edit field, the other types don't.
        /// </summary>
        [Test]
        public void Create_Types_ReturnsMatchingFields()
        {
            Assert.IsInstanceOf<TextField>(EditFields.Create(typeof(string), "text"));
            Assert.IsInstanceOf<TextField>(EditFields.Create(typeof(char), 'a'));
            Assert.IsInstanceOf<IntegerField>(EditFields.Create(typeof(int), 3));
            Assert.IsInstanceOf<IntegerField>(EditFields.Create(typeof(byte), (byte)3));
            Assert.IsInstanceOf<LongField>(EditFields.Create(typeof(long), 3L));
            Assert.IsInstanceOf<FloatField>(EditFields.Create(typeof(float), 1.5f));
            Assert.IsInstanceOf<DoubleField>(EditFields.Create(typeof(double), 1.5));
            Assert.IsInstanceOf<Toggle>(EditFields.Create(typeof(bool), true));
            Assert.IsInstanceOf<EnumField>(EditFields.Create(typeof(DayOfWeek), DayOfWeek.Friday));
            Assert.IsInstanceOf<EnumFlagsField>(EditFields.Create(typeof(AttributeTargets), AttributeTargets.Class));
            Assert.IsInstanceOf<ObjectField>(EditFields.Create(typeof(ScriptableObject), null));
            Assert.IsInstanceOf<ColorField>(EditFields.Create(typeof(Color), Color.red));
            Assert.IsInstanceOf<ColorField>(EditFields.Create(typeof(Color32), new Color32(255, 0, 0, 255)));
            Assert.IsInstanceOf<Vector3Field>(EditFields.Create(typeof(Vector3), Vector3.zero));
            Assert.IsInstanceOf<Vector3Field>(EditFields.Create(typeof(Quaternion), Quaternion.identity));
            Assert.IsInstanceOf<LayerMaskField>(EditFields.Create(typeof(LayerMask), (LayerMask)1));
            Assert.IsNull(EditFields.Create(typeof(int[]), new int[0]));
        }

        /// <summary>
        /// Values edited with a field of another type are converted back to the type of the column.
        /// </summary>
        [Test]
        public void TryGetValue_OtherFieldTypes_ConvertsValue()
        {
            Assert.IsTrue(EditFields.TryGetValue(EditFields.Create(typeof(char), 'x'), typeof(char), out object letter));
            Assert.AreEqual('x', letter);

            Color32 tint = new Color32(10, 20, 30, 40);
            Assert.IsTrue(EditFields.TryGetValue(EditFields.Create(typeof(Color32), tint), typeof(Color32), out object color));
            Assert.AreEqual(tint, color);

            Quaternion rotation = Quaternion.Euler(0, 90, 0);
            VisualElement rotationField = EditFields.Create(typeof(Quaternion), rotation);
            Assert.IsTrue(EditFields.TryGetValue(rotationField, typeof(Quaternion), out object quaternion));
            Assert.AreEqual(1f, Mathf.Abs(Quaternion.Dot(rotation, (Quaternion)quaternion)), 0.0001f);

            Assert.IsTrue(EditFields.TryGetValue(EditFields.Create(typeof(LayerMask), (LayerMask)5), typeof(LayerMask), out object mask));
            Assert.AreEqual(5, ((LayerMask)mask).value);
        }

        /// <summary>
        /// A character field can't be emptied: an empty text is rejected.
        /// </summary>
        [Test]
        public void TryGetValue_EmptyCharacter_ReturnsFalse()
        {
            TextField field = (TextField)EditFields.Create(typeof(char), 'x');
            field.value = string.Empty;

            Assert.IsFalse(EditFields.TryGetValue(field, typeof(char), out _));
        }

        /// <summary>
        /// An empty reference is a valid value.
        /// </summary>
        [Test]
        public void TryGetValue_EmptyReference_ReturnsNull()
        {
            Assert.IsTrue(EditFields.TryGetValue(EditFields.Create(typeof(ScriptableObject), null), typeof(ScriptableObject),
                out object reference));
            Assert.IsNull(reference);
        }

        /// <summary>
        /// Types without a matching field are edited as text when the text conversion of their handler supports them, and can't be
        /// edited otherwise.
        /// </summary>
        [Test]
        public void CreateEditField_NoMatchingField_UsesTextConversion()
        {
            CellHandler handler = new ConvertedTextHandler();
            CellContext context = new CellContext(null, null, typeof(Vector2[]));

            VisualElement field = handler.CreateEditField(null, new Vector2[0], context);
            Assert.IsNotNull(field);
            ((TextField)field).value = "3";
            Assert.IsTrue(handler.TryGetEditedValue(field, context, out object value));
            Assert.AreEqual(3, ((Vector2[])value).Length);

            Assert.IsNull(new DefaultCellHandler().CreateEditField(null, new Vector2[0], context));
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
        /// The property types that need a specific write receive their C# values: enums, characters, layer masks, 32 bits colors,
        /// quaternions and references.
        /// </summary>
        [Test]
        public void Write_SpecificPropertyTypes_WritesValues()
        {
            EditTestSO asset = ScriptableObject.CreateInstance<EditTestSO>();
            try
            {
                Color32 tint = new Color32(10, 20, 30, 40);
                Quaternion rotation = Quaternion.Euler(0, 90, 0);

                WriteAndConfirm(asset, nameof(EditTestSO.day), DayOfWeek.Friday);
                WriteAndConfirm(asset, nameof(EditTestSO.letter), 'z');
                WriteAndConfirm(asset, nameof(EditTestSO.layers), (LayerMask)5);
                WriteAndConfirm(asset, nameof(EditTestSO.tint), tint);
                WriteAndConfirm(asset, nameof(EditTestSO.rotation), rotation);
                WriteAndConfirm(asset, nameof(EditTestSO.reference), _asset);

                Assert.AreEqual(DayOfWeek.Friday, asset.day);
                Assert.AreEqual('z', asset.letter);
                Assert.AreEqual(5, asset.layers.value);
                Assert.AreEqual(tint, asset.tint);
                Assert.AreEqual(1f, Mathf.Abs(Quaternion.Dot(rotation, asset.rotation)), 0.0001f);
                Assert.AreSame(_asset, asset.reference);
            }
            finally
            {
                Object.DestroyImmediate(asset);
            }
        }

        /// <summary>
        /// An edit can't start on a property that doesn't exist.
        /// </summary>
        [Test]
        public void Constructor_UnknownProperty_Throws()
        {
            Assert.Throws<ArgumentException>(() => new CellEditSession(_asset, "_unknown", "Edit"));
        }

        /// <summary>
        /// The writes made to other cells during an edit join its Undo step: undoing the edit reverts all the cells.
        /// </summary>
        [Test]
        public void WriteValue_ConfirmedEdit_SingleUndoStep()
        {
            PipelineTestSO otherAsset = ScriptableObject.CreateInstance<PipelineTestSO>();
            otherAsset.number = 5;
            try
            {
                using (CellEditSession session = new CellEditSession(_asset, NumberPath, "Edit Number"))
                {
                    session.Write(10);
                    Assert.IsTrue(CellEditSession.WriteValue(otherAsset, NumberPath, 10));
                    session.Confirm();
                }

                Assert.AreEqual(10, _asset.number);
                Assert.AreEqual(10, otherAsset.number);

                Undo.PerformUndo();
                Assert.AreEqual(3, _asset.number);
                Assert.AreEqual(5, otherAsset.number);
            }
            finally
            {
                Object.DestroyImmediate(otherAsset);
            }
        }

        /// <summary>
        /// A cancelled edit also reverts the writes made to other cells.
        /// </summary>
        [Test]
        public void WriteValue_CancelledEdit_Reverted()
        {
            PipelineTestSO otherAsset = ScriptableObject.CreateInstance<PipelineTestSO>();
            otherAsset.number = 5;
            try
            {
                using (CellEditSession session = new CellEditSession(_asset, NumberPath, "Edit Number"))
                {
                    session.Write(10);
                    CellEditSession.WriteValue(otherAsset, NumberPath, 10);
                    session.Cancel();
                }

                Assert.AreEqual(3, _asset.number);
                Assert.AreEqual(5, otherAsset.number);
            }
            finally
            {
                Object.DestroyImmediate(otherAsset);
            }
        }

        /// <summary>
        /// A write to a property that doesn't exist throws, like the start of an edit.
        /// </summary>
        [Test]
        public void WriteValue_UnknownProperty_Throws()
        {
            Assert.Throws<ArgumentException>(() => CellEditSession.WriteValue(_asset, "_unknown", 10));
        }

        #endregion


        #region Private API

        /// <summary>
        /// Writes a value to a property of an asset, and confirms the edit.
        /// </summary>
        /// <param name="asset">The asset to edit.</param>
        /// <param name="propertyPath">The path of the property to write.</param>
        /// <param name="value">The value to write.</param>
        private static void WriteAndConfirm(Object asset, string propertyPath, object value)
        {
            using (CellEditSession session = new CellEditSession(asset, propertyPath, "Edit"))
            {
                session.Write(value);
                session.Confirm();
            }
        }

        #endregion

    }

}
