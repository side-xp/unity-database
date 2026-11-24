using System;

using UnityEditor;

namespace SideXP.Database.EditorOnly
{

    /// <summary>
    /// The default comparison of values, used to sort the columns whose cell handler doesn't define its own.
    /// </summary>
    internal static class ValueComparison
    {

        #region Public API

        /// <summary>
        /// Checks if the values of a type can be compared.
        /// </summary>
        /// <param name="valueType">The type of the values.</param>
        /// <returns>Returns true if the type implements <see cref="IComparable"/>, like strings, numbers, booleans and enums.</returns>
        public static bool CanCompare(Type valueType)
        {
            return valueType != null && typeof(IComparable).IsAssignableFrom(valueType);
        }

        /// <summary>
        /// Compares two values.
        /// </summary>
        /// <remarks>
        /// Null values come first. Strings use the natural order of the Project window, so numbers in names are compared by value
        /// (<c>Sword 2</c> before <c>Sword 10</c>). Other values use <see cref="IComparable"/>. Values of different types (for example
        /// in a field of a base type) are compared by their text.
        /// </remarks>
        /// <param name="a">The first value.</param>
        /// <param name="b">The second value.</param>
        /// <returns>Returns a negative number if <paramref name="a"/> comes before <paramref name="b"/>, a positive number if it comes
        /// after, or 0 if they are equal.</returns>
        public static int Compare(object a, object b)
        {
            if (a == null && b == null)
                return 0;

            if (a == null)
                return -1;

            if (b == null)
                return 1;

            if (a is string textA && b is string textB)
                return EditorUtility.NaturalCompare(textA, textB);

            if (a.GetType() == b.GetType() && a is IComparable comparable)
                return comparable.CompareTo(b);

            return EditorUtility.NaturalCompare(TextConversion.ToText(a), TextConversion.ToText(b));
        }

        #endregion

    }

}
