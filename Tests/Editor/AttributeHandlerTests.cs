using System.Reflection;

using NUnit.Framework;

using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

using SideXP.Core;
using SideXP.Core.EditorOnly;
using SideXP.Database.EditorOnly;

namespace SideXP.Database.Tests
{

    /// <summary>
    /// Tests for the built-in cell handlers of Unity's attributes (<see cref="RangeAttribute"/>, <see cref="MinAttribute"/>,
    /// <see cref="ColorUsageAttribute"/>), and for the limits they apply (<see cref="FieldLimits"/>).
    /// </summary>
    public class AttributeHandlerTests
    {

        #region Handler selection

        /// <summary>
        /// The fields marked with Unity's attributes use the handlers of these attributes.
        /// </summary>
        [Test]
        public void GetHandler_UnityAttributes_UsesAttributeHandlers()
        {
            Assert.IsInstanceOf<RangeCellHandler>(GetHandler(nameof(AttributeHandlerTestFields.rangeFloat)));
            Assert.IsInstanceOf<MinCellHandler>(GetHandler(nameof(AttributeHandlerTestFields.minFloat)));
            Assert.IsInstanceOf<ColorUsageCellHandler>(GetHandler(nameof(AttributeHandlerTestFields.hdrColor)));
        }

        /// <summary>
        /// The fields marked with <see cref="AssetDropdownAttribute"/> use its handler, even if the dropdown doesn't support their type.
        /// </summary>
        [Test]
        public void GetHandler_AssetDropdown_UsesAssetDropdownHandler()
        {
            Assert.IsInstanceOf<AssetDropdownCellHandler>(GetHandler(nameof(AttributeHandlerTestFields.assetDropdown)));
            Assert.IsInstanceOf<AssetDropdownCellHandler>(GetHandler(nameof(AttributeHandlerTestFields.unsupportedAssetDropdown)));
        }

        #endregion


        #region Asset dropdown handler

        /// <summary>
        /// A supported field is a live dropdown, without edit mode.
        /// </summary>
        [Test]
        public void AssetDropdownHandler_SupportedField_LiveDropdownWithoutEditField()
        {
            AssetDropdownCellHandler handler = new AssetDropdownCellHandler();
            CellContext context = CreateContext(nameof(AttributeHandlerTestFields.assetDropdown));
            VisualElement display = handler.CreateDisplay();
            handler.BindDisplay(display, null, context);

            Assert.IsInstanceOf<AssetDropdownField>(display[0]);
            Assert.AreEqual(typeof(ItemSO), ((AssetDropdownField)display[0]).AssetType);
            Assert.IsFalse(display[0].focusable);
            Assert.IsNull(handler.CreateEditField(null, null, context));
        }

        /// <summary>
        /// A field of a type the dropdown doesn't support is displayed and edited like the other references.
        /// </summary>
        [Test]
        public void AssetDropdownHandler_UnsupportedField_LikeOtherReferences()
        {
            AssetDropdownCellHandler handler = new AssetDropdownCellHandler();
            CellContext context = CreateContext(nameof(AttributeHandlerTestFields.unsupportedAssetDropdown));
            VisualElement display = handler.CreateDisplay();
            handler.BindDisplay(display, null, context);

            Assert.IsNotInstanceOf<AssetDropdownField>(display[0]);
            Assert.IsInstanceOf<ObjectField>(handler.CreateEditField(null, null, context));
        }

        #endregion


        #region Field limits

        /// <summary>
        /// Numbers are clamped to the range of their field, and the values within the range are unchanged.
        /// </summary>
        [Test]
        public void Clamp_Range_ClampsNumbers()
        {
            FieldInfo field = GetField(nameof(AttributeHandlerTestFields.rangeFloat));

            Assert.AreEqual(1f, FieldLimits.Clamp(1.5f, field));
            Assert.AreEqual(0f, FieldLimits.Clamp(-2f, field));
            Assert.AreEqual(0.25f, FieldLimits.Clamp(0.25f, field));
        }

