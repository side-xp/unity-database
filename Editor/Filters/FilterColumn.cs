using System;

namespace SideXP.Database.EditorOnly
{

    /// <summary>
    /// What a filter knows about a column of the table when it's compiled (see <see cref="FilterNode.Compile"/>): where its values are
    /// in the rows, their type, and their text conversion.
    /// </summary>
    internal readonly struct FilterColumn
    {

        #region Fields

        /// <summary>
        /// The index of the column in the pipeline, where its cached values are in the rows (see <see cref="TableRow.GetValue"/>).
        /// </summary>
        public readonly int Index;

        /// <summary>
        /// The type of the values of the column, which decides the conditions that apply to it.
        /// </summary>
        public readonly Type ValueType;

        /// <summary>
        /// Converts a value of the column to text, like the search does (the text conversion of the handler of the column). Returns
        /// null if the conversion fails.
        /// </summary>
        public readonly Func<object, string> ToText;

        /// <summary>
        /// Checks if the values of the column are not known yet when the filter runs (the values of a computed column, in the first
        /// filter pass of the pipeline). The conditions that apply to the column then count as met (see
        /// <see cref="FilterNode.Deferred"/>), so the filter keeps every row that may meet it once the values are known.
        /// </summary>
        public readonly bool IsDeferred;

        #endregion


        #region Lifecycle

        /// <inheritdoc cref="FilterColumn"/>
        /// <param name="index"><inheritdoc cref="Index" path="/summary"/></param>
        /// <param name="valueType"><inheritdoc cref="ValueType" path="/summary"/></param>
        /// <param name="toText"><inheritdoc cref="ToText" path="/summary"/></param>
        /// <param name="isDeferred"><inheritdoc cref="IsDeferred" path="/summary"/></param>
        public FilterColumn(int index, Type valueType, Func<object, string> toText, bool isDeferred = false)
        {
            Index = index;
            ValueType = valueType;
            ToText = toText;
            IsDeferred = isDeferred;
        }

        #endregion

    }

}
