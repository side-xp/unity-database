using System;
using System.Reflection;

using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;

namespace SideXP.Database.EditorOnly
{

    /// <summary>
    /// Handles the fields marked with <see cref="RangeAttribute"/>: edits them with a slider and a number field, like the Inspector,
    /// and clamps the edited and parsed values to the range.
    /// </summary>
    /// <remarks>
    /// The slider is used for <see cref="float"/> and <see cref="int"/> fields, like in the Inspector, and only in the cells wide
    /// enough to use it: narrower cells show the number field alone. The other number types get their default field, clamped when the
    /// edit is confirmed. On a field that is not a number, the handler behaves like the default one.
    /// </remarks>
    [CustomCellHandler(typeof(RangeAttribute))]
    internal class RangeCellHandler : CellHandler
    {

        #region Subclasses

        /// <summary>
        /// The edit field of a range: a slider and a number field, synced together. The slider is hidden when the cell is narrower
        /// than <see cref="MinSliderCellWidth"/>.
        /// </summary>
        /// <remarks>
        /// The number field holds the edited value: it's not delayed, so its value is up to date when the edit is confirmed, and it
        /// keeps the typed numbers outside of the range until they are clamped. The slider is not focusable, so the focus (and the
        /// typed characters) go to the number field.
        /// </remarks>
        private sealed class RangeEditField : VisualElement
        {

            /// <summary>
            /// The number field, which holds the edited value.
            /// </summary>
            public readonly VisualElement NumberField;

            /// <inheritdoc cref="RangeEditField"/>
            /// <param name="slider">The slider.</param>
            /// <param name="numberField"><inheritdoc cref="NumberField" path="/summary"/></param>
            public RangeEditField(VisualElement slider, VisualElement numberField)
            {
                NumberField = numberField;
                AddToClassList(EditFieldClass);
                AddToClassList(RangeFieldClass);

                slider.focusable = false;
                slider.AddToClassList(RangeSliderClass);
                Add(slider);

                numberField.AddToClassList(RangeNumberClass);
                Add(numberField);

                RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            }

            /// <summary>
            /// Called when the field changes size, for example when the column is resized. Shows the slider only if the cell is wide
            /// enough.
            /// </summary>
            /// <param name="evt">The emitted event.</param>
            private void OnGeometryChanged(GeometryChangedEvent evt)
            {
                // The width of the cell, not of this field: hiding the slider doesn't change it, so this never loops
                float cellWidth = parent != null ? parent.layout.width : layout.width;
                EnableInClassList(CompactRangeFieldClass, cellWidth < MinSliderCellWidth);
            }

        }

        #endregion


        #region Fields

        private const string RangeFieldClass = "database-cell__range";
        private const string CompactRangeFieldClass = "database-cell__range--compact";
        private const string RangeSliderClass = "database-cell__range-slider";
        private const string RangeNumberClass = "database-cell__range-number";

        /// <summary>
        /// The minimum width of a cell to show the slider, in pixels. Below it, the slider would be too short to pick a value, and
        /// would take the room of the number field.
        /// </summary>
        private const float MinSliderCellWidth = 120;

        #endregion


        #region Edit

        /// <inheritdoc/>
        public override VisualElement CreateEditField(SerializedProperty property, object value, CellContext context)
        {
            RangeAttribute range = context.Field?.GetCustomAttribute<RangeAttribute>(true);
            RangeEditField field = range != null ? CreateRangeField(range, value, context.ValueType) : null;
            return field ?? base.CreateEditField(property, value, context);
        }

        /// <inheritdoc/>
        public override bool TryGetEditedValue(VisualElement field, CellContext context, out object value)
        {
            bool hasValue = field is RangeEditField rangeField
                ? EditFields.TryGetValue(rangeField.NumberField, context.ValueType, out value)
                : base.TryGetEditedValue(field, context, out value);

            if (!hasValue)
                return false;

            value = ClampToFieldLimits(value, context);
            return true;
        }

        #endregion


        #region Text conversion

        /// <summary>
        /// Converts a text to a number clamped to the range of the field.
        /// </summary>
        /// <inheritdoc/>
        public override bool TryParse(string text, CellContext context, out object value)
        {
            if (!base.TryParse(text, context, out value))
                return false;

            value = ClampToFieldLimits(value, context);
            return true;
        }

        #endregion


        #region Private API

        /// <summary>
        /// Creates the edit field of a range, with a slider and a number field synced together.
        /// </summary>
        /// <param name="range">The range of the field.</param>
        /// <param name="value">The current value of the cell.</param>
        /// <param name="valueType">The type of the values of the column.</param>
        /// <returns>Returns the created field, or null if the type has no slider (only <see cref="float"/> and <see cref="int"/>
        /// have one).</returns>
        private static RangeEditField CreateRangeField(RangeAttribute range, object value, Type valueType)
        {
            if (valueType == typeof(float))
            {
                float number = value is float floatValue ? floatValue : 0;
                Slider slider = new Slider(range.min, range.max) { value = number };
                FloatField numberField = (FloatField)EditFields.Create(typeof(float), number);
                slider.RegisterValueChangedCallback(evt => numberField.SetValueWithoutNotify(evt.newValue));
                numberField.RegisterValueChangedCallback(evt => slider.SetValueWithoutNotify(evt.newValue));
                return new RangeEditField(slider, numberField);
            }

            if (valueType == typeof(int))
            {
                int number = value is int intValue ? intValue : 0;
                // Truncated limits, like the Inspector
                SliderInt slider = new SliderInt((int)range.min, (int)range.max) { value = number };
                IntegerField numberField = (IntegerField)EditFields.Create(typeof(int), number);
                slider.RegisterValueChangedCallback(evt => numberField.SetValueWithoutNotify(evt.newValue));
                numberField.RegisterValueChangedCallback(evt => slider.SetValueWithoutNotify(evt.newValue));
                return new RangeEditField(slider, numberField);
            }

            return null;
        }

        #endregion

    }

}
