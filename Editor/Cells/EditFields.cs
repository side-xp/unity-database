using System;
using System.Globalization;

using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.UIElements;

using Object = UnityEngine.Object;

namespace SideXP.Database.EditorOnly
{

    /// <summary>
    /// The default edit fields, used by the cell handlers that don't define their own: the UI Toolkit field that matches the type of
    /// the value, or a text field that goes through the text conversion of the handler.
    /// </summary>
    /// <remarks>
    /// The fields are not bound to the serialized property: their value is written when the edit is confirmed, so nothing changes
    /// while the user types, and cancelling an edit has nothing to revert. Text fields update their value on each change
    /// (<see cref="TextInputBaseField{T}.isDelayed"/> disabled), so the value is up to date whenever the edit is confirmed. Curves
    /// and gradients are the exception: their fields are bound (see <see cref="CreateBoundField(Type, SerializedProperty)"/>).
    /// </remarks>
    internal static class EditFields
    {

        #region Subclasses

        /// <summary>
        /// A text field whose text is converted to a value by the text conversion of a cell handler, for the types that have no
        /// matching field.
        /// </summary>
        private sealed class TextConversionField : TextField { }

        #endregion


        #region Fields

        /// <summary>
        /// The value read from a field that is not a default edit field. A sentinel, since null is a valid value (an empty reference).
        /// </summary>
        private static readonly object NotADefaultField = new object();

        /// <summary>
        /// The USS class of the edit fields bound to their property, which write it themselves.
        /// </summary>
        private const string BoundFieldClass = "database-cell__edit-field--bound";

        #endregion


        #region Public API

        /// <summary>
        /// Creates the default edit field of a type of value.
        /// </summary>
        /// <remarks>
        /// Supported types: strings, characters, numbers, booleans, enums (with flags or not), references to Unity objects, colors,
        /// vectors, rectangles, bounds, quaternions (as Euler angles), layer masks and <see cref="Hash128"/>. With a property, curves
        /// and gradients too.
        /// </remarks>
        /// <param name="valueType">The type of the value.</param>
        /// <param name="value">The current value of the cell.</param>
        /// <param name="property">The edited property, used to bind the fields of the types that must be bound (curves and gradients),
        /// or null to only create unbound fields.</param>
        /// <returns>Returns the created field, or null if the type has no default edit field.</returns>
        public static VisualElement Create(Type valueType, object value, SerializedProperty property = null)
        {
            VisualElement field = property != null ? CreateBoundField(valueType, property) : null;
            field ??= CreateField(valueType, value);
            field?.AddToClassList(CellHandler.EditFieldClass);
            return field;
        }

        /// <summary>
        /// Marks an edit field as bound to its property: it writes the property itself, so there is no value to write when the edit is
        /// confirmed.
        /// </summary>
        /// <param name="field">The edit field.</param>
        public static void MarkAsBound(VisualElement field)
        {
            field.AddToClassList(BoundFieldClass);
        }

        /// <summary>
        /// Checks if an edit field is bound to its property (see <see cref="MarkAsBound(VisualElement)"/>).
        /// </summary>
        /// <param name="field">The edit field.</param>
        /// <returns>Returns true if the field writes its property itself.</returns>
        public static bool IsBound(VisualElement field)
        {
            return field != null && field.ClassListContains(BoundFieldClass);
        }

        /// <summary>
        /// Creates a text field whose text is converted to a value by the text conversion of a cell handler.
        /// </summary>
        /// <param name="text">The text of the current value.</param>
        /// <returns>Returns the created field.</returns>
        public static VisualElement CreateTextConversionField(string text)
        {
            TextConversionField field = new TextConversionField { value = text ?? string.Empty, isDelayed = false };
            field.AddToClassList(CellHandler.EditFieldClass);
            return field;
        }

        /// <summary>
        /// Gets the text of a field created by <see cref="CreateTextConversionField(string)"/>.
        /// </summary>
        /// <param name="field">The edit field.</param>
        /// <param name="text">Outputs the text of the field.</param>
        /// <returns>Returns true if the field is a text conversion field.</returns>
        public static bool TryGetConvertedText(VisualElement field, out string text)
        {
            text = field is TextConversionField textField ? textField.value : null;
            return text != null;
        }

