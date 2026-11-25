using System;
using System.Collections.Generic;
using System.Globalization;

using UnityEditor;

using Object = UnityEngine.Object;

namespace SideXP.Database.EditorOnly
{

    /// <summary>
    /// The rules of the filter editor that don't need its elements: the condition a column gets when it's added to a filter, the
    /// texts of the values of the conditions, and the columns a filter uses.
    /// </summary>
    internal static class FilterConditions
    {

        #region Fields

        /// <summary>
        /// The text of an enum condition whose chosen values are all the values of the enum.
        /// </summary>
        public const string AnyEnumValueText = "Any value";

        /// <summary>
        /// The text of an enum condition without any chosen value, which keeps no row.
        /// </summary>
        public const string NoEnumValueText = "No value";

        /// <summary>
        /// The separator of the chosen values in the text of an enum condition.
        /// </summary>
        private const string EnumValueSeparator = ", ";

        #endregion


        #region Public API

        /// <summary>
        /// Creates the default condition of a column, from the type of its values.
        /// </summary>
        /// <remarks>
        /// The default conditions keep all the rows until the user sets them, except the conditions of references, which have no
        /// neutral state:
        /// <list type="bullet">
        /// <item>Checkboxes: a <see cref="BoolCondition"/> that keeps the checked rows.</item>
        /// <item>Numbers: a <see cref="NumberRangeCondition"/> without limits.</item>
        /// <item>Enums: an <see cref="EnumCondition"/> with all the values of the enum.</item>
        /// <item>References: a <see cref="ReferenceCondition"/> that keeps the empty references (<i>None</i>), until the user picks an
        /// asset.</item>
        /// <item>The other types: a <see cref="TextCondition"/> with an empty text.</item>
        /// </list>
        /// </remarks>
        /// <param name="columnId">The identifier of the column.</param>
        /// <param name="valueType">The type of the values of the column, or null if it's unknown.</param>
        /// <returns>Returns the created condition, which can filter the column if its type is known.</returns>
        public static FilterCondition CreateDefault(string columnId, Type valueType)
        {
            if (valueType == typeof(bool))
                return new BoolCondition(columnId, true);

            if (NumberRangeCondition.IsNumberType(valueType))
                return new NumberRangeCondition(columnId, null, null);

            if (valueType != null && valueType.IsEnum)
            {
                EnumCondition enumCondition = new EnumCondition { ColumnId = columnId };
                enumCondition.Values.AddRange(GetEnumNumbers(valueType));
                return enumCondition;
            }

            if (valueType != null && typeof(Object).IsAssignableFrom(valueType))
                return new ReferenceCondition(columnId, ReferenceCondition.ReferenceMatch.IsAsset);

            return new TextCondition(columnId, string.Empty);
        }

        /// <summary>
        /// Parses a limit of a number range, typed in the invariant culture (a dot before the decimals), like the cells display
        /// numbers.
        /// </summary>
        /// <param name="text">The typed text.</param>
        /// <param name="limit">Outputs the limit, or null if the text is empty (no limit).</param>
        /// <returns>Returns true if the text is empty or a number, false if it can't be parsed.</returns>
        public static bool TryParseLimit(string text, out double? limit)
        {
            limit = null;
            if (string.IsNullOrWhiteSpace(text))
                return true;

            if (!double.TryParse(text.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double number) || double.IsNaN(number))
                return false;

            limit = number;
            return true;
        }

        /// <summary>
        /// Gets the text of a limit of a number range, as <see cref="TryParseLimit"/> reads it.
        /// </summary>
        /// <param name="limit">The limit, or null if there is none.</param>
        /// <returns>Returns the text of the limit, or an empty text if there is none.</returns>
        public static string ToLimitText(double? limit)
        {
            return limit.HasValue ? limit.Value.ToString("R", CultureInfo.InvariantCulture) : string.Empty;
        }

