using System;
using System.Globalization;

using UnityEngine;
using UnityEngine.UIElements;

namespace SideXP.Database.EditorOnly
{

    /// <summary>
    /// The default edit fields, used by the cell handlers that don't define their own: a text field for strings, and number fields for
    /// numbers.
    /// </summary>
    /// <remarks>
    /// The fields are not bound to the serialized property: their value is written when the edit is confirmed, so nothing changes
    /// while the user types, and cancelling an edit has nothing to revert. The fields update their value on each change
    /// (<see cref="TextInputBaseField{T}.isDelayed"/> disabled), so the value is up to date whenever the edit is confirmed.
    /// </remarks>
    internal static class EditFields
    {

        #region Public API

        /// <summary>
        /// Checks if a type of value has a default edit field.
        /// </summary>
        /// <param name="valueType">The type of the value.</param>
        /// <returns>Returns true for strings and numbers.</returns>
        public static bool CanEdit(Type valueType)
        {
            if (valueType == null || valueType.IsEnum)
                return false;

            switch (Type.GetTypeCode(valueType))
            {
                case TypeCode.String:
                case TypeCode.SByte:
                case TypeCode.Byte:
                case TypeCode.Int16:
                case TypeCode.UInt16:
                case TypeCode.Int32:
                case TypeCode.UInt32:
                case TypeCode.Int64:
                case TypeCode.UInt64:
                case TypeCode.Single:
                case TypeCode.Double:
                    return true;

                default:
                    return false;
            }
        }

        /// <summary>
        /// Creates the default edit field of a type of value.
        /// </summary>
        /// <param name="valueType">The type of the value.</param>
        /// <param name="value">The current value of the cell.</param>
        /// <returns>Returns the created field, or null if the type has no default edit field.</returns>
        public static VisualElement Create(Type valueType, object value)
        {
            if (!CanEdit(valueType))
                return null;

            CultureInfo culture = CultureInfo.InvariantCulture;
            VisualElement field;
            switch (Type.GetTypeCode(valueType))
            {
                case TypeCode.String:
                    field = new TextField { value = value as string ?? string.Empty, isDelayed = false };
                    break;

                // The smaller integer types fit in an int, and their range is checked when the value is written
                case TypeCode.SByte:
                case TypeCode.Byte:
                case TypeCode.Int16:
                case TypeCode.UInt16:
                case TypeCode.Int32:
                    field = new IntegerField { value = Convert.ToInt32(value ?? 0, culture), isDelayed = false };
                    break;

                case TypeCode.UInt32:
                    field = new UnsignedIntegerField { value = Convert.ToUInt32(value ?? 0u, culture), isDelayed = false };
                    break;

                case TypeCode.Int64:
                    field = new LongField { value = Convert.ToInt64(value ?? 0L, culture), isDelayed = false };
                    break;

                case TypeCode.UInt64:
                    field = new UnsignedLongField { value = Convert.ToUInt64(value ?? 0ul, culture), isDelayed = false };
                    break;

                case TypeCode.Single:
                    field = new FloatField { value = Convert.ToSingle(value ?? 0f, culture), isDelayed = false };
                    break;

                case TypeCode.Double:
                    field = new DoubleField { value = Convert.ToDouble(value ?? 0d, culture), isDelayed = false };
                    break;

                default:
                    return null;
            }

            field.AddToClassList(CellHandler.EditFieldClass);
            return field;
        }

        /// <summary>
        /// Gets the value of a field created by <see cref="Create(Type, object)"/>, converted to the type of the column.
        /// </summary>
        /// <param name="field">The edit field.</param>
        /// <param name="valueType">The type of the values of the column.</param>
        /// <param name="value">Outputs the value of the field, converted to the type of the column.</param>
        /// <returns>Returns true if the value has been read and converted. Returns false and logs a warning if the value doesn't fit in
        /// the type of the column (for example 300 for a <see cref="byte"/>).</returns>
        public static bool TryGetValue(VisualElement field, Type valueType, out object value)
        {
            switch (field)
            {
                case TextField textField:
                    value = textField.value;
                    break;

                case IntegerField integerField:
                    value = integerField.value;
                    break;

                case UnsignedIntegerField unsignedIntegerField:
                    value = unsignedIntegerField.value;
                    break;

                case LongField longField:
                    value = longField.value;
                    break;

                case UnsignedLongField unsignedLongField:
                    value = unsignedLongField.value;
                    break;

                case FloatField floatField:
                    value = floatField.value;
                    break;

                case DoubleField doubleField:
                    value = doubleField.value;
                    break;

                default:
                    value = null;
                    return false;
            }

            if (value.GetType() == valueType)
                return true;

            try
            {
                value = Convert.ChangeType(value, valueType, CultureInfo.InvariantCulture);
                return true;
            }
            catch (OverflowException)
            {
                Debug.LogWarning($"{DatabaseConstants.LogPrefix}The value {TextConversion.ToText(value)} doesn't fit in a " +
                    $"{valueType.Name}: the edit is ignored.");
                value = null;
                return false;
            }
        }

        #endregion

    }

}
