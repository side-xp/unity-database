using System;
using System.Collections;
using System.Globalization;

using Object = UnityEngine.Object;

namespace SideXP.Database.EditorOnly
{

    /// <summary>
    /// The default conversion of values to text and back, used by the cell handlers that don't define their own.
    /// </summary>
    /// <remarks>
    /// Texts don't depend on the culture of the user (<see cref="CultureInfo.InvariantCulture"/>), so a text copied or exported on a
    /// computer can be pasted or imported on another one.
    /// </remarks>
    internal static class TextConversion
    {

        #region Fields

        /// <summary>
        /// The format of floating point numbers: the shortest text that converts back to the same value.
        /// </summary>
        private const string RoundTripFormat = "R";

        #endregion


        #region Public API

        /// <summary>
        /// Converts a value to text.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <returns>Returns the converted value: an empty string for null, the asset name for a Unity object, the number of elements
        /// for a collection, invariant culture formats for numbers, and names for enums.</returns>
        public static string ToText(object value)
        {
            switch (value)
            {
                case null:
                    return string.Empty;

                case string text:
                    return text;

                // Check Unity objects before the other types, since a missing or destroyed reference is not null for C#
                case Object unityObject:
                    return unityObject != null ? unityObject.name : string.Empty;

                case float number:
                    return number.ToString(RoundTripFormat, CultureInfo.InvariantCulture);

                case double number:
                    return number.ToString(RoundTripFormat, CultureInfo.InvariantCulture);

                case ICollection collection:
                    return collection.Count == 1 ? "1 element" : $"{collection.Count} elements";

                case IFormattable formattable:
                    return formattable.ToString(null, CultureInfo.InvariantCulture);

                default:
                    return value.ToString();
            }
        }

        /// <summary>
        /// Converts a text to a value of a given type.
        /// </summary>
        /// <param name="text">The text to convert.</param>
        /// <param name="valueType">The type of the value to get: a string, a number, a boolean, a character or an enum.</param>
        /// <param name="value">Outputs the converted value, or null if the text can't be converted.</param>
        /// <returns>Returns true if the text has been converted.</returns>
        public static bool TryParse(string text, Type valueType, out object value)
        {
            value = null;
            if (text == null || valueType == null)
                return false;

            if (valueType == typeof(string))
            {
                value = text;
                return true;
            }

            string trimmedText = text.Trim();
            if (valueType.IsEnum)
                return Enum.TryParse(valueType, trimmedText, true, out value);

            CultureInfo culture = CultureInfo.InvariantCulture;
            bool isParsed = false;
            switch (Type.GetTypeCode(valueType))
            {
                case TypeCode.Boolean:
                    isParsed = bool.TryParse(trimmedText, out bool boolValue);
                    value = boolValue;
                    break;

                case TypeCode.Char:
                    isParsed = char.TryParse(text, out char charValue);
                    value = charValue;
                    break;

                case TypeCode.SByte:
                    isParsed = sbyte.TryParse(trimmedText, NumberStyles.Integer, culture, out sbyte sbyteValue);
                    value = sbyteValue;
                    break;

                case TypeCode.Byte:
                    isParsed = byte.TryParse(trimmedText, NumberStyles.Integer, culture, out byte byteValue);
                    value = byteValue;
                    break;

                case TypeCode.Int16:
                    isParsed = short.TryParse(trimmedText, NumberStyles.Integer, culture, out short shortValue);
                    value = shortValue;
                    break;

                case TypeCode.UInt16:
                    isParsed = ushort.TryParse(trimmedText, NumberStyles.Integer, culture, out ushort ushortValue);
                    value = ushortValue;
                    break;

                case TypeCode.Int32:
                    isParsed = int.TryParse(trimmedText, NumberStyles.Integer, culture, out int intValue);
                    value = intValue;
                    break;

                case TypeCode.UInt32:
                    isParsed = uint.TryParse(trimmedText, NumberStyles.Integer, culture, out uint uintValue);
                    value = uintValue;
                    break;

                case TypeCode.Int64:
                    isParsed = long.TryParse(trimmedText, NumberStyles.Integer, culture, out long longValue);
                    value = longValue;
                    break;

                case TypeCode.UInt64:
                    isParsed = ulong.TryParse(trimmedText, NumberStyles.Integer, culture, out ulong ulongValue);
                    value = ulongValue;
                    break;

                case TypeCode.Single:
                    isParsed = float.TryParse(trimmedText, NumberStyles.Float, culture, out float floatValue);
                    value = floatValue;
                    break;

                case TypeCode.Double:
                    isParsed = double.TryParse(trimmedText, NumberStyles.Float, culture, out double doubleValue);
                    value = doubleValue;
                    break;

                case TypeCode.Decimal:
                    isParsed = decimal.TryParse(trimmedText, NumberStyles.Number, culture, out decimal decimalValue);
                    value = decimalValue;
                    break;
            }

            if (!isParsed)
                value = null;

            return isParsed;
        }

        #endregion

    }

}