        /// <summary>
        /// Gets the value of a field created by <see cref="Create(Type, object)"/>, converted to the type of the column.
        /// </summary>
        /// <param name="field">The edit field.</param>
        /// <param name="valueType">The type of the values of the column.</param>
        /// <param name="value">Outputs the value of the field, converted to the type of the column.</param>
        /// <returns>Returns true if the value has been read and converted. Returns false and logs a warning if the value doesn't fit in
        /// the type of the column (for example 300 for a <see cref="byte"/>, or an empty text for a <see cref="char"/>).</returns>
        public static bool TryGetValue(VisualElement field, Type valueType, out object value)
        {
            if (!TryReadField(field, out object fieldValue))
            {
                value = null;
                return false;
            }

            return TryConvert(fieldValue, valueType, out value);
        }

        #endregion


        #region Private API

        /// <summary>
        /// Creates the field of a type whose values must be edited by a field bound to their property.
        /// </summary>
        /// <remarks>
        /// Curves and gradients are reference types: their cached value is the instance of the asset itself, which an unbound field
        /// could change before the edit is confirmed. A bound field edits a copy, and writes it through the property.
        /// </remarks>
        /// <param name="valueType">The type of the value.</param>
        /// <param name="property">The edited property.</param>
        /// <returns>Returns the created field, bound to the property, or null if the type doesn't need a bound field.</returns>
        private static VisualElement CreateBoundField(Type valueType, SerializedProperty property)
        {
            VisualElement field;
            if (valueType == typeof(AnimationCurve))
            {
                CurveField curveField = new CurveField();
                curveField.BindProperty(property);
                field = curveField;
            }
            else if (valueType == typeof(Gradient))
            {
                GradientField gradientField = new GradientField();
                gradientField.BindProperty(property);
                field = gradientField;
            }
            else
            {
                return null;
            }

            MarkAsBound(field);
            return field;
        }

        /// <summary>
        /// Creates the field that matches a type of value, without its USS class.
        /// </summary>
        /// <inheritdoc cref="Create(Type, object)"/>
        private static VisualElement CreateField(Type valueType, object value)
        {
            if (valueType == null)
                return null;

            if (valueType.IsEnum)
            {
                Enum enumValue = (Enum)(value ?? Enum.ToObject(valueType, 0));
                return valueType.IsDefined(typeof(FlagsAttribute), false)
                    ? new EnumFlagsField(enumValue)
                    : new EnumField(enumValue);
            }

            if (typeof(Object).IsAssignableFrom(valueType))
                return new ObjectField { objectType = valueType, allowSceneObjects = false, value = value as Object };

            if (valueType == typeof(char))
            {
                string text = value is char character && character != '\0' ? character.ToString() : string.Empty;
                return new TextField { value = text, maxLength = 1, isDelayed = false };
            }

            CultureInfo culture = CultureInfo.InvariantCulture;
            switch (Type.GetTypeCode(valueType))
            {
                case TypeCode.String:
                    return new TextField { value = value as string ?? string.Empty, isDelayed = false };

                case TypeCode.Boolean:
                    return new Toggle { value = value is bool isOn && isOn };

                // The smaller integer types fit in an int, and their range is checked when the value is written
                case TypeCode.SByte:
                case TypeCode.Byte:
                case TypeCode.Int16:
                case TypeCode.UInt16:
                case TypeCode.Int32:
                    return new IntegerField { value = Convert.ToInt32(value ?? 0, culture), isDelayed = false };

                case TypeCode.UInt32:
                    return new UnsignedIntegerField { value = Convert.ToUInt32(value ?? 0u, culture), isDelayed = false };

                case TypeCode.Int64:
                    return new LongField { value = Convert.ToInt64(value ?? 0L, culture), isDelayed = false };

                case TypeCode.UInt64:
                    return new UnsignedLongField { value = Convert.ToUInt64(value ?? 0ul, culture), isDelayed = false };

                case TypeCode.Single:
                    return new FloatField { value = Convert.ToSingle(value ?? 0f, culture), isDelayed = false };

                case TypeCode.Double:
                    return new DoubleField { value = Convert.ToDouble(value ?? 0d, culture), isDelayed = false };
            }

            switch (value)
            {
                case Color color:
                    return new ColorField { value = color };

                case Color32 color32:
                    return new ColorField { value = color32 };

                case Vector2 vector2:
                    return new Vector2Field { value = vector2 };

                case Vector3 vector3:
                    return new Vector3Field { value = vector3 };

                case Vector4 vector4:
                    return new Vector4Field { value = vector4 };

                case Vector2Int vector2Int:
                    return new Vector2IntField { value = vector2Int };

                case Vector3Int vector3Int:
                    return new Vector3IntField { value = vector3Int };

                case Rect rect:
                    return new RectField { value = rect };

                case RectInt rectInt:
                    return new RectIntField { value = rectInt };

                case Bounds bounds:
                    return new BoundsField { value = bounds };

                case BoundsInt boundsInt:
                    return new BoundsIntField { value = boundsInt };

                // Quaternions are edited as Euler angles, like in the Inspector
                case Quaternion quaternion:
                    return new Vector3Field { value = quaternion.eulerAngles };

                case LayerMask layerMask:
                    return new LayerMaskField(layerMask.value);

                case Hash128 hash:
                    return new Hash128Field { value = hash };

                default:
                    return null;
            }
        }