        /// <summary>
        /// Gets the values of an enum, as the numbers stored by <see cref="EnumCondition"/>, in the order of the declaration. Members
        /// with the same value are listed once.
        /// </summary>
        /// <param name="enumType">The type of the enum.</param>
        /// <returns>Returns the numbers of the values.</returns>
        public static List<long> GetEnumNumbers(Type enumType)
        {
            List<long> numbers = new List<long>();
            foreach (object value in Enum.GetValues(enumType))
            {
                long number = EnumCondition.ToNumber(value);
                if (!numbers.Contains(number))
                    numbers.Add(number);
            }

            return numbers;
        }

        /// <summary>
        /// Gets the name of an enum value as the editor displays it (<c>VeryRare</c> is displayed <c>Very Rare</c>).
        /// </summary>
        /// <param name="enumType">The type of the enum.</param>
        /// <param name="number">The number of the value (see <see cref="EnumCondition.ToNumber(object)"/>).</param>
        /// <returns>Returns the displayed name of the value, or its number if the enum has no member with that value.</returns>
        public static string GetEnumValueText(Type enumType, long number)
        {
            foreach (object value in Enum.GetValues(enumType))
            {
                if (EnumCondition.ToNumber(value) == number)
                    return ObjectNames.NicifyVariableName(Enum.GetName(enumType, value));
            }

            return number.ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Gets the text of the chosen values of an enum condition: <see cref="AnyEnumValueText"/> if all the values of the enum are
        /// chosen, <see cref="NoEnumValueText"/> if none is, or the names of the chosen values in the order of the declaration.
        /// </summary>
        /// <param name="enumType">The type of the enum.</param>
        /// <param name="chosenValues">The chosen values, as the numbers stored by <see cref="EnumCondition"/>.</param>
        /// <returns>Returns the text of the chosen values.</returns>
        public static string GetEnumValuesText(Type enumType, ICollection<long> chosenValues)
        {
            List<long> numbers = GetEnumNumbers(enumType);
            List<string> names = new List<string>(numbers.Count);
            foreach (long number in numbers)
            {
                if (chosenValues.Contains(number))
                    names.Add(GetEnumValueText(enumType, number));
            }

            if (names.Count == 0)
                return NoEnumValueText;

            return names.Count == numbers.Count ? AnyEnumValueText : string.Join(EnumValueSeparator, names);
        }

        /// <summary>
        /// Gets the columns a filter uses: the columns of its conditions that apply to the table.
        /// </summary>
        /// <param name="root">The root of the filter tree, or null.</param>
        /// <param name="columns">The columns of the table that can be filtered, mapped by their id.</param>
        /// <returns>Returns the identifiers of the filtered columns.</returns>
        public static HashSet<string> GetFilteredColumnIds(FilterNode root, IReadOnlyDictionary<string, FilterColumn> columns)
        {
            HashSet<string> columnIds = new HashSet<string>();
            CollectFilteredColumnIds(root, columns, columnIds);
            return columnIds;
        }

        #endregion


        #region Private API

        /// <summary>
        /// Adds the columns of the conditions of a node that apply to the table to a set, through the nodes of its groups.
        /// </summary>
        /// <param name="node">The node of which to collect the columns, or null.</param>
        /// <param name="columns">The columns of the table that can be filtered, mapped by their id.</param>
        /// <param name="columnIds">The set to which the identifiers of the filtered columns are added.</param>
        private static void CollectFilteredColumnIds(FilterNode node, IReadOnlyDictionary<string, FilterColumn> columns,
            HashSet<string> columnIds)
        {
            if (node is FilterGroup group)
            {
                foreach (FilterNode child in group.Nodes)
                    CollectFilteredColumnIds(child, columns, columnIds);
            }
            else if (node is FilterCondition condition && condition.ColumnId != null
                && columns.TryGetValue(condition.ColumnId, out FilterColumn column)
                && column.ValueType != null && condition.CanApplyTo(column.ValueType))
            {
                columnIds.Add(condition.ColumnId);
            }
        }

        #endregion

    }

}
