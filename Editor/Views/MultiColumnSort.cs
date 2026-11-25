using System.Collections.Generic;

namespace SideXP.Database.EditorOnly
{

    /// <summary>
    /// The rules of the sort on several columns: how the clicks on the headers and the items of their menu change the sorted columns.
    /// </summary>
    /// <remarks>
    /// A sort is a list of columns by priority (see <see cref="ColumnSort"/>). The methods never change the given list: they return a
    /// new one.
    /// </remarks>
    internal static class MultiColumnSort
    {

        #region Public API

        /// <summary>
        /// Gets the priority of a column in a sort.
        /// </summary>
        /// <param name="sort">The sorted columns, by priority.</param>
        /// <param name="columnId">The identifier of the column.</param>
        /// <returns>Returns the index of the column in the sort, or -1 if the column is not sorted.</returns>
        public static int IndexOf(IReadOnlyList<ColumnSort> sort, string columnId)
        {
            for (int i = 0; i < sort.Count; i++)
            {
                if (sort[i].ColumnId == columnId)
                    return i;
            }

            return -1;
        }

        /// <summary>
        /// Sorts by a single column, as a click on its header does: in the other direction if the column is already sorted, in
        /// ascending order otherwise. The other columns leave the sort.
        /// </summary>
        /// <param name="sort">The current sort.</param>
        /// <param name="columnId">The identifier of the clicked column.</param>
        /// <returns>Returns the new sort.</returns>
        public static List<ColumnSort> SortBy(IReadOnlyList<ColumnSort> sort, string columnId)
        {
            int index = IndexOf(sort, columnId);
            ColumnSort columnSort = index >= 0 ? sort[index].Reversed() : new ColumnSort(columnId, false);
            return new List<ColumnSort> { columnSort };
        }

        /// <summary>
        /// Changes the sort of a column and keeps the other columns, as an Alt-click on its header does: a column that is not sorted
        /// is added last in ascending order, an ascending column becomes descending, and a descending column leaves the sort.
        /// </summary>
        /// <example>
        /// With the sort <c>Tier (ascending)</c>, three Alt-clicks on <i>Price</i> give <c>Tier, Price (ascending)</c>, then
        /// <c>Tier, Price (descending)</c>, then <c>Tier</c> again.
        /// </example>
        /// <param name="sort">The current sort.</param>
        /// <param name="columnId">The identifier of the clicked column.</param>
        /// <returns>Returns the new sort.</returns>
        public static List<ColumnSort> Cycle(IReadOnlyList<ColumnSort> sort, string columnId)
        {
            List<ColumnSort> newSort = new List<ColumnSort>(sort);
            int index = IndexOf(sort, columnId);
            if (index < 0)
                newSort.Add(new ColumnSort(columnId, false));
            else if (!sort[index].IsDescending)
                newSort[index] = sort[index].Reversed();
            else
                newSort.RemoveAt(index);

            return newSort;
        }

        /// <summary>
        /// Adds a column last to the sort, in ascending order. A column already sorted keeps its place and direction.
        /// </summary>
        /// <param name="sort">The current sort.</param>
        /// <param name="columnId">The identifier of the column to add.</param>
        /// <returns>Returns the new sort.</returns>
        public static List<ColumnSort> Add(IReadOnlyList<ColumnSort> sort, string columnId)
        {
            List<ColumnSort> newSort = new List<ColumnSort>(sort);
            if (IndexOf(sort, columnId) < 0)
                newSort.Add(new ColumnSort(columnId, false));

            return newSort;
        }

        /// <summary>
        /// Removes a column from the sort. The next columns take its priority.
        /// </summary>
        /// <param name="sort">The current sort.</param>
        /// <param name="columnId">The identifier of the column to remove.</param>
        /// <returns>Returns the new sort.</returns>
        public static List<ColumnSort> Remove(IReadOnlyList<ColumnSort> sort, string columnId)
        {
            List<ColumnSort> newSort = new List<ColumnSort>(sort);
            int index = IndexOf(sort, columnId);
            if (index >= 0)
                newSort.RemoveAt(index);

            return newSort;
        }

        #endregion

    }

}
