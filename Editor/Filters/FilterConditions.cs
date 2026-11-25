using System;

namespace SideXP.Database.EditorOnly
{

    /// <summary>
    /// Creates the conditions of the filter editor: the condition a column gets when it's added to a filter, from the type of its
    /// values.
    /// </summary>
    internal static class FilterConditions
    {

        #region Public API

        /// <summary>
        /// Creates the default condition of a column, from the type of its values.
        /// </summary>
        /// <remarks>
        /// Checkboxes get a <see cref="BoolCondition"/> that keeps the checked rows. The other columns get a
        /// <see cref="TextCondition"/> with an empty text, which keeps all the rows until the user types.
        /// <para>
        /// @todo 21b2b: the conditions of numbers, enums and references.
        /// </para>
        /// </remarks>
        /// <param name="columnId">The identifier of the column.</param>
        /// <param name="valueType">The type of the values of the column, or null if it's unknown.</param>
        /// <returns>Returns the created condition, which can always filter the column.</returns>
        public static FilterCondition CreateDefault(string columnId, Type valueType)
        {
            if (valueType == typeof(bool))
                return new BoolCondition(columnId, true);

            return new TextCondition(columnId, string.Empty);
        }

        #endregion

    }

}
