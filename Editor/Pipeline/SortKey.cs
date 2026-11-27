using System;

namespace SideXP.Database.EditorOnly
{

    /// <summary>
    /// A column the rows of a <see cref="TablePipeline"/> are sorted by, with its direction and the comparison of its values.
    /// </summary>
    internal readonly struct SortKey
    {

        #region Fields

        /// <summary>
        /// The index of the sorted column in the pipeline.
        /// </summary>
        public readonly int ColumnIndex;

        /// <summary>
        /// Checks if the rows are sorted in descending order of this column.
        /// </summary>
        public readonly bool IsDescending;

        /// <summary>
        /// Compares two values of the column.
        /// </summary>
        public readonly Comparison<object> Comparison;

        #endregion


        #region Lifecycle

        /// <inheritdoc cref="SortKey"/>
        /// <param name="columnIndex"><inheritdoc cref="ColumnIndex" path="/summary"/></param>
        /// <param name="isDescending"><inheritdoc cref="IsDescending" path="/summary"/></param>
        /// <param name="comparison"><inheritdoc cref="Comparison" path="/summary"/></param>
        public SortKey(int columnIndex, bool isDescending, Comparison<object> comparison)
        {
            ColumnIndex = columnIndex;
            IsDescending = isDescending;
            Comparison = comparison;
        }

        #endregion

    }

}
