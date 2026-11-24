using System;
using System.Globalization;
using System.Reflection;

using UnityEngine;

namespace SideXP.Database.EditorOnly
{

    /// <summary>
    /// Applies the limits that Unity's attributes declare on a number field: <see cref="RangeAttribute"/> and
    /// <see cref="MinAttribute"/>.
    /// </summary>
    /// <remarks>
    /// The limits are applied like in the Inspector: values are clamped (not rejected), and the limits are truncated to integers for
    /// integer fields. Only numbers are clamped: the other values (including the vectors that <see cref="MinAttribute"/> also supports
    /// in the Inspector) are returned unchanged.
    /// </remarks>
    internal static class FieldLimits
    {

        #region Public API

        /// <summary>
        /// Clamps a number to the limits of a field.
        /// </summary>
        /// <param name="value">The value to clamp, of the type of the field.</param>
        /// <param name="field">The field whose attributes declare the limits, or null if there is none.</param>
        /// <returns>Returns the clamped value, of the same type, or the value itself if it's not a number, if it's within the limits, or
        /// if the field declares no limit.</returns>
        public static object Clamp(object value, FieldInfo field)
        {
            if (value == null || field == null || !IsNumber(value.GetType()))
                return value;

            RangeAttribute range = field.GetCustomAttribute<RangeAttribute>(true);
            MinAttribute min = field.GetCustomAttribute<MinAttribute>(true);
            if (range == null && min == null)
                return value;

            CultureInfo culture = CultureInfo.InvariantCulture;
            bool isInteger = IsInteger(value.GetType());
            double number = Convert.ToDouble(value, culture);
            double clamped = number;

            // Not Math.Clamp(), which throws if the limits are inverted
            if (range != null)
                clamped = Math.Min(Math.Max(clamped, GetLimit(range.min, isInteger)), GetLimit(range.max, isInteger));

            if (min != null)
                clamped = Math.Max(clamped, GetLimit(min.min, isInteger));

            if (clamped == number)
                return value;

            try
            {
                return Convert.ChangeType(clamped, value.GetType(), culture);
            }
            catch (OverflowException)
            {
                // A limit outside of the range of the type (for example a minimum of 300 for a byte) can't be applied
                return value;
            }
        }

        #endregion


        #region Private API

        /// <summary>
        /// Gets a limit for a field, truncated for integer fields, like the Inspector does.
        /// </summary>
        /// <param name="limit">The limit declared by the attribute.</param>
        /// <param name="isInteger">Checks if the field is an integer.</param>
        /// <returns>Returns the limit to apply.</returns>
        private static double GetLimit(float limit, bool isInteger)
        {
            return isInteger ? Math.Truncate(limit) : limit;
        }

        /// <summary>
        /// Checks if a type is a number.
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <returns>Returns true for the integer and floating point types, but not for enums and characters.</returns>
        private static bool IsNumber(Type type)
        {
            if (type.IsEnum)
                return false;

            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Single:
                case TypeCode.Double:
                case TypeCode.Decimal:
                    return true;

                default:
                    return IsInteger(type);
            }
        }

        /// <summary>
        /// Checks if a type is an integer.
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <returns>Returns true for the integer types, but not for enums and characters.</returns>
        private static bool IsInteger(Type type)
        {
            if (type.IsEnum)
                return false;

            switch (Type.GetTypeCode(type))
            {
                case TypeCode.SByte:
                case TypeCode.Byte:
                case TypeCode.Int16:
                case TypeCode.UInt16:
                case TypeCode.Int32:
                case TypeCode.UInt32:
                case TypeCode.Int64:
                case TypeCode.UInt64:
                    return true;

                default:
                    return false;
            }
        }

        #endregion

    }

}
