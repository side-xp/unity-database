using System;
using System.Collections.Generic;

using UnityEngine;

namespace SideXP.Database.EditorOnly
{

    /// <summary>
    /// The display settings of a table, remembered for the user: column order, widths and sort.
    /// </summary>
    /// <remarks>
    /// The tool stores one per table, as the user's default view of the table (see <see cref="UserViews"/>), so it becomes a regular
    /// view when views are implemented.
    /// <para>
    /// Columns are identified by their id. The settings of columns that don't exist anymore are kept but ignored, so they apply again
    /// if the column comes back. The columns that have no settings yet are added at their default position.
    /// </para>
    /// </remarks>
    [Serializable]
    internal class ViewSettings
    {

        #region Fields

        /// <summary>
        /// The stable identifier of the table of the view (see <see cref="TableDefinition.Id"/>).
        /// </summary>
        [SerializeField]
        private string _tableId = null;

        /// <summary>
        /// The settings of the columns, in their display order. May contain columns that don't exist anymore.
        /// </summary>
        [SerializeField]
        private List<ColumnSettings> _columns = new List<ColumnSettings>();

        /// <summary>
        /// The identifier of the column used to sort the rows, or an empty string to keep the order of the source.
        /// </summary>
        [SerializeField]
        private string _sortColumnId = string.Empty;

        /// <summary>
        /// Checks if the rows are sorted in descending order.
        /// </summary>
        [SerializeField]
        private bool _isSortDescending = false;

        #endregion


        #region Lifecycle

        /// <inheritdoc cref="ViewSettings"/>
        /// <param name="tableId"><inheritdoc cref="_tableId" path="/summary"/></param>
        public ViewSettings(string tableId)
        {
            _tableId = tableId;
        }

        #endregion


        #region Public API

        /// <inheritdoc cref="_tableId"/>
        public string TableId => _tableId;

        /// <summary>
        /// Checks if the rows are sorted by a column.
        /// </summary>
        public bool HasSort => !string.IsNullOrEmpty(_sortColumnId);

        /// <summary>
        /// The identifier of the column used to sort the rows, or null if the rows are not sorted.
        /// </summary>
        public string SortColumnId => HasSort ? _sortColumnId : null;

        /// <inheritdoc cref="_isSortDescending"/>
        public bool IsSortDescending => HasSort && _isSortDescending;

        /// <summary>
        /// Gets the display order of the columns of the table.
        /// </summary>
        /// <remarks>
        /// The columns that have no settings yet get settings, right after the column that precedes them in the default order (or
        /// first if they are first), so a field added to a type appears next to its neighbors.
        /// </remarks>
        /// <param name="columnIds">The identifiers of the columns of the table, in their default order.</param>
        /// <returns>Returns the identifiers of the columns of the table, in their display order. Columns that are not in the given list are
        /// not included.</returns>
        public List<string> GetColumnOrder(IReadOnlyList<string> columnIds)
        {
            for (int i = 0; i < columnIds.Count; i++)
            {
                if (IndexOf(columnIds[i]) >= 0)
                    continue;

                // The previous column has settings at this point, since the columns are processed in order
                int insertIndex = i > 0 ? IndexOf(columnIds[i - 1]) + 1 : 0;
                _columns.Insert(insertIndex, new ColumnSettings(columnIds[i]));
            }

            HashSet<string> remainingIds = new HashSet<string>(columnIds);
            List<string> order = new List<string>(columnIds.Count);
            foreach (ColumnSettings column in _columns)
            {
                // Removing the id also skips the duplicates of a hand-edited file
                if (column != null && remainingIds.Remove(column.Id))
                    order.Add(column.Id);
            }

            return order;
        }

        /// <summary>
        /// Changes the order of some columns: they take the places of the same columns in the current order, so the other columns (for
        /// example the frozen column, or columns that don't exist anymore) keep theirs.
        /// </summary>
        /// <example>
        /// With the order <c>A, B, C, D</c>, reordering <c>D, B</c> gives <c>A, D, C, B</c>.
        /// </example>
        /// <param name="columnIds">The identifiers of the reordered columns, in their new order. Columns without settings are
        /// ignored.</param>
        /// <returns>Returns true if the order has changed.</returns>
        public bool SetColumnOrder(IReadOnlyList<string> columnIds)
        {
            List<int> places = new List<int>(columnIds.Count);
            List<ColumnSettings> reorderedColumns = new List<ColumnSettings>(columnIds.Count);
            foreach (string columnId in columnIds)
            {
                int index = IndexOf(columnId);
                if (index < 0 || places.Contains(index))
                    continue;

                places.Add(index);
                reorderedColumns.Add(_columns[index]);
            }

            // The places are filled in the order of the list, so they must be sorted
            places.Sort();
            bool hasChanged = false;
            for (int i = 0; i < places.Count; i++)
            {
                if (_columns[places[i]] == reorderedColumns[i])
                    continue;

                _columns[places[i]] = reorderedColumns[i];
                hasChanged = true;
            }

            return hasChanged;
        }

        /// <summary>
        /// Gets the width of a column chosen by the user.
        /// </summary>
        /// <param name="columnId">The identifier of the column.</param>
        /// <returns>Returns the width of the column in pixels, or 0 if it has its default width.</returns>
        public float GetWidth(string columnId)
        {
            int index = IndexOf(columnId);
            return index >= 0 ? Mathf.Max(0, _columns[index].Width) : 0;
        }

        /// <summary>
        /// Sets the width of a column chosen by the user.
        /// </summary>
        /// <param name="columnId">The identifier of the column. If it has no settings yet, they are added after the other columns.</param>
        /// <param name="width">The width of the column, in pixels. If 0 or negative, the column gets its default width.</param>
        /// <returns>Returns true if the width has changed.</returns>
        public bool SetWidth(string columnId, float width)
        {
            width = Mathf.Max(0, width);
            int index = IndexOf(columnId);
            if (index < 0)
            {
                _columns.Add(new ColumnSettings(columnId));
                index = _columns.Count - 1;
            }

            if (Mathf.Approximately(_columns[index].Width, width))
                return false;

            _columns[index].Width = width;
            return true;
        }

        /// <summary>
        /// Sorts the rows by a column.
        /// </summary>
        /// <param name="columnId">The identifier of the column used to sort the rows. If null or empty, the sort is removed.</param>
        /// <param name="isDescending"><inheritdoc cref="_isSortDescending" path="/summary"/></param>
        /// <returns>Returns true if the sort has changed.</returns>
        public bool SetSort(string columnId, bool isDescending)
        {
            if (string.IsNullOrEmpty(columnId))
                return ClearSort();

            if (_sortColumnId == columnId && _isSortDescending == isDescending)
                return false;

            _sortColumnId = columnId;
            _isSortDescending = isDescending;
            return true;
        }

        /// <summary>
        /// Removes the sort, so the rows are in the order of the source.
        /// </summary>
        /// <returns>Returns true if the rows were sorted.</returns>
        public bool ClearSort()
        {
            if (!HasSort)
                return false;

            _sortColumnId = string.Empty;
            _isSortDescending = false;
            return true;
        }

        #endregion


        #region Private API

        /// <summary>
        /// Gets the index of the settings of a column.
        /// </summary>
        /// <param name="columnId">The identifier of the column.</param>
        /// <returns>Returns the index of the first settings of the column, or -1 if it has none.</returns>
        private int IndexOf(string columnId)
        {
            return _columns.FindIndex(column => column != null && column.Id == columnId);
        }

        #endregion

    }

}