        /// <summary>
        /// The limits are truncated for integer fields, like in the Inspector, and the result keeps the type of the field.
        /// </summary>
        [Test]
        public void Clamp_IntegerField_TruncatesLimits()
        {
            FieldInfo rangeField = GetField(nameof(AttributeHandlerTestFields.rangeInt));
            Assert.AreEqual(10, FieldLimits.Clamp(20, rangeField));
            Assert.AreEqual(0, FieldLimits.Clamp(-1, rangeField));

            Assert.AreEqual(1, FieldLimits.Clamp(0, GetField(nameof(AttributeHandlerTestFields.minInt))));
            Assert.AreEqual((byte)100, FieldLimits.Clamp((byte)200, GetField(nameof(AttributeHandlerTestFields.rangeByte))));
        }

        /// <summary>
        /// Numbers are clamped to the minimum of their field.
        /// </summary>
        [Test]
        public void Clamp_Min_ClampsNumbers()
        {
            FieldInfo field = GetField(nameof(AttributeHandlerTestFields.minFloat));

            Assert.AreEqual(2f, FieldLimits.Clamp(-5f, field));
            Assert.AreEqual(3f, FieldLimits.Clamp(3f, field));
        }

        /// <summary>
        /// A field with a range and a minimum applies both.
        /// </summary>
        [Test]
        public void Clamp_RangeAndMin_AppliesBoth()
        {
            FieldInfo field = GetField(nameof(AttributeHandlerTestFields.rangeAndMin));

            Assert.AreEqual(5f, FieldLimits.Clamp(1f, field));
            Assert.AreEqual(10f, FieldLimits.Clamp(20f, field));
        }

        /// <summary>
        /// The values that are not numbers, and the fields without limits, are unchanged.
        /// </summary>
        [Test]
        public void Clamp_NotANumberOrNoLimit_ReturnsValue()
        {
            Assert.AreEqual("text", FieldLimits.Clamp("text", GetField(nameof(AttributeHandlerTestFields.rangeString))));
            Assert.AreEqual(50f, FieldLimits.Clamp(50f, GetField(nameof(AttributeHandlerTestFields.plainFloat))));
            Assert.AreEqual(50f, FieldLimits.Clamp(50f, null));
        }

        #endregion


        #region Range handler

        /// <summary>
        /// Float and int fields are edited with a slider and a number field, the other types with their default field.
        /// </summary>
        [Test]
        public void RangeHandler_CreateEditField_UsesSliders()
        {
            RangeCellHandler handler = new RangeCellHandler();

            VisualElement floatField = CreateEditField(handler, nameof(AttributeHandlerTestFields.rangeFloat), 0.5f);
            Assert.IsNotNull(floatField.Q<Slider>());
            Assert.IsNotNull(floatField.Q<FloatField>());

            VisualElement intField = CreateEditField(handler, nameof(AttributeHandlerTestFields.rangeInt), 3);
            Assert.IsNotNull(intField.Q<SliderInt>());
            Assert.IsNotNull(intField.Q<IntegerField>());

            Assert.IsInstanceOf<IntegerField>(CreateEditField(handler, nameof(AttributeHandlerTestFields.rangeByte), (byte)3));
            Assert.IsInstanceOf<TextField>(CreateEditField(handler, nameof(AttributeHandlerTestFields.rangeString), "text"));
        }

        /// <summary>
        /// The value of the number field is the edited value, clamped to the range.
        /// </summary>
        /// <remarks>
        /// The sync between the slider and the number field isn't tested: the change events are only sent to the elements of a panel.
        /// </remarks>
        [Test]
        public void RangeHandler_SliderField_ReadsAndClampsNumberField()
        {
            RangeCellHandler handler = new RangeCellHandler();
            CellContext context = CreateContext(nameof(AttributeHandlerTestFields.rangeFloat));
            VisualElement field = handler.CreateEditField(null, 0.5f, context);
            field.Q<FloatField>().value = 5;

            Assert.IsTrue(handler.TryGetEditedValue(field, context, out object edited));
            Assert.AreEqual(1f, edited);
        }