        /// <summary>
        /// Reads the value of a field created by <see cref="Create(Type, object)"/>.
        /// </summary>
        /// <param name="field">The edit field.</param>
        /// <param name="value">Outputs the value of the field, of the type of the field.</param>
        /// <returns>Returns true if the field is a default edit field.</returns>
        private static bool TryReadField(VisualElement field, out object value)
        {
            value = field switch
            {
                TextField textField => (object)textField.value,
                Toggle toggle => toggle.value,
                IntegerField integerField => integerField.value,
                UnsignedIntegerField unsignedIntegerField => unsignedIntegerField.value,
                LongField longField => longField.value,
                UnsignedLongField unsignedLongField => unsignedLongField.value,
                FloatField floatField => floatField.value,
                DoubleField doubleField => doubleField.value,
                EnumField enumField => enumField.value,
                EnumFlagsField enumFlagsField => enumFlagsField.value,
                ObjectField objectField => objectField.value,
                ColorField colorField => colorField.value,
                Vector2Field vector2Field => vector2Field.value,
                Vector3Field vector3Field => vector3Field.value,
                Vector4Field vector4Field => vector4Field.value,
                Vector2IntField vector2IntField => vector2IntField.value,
                Vector3IntField vector3IntField => vector3IntField.value,
                RectField rectField => rectField.value,
                RectIntField rectIntField => rectIntField.value,
                BoundsField boundsField => boundsField.value,
                BoundsIntField boundsIntField => boundsIntField.value,
                LayerMaskField layerMaskField => layerMaskField.value,
                Hash128Field hashField => hashField.value,
                _ => NotADefaultField,
            };

            if (value != NotADefaultField)
                return true;

            value = null;
            return false;
        }

        /// <summary>
        /// Converts the value of a field to the type of the column, when the field edits another type (for example an
        /// <see cref="IntegerField"/> for a <see cref="byte"/>, or a <see cref="Vector3Field"/> for a <see cref="Quaternion"/>).
        /// </summary>
        /// <param name="fieldValue">The value of the field.</param>
        /// <param name="valueType">The type of the values of the column.</param>
        /// <param name="value">Outputs the converted value.</param>
        /// <returns>Returns true if the value has been converted. Returns false and logs a warning if it doesn't fit in the type of the
        /// column.</returns>
        private static bool TryConvert(object fieldValue, Type valueType, out object value)
        {
            value = fieldValue;

            // Only references can be null (None)
            if (fieldValue == null)
                return !valueType.IsValueType;

            if (valueType.IsInstanceOfType(fieldValue))
                return true;

            if (valueType == typeof(char))
            {
                if (fieldValue is string text && text.Length == 1)
                {
                    value = text[0];
                    return true;
                }

                LogRejected(fieldValue, valueType);
                value = null;
                return false;
            }

            if (valueType == typeof(Color32) && fieldValue is Color color)
            {
                value = (Color32)color;
                return true;
            }

            if (valueType == typeof(Quaternion) && fieldValue is Vector3 eulerAngles)
            {
                value = Quaternion.Euler(eulerAngles);
                return true;
            }

            if (valueType == typeof(LayerMask) && fieldValue is int mask)
            {
                value = (LayerMask)mask;
                return true;
            }

            try
            {
                value = Convert.ChangeType(fieldValue, valueType, CultureInfo.InvariantCulture);
                return true;
            }
            catch (Exception exception) when (exception is OverflowException || exception is InvalidCastException)
            {
                LogRejected(fieldValue, valueType);
                value = null;
                return false;
            }
        }

        /// <summary>
        /// Logs that the value of a field can't be written to a column.
        /// </summary>
        /// <param name="fieldValue">The value of the field.</param>
        /// <param name="valueType">The type of the values of the column.</param>
        private static void LogRejected(object fieldValue, Type valueType)
        {
            Debug.LogWarning($"{DatabaseConstants.LogPrefix}The value \"{TextConversion.ToText(fieldValue)}\" doesn't fit in a " +
                $"{valueType.Name}: the edit is ignored.");
        }

        #endregion

    }

}