        /// <summary>
        /// The values read from the edit fields, and the parsed texts, are clamped to the range.
        /// </summary>
        [Test]
        public void RangeHandler_EditedAndParsedValues_AreClamped()
        {
            RangeCellHandler handler = new RangeCellHandler();
            CellContext byteContext = CreateContext(nameof(AttributeHandlerTestFields.rangeByte));
            IntegerField byteField = (IntegerField)handler.CreateEditField(null, (byte)3, byteContext);
            byteField.value = 150;

            Assert.IsTrue(handler.TryGetEditedValue(byteField, byteContext, out object edited));
            Assert.AreEqual((byte)100, edited);

            Assert.IsTrue(handler.TryParse("5", CreateContext(nameof(AttributeHandlerTestFields.rangeFloat)), out object parsed));
            Assert.AreEqual(1f, parsed);
        }

        #endregion


        #region Min handler

        /// <summary>
        /// The values read from the edit fields, and the parsed texts, are clamped to the minimum.
        /// </summary>
        [Test]
        public void MinHandler_EditedAndParsedValues_AreClamped()
        {
            MinCellHandler handler = new MinCellHandler();
            CellContext context = CreateContext(nameof(AttributeHandlerTestFields.minFloat));
            FloatField field = (FloatField)handler.CreateEditField(null, 3f, context);
            field.value = -1;

            Assert.IsTrue(handler.TryGetEditedValue(field, context, out object edited));
            Assert.AreEqual(2f, edited);

            Assert.IsTrue(handler.TryParse("-10", context, out object parsed));
            Assert.AreEqual(2f, parsed);
        }

        #endregion


        #region Color usage handler

        /// <summary>
        /// The color field follows the options of the attribute.
        /// </summary>
        [Test]
        public void ColorUsageHandler_CreateEditField_AppliesOptions()
        {
            ColorUsageCellHandler handler = new ColorUsageCellHandler();
            ColorField field = (ColorField)CreateEditField(handler, nameof(AttributeHandlerTestFields.hdrColor), Color.red);

            Assert.IsTrue(field.hdr);
            Assert.IsFalse(field.showAlpha);
        }

        #endregion


        #region Private API

        /// <summary>
        /// Gets a field of <see cref="AttributeHandlerTestFields"/>.
        /// </summary>
        /// <param name="name">The name of the field.</param>
        /// <returns>Returns the found field.</returns>
        private static FieldInfo GetField(string name)
        {
            return typeof(AttributeHandlerTestFields).GetField(name, BindingFlags.Instance | BindingFlags.Public);
        }

        /// <summary>
        /// Gets the handler that the registry selects for a field of <see cref="AttributeHandlerTestFields"/>.
        /// </summary>
        /// <param name="name">The name of the field.</param>
        /// <returns>Returns the selected handler.</returns>
        private static CellHandler GetHandler(string name)
        {
            FieldInfo field = GetField(name);
            return CellHandlerRegistry.Default.GetHandler(field, field.FieldType);
        }

        /// <summary>
        /// Creates the context of a cell of a field of <see cref="AttributeHandlerTestFields"/>.
        /// </summary>
        /// <param name="name">The name of the field.</param>
        /// <returns>Returns the created context, without asset.</returns>
        private static CellContext CreateContext(string name)
        {
            FieldInfo field = GetField(name);
            return new CellContext(null, field, field.FieldType);
        }

        /// <summary>
        /// Creates the edit field of a cell of a field of <see cref="AttributeHandlerTestFields"/>, without a property.
        /// </summary>
        /// <param name="handler">The handler that creates the field.</param>
        /// <param name="name">The name of the field.</param>
        /// <param name="value">The value of the cell.</param>
        /// <returns>Returns the created field.</returns>
        private static VisualElement CreateEditField(CellHandler handler, string name, object value)
        {
            return handler.CreateEditField(null, value, CreateContext(name));
        }

        #endregion

    }

}
