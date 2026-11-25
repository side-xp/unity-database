using System;
using System.Collections.Generic;

using UnityEngine;

namespace SideXP.Database.EditorOnly
{

    /// <summary>
    /// The display settings of a table, remembered for the user: column order, widths, hidden columns, the sort on one or several
    /// columns, the filters and the folder scope.
    /// </summary>
    /// <remarks>
    /// The tool stores one per table, as the user's default view of the table (see <see cref="UserViews"/>), so it becomes a regular
    /// view when views are implemented.
    /// <para>
    /// Columns are identified by their id. The settings of columns that don't exist anymore are kept but ignored, so they apply again
    /// if the column comes back. The columns that have no settings yet are added at their default position.
    /// </para>
    /// <para>
    /// The sort is a list of columns, by priority. It's replaced as a whole when the user changes it.
    /// </para>
    /// </remarks>
    [Serializable]
    internal class ViewSettings : ISerializationCallbackReceiver
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
        /// The columns used to sort the rows, by priority, or an empty list to keep the order of the source. May contain columns that
        /// don't exist anymore.
        /// </summary>
        [SerializeField]
        private List<ColumnSort> _sort = new List<ColumnSort>();

        /// <summary>
        /// The identifier of the column of the single sort remembered before the sort on several columns, or an empty string. Only read,
        /// to move it to <see cref="_sort"/> (see <see cref="OnAfterDeserialize"/>).
        /// </summary>
        [SerializeField]
        private string _sortColumnId = string.Empty;

        /// <summary>
        /// The direction of the sort of <see cref="_sortColumnId"/>. Only read, like the column.
        /// </summary>
        [SerializeField]
        private bool _isSortDescending = false;

        /// <summary>
        /// The root of the filter tree, or null if the rows are not filtered. May contain conditions on columns that don't exist
        /// anymore, which are ignored (see <see cref="FilterNode.Compile"/>).
        /// </summary>
        [SerializeReference]
        private FilterNode _filter = null;

        /// <summary>
        /// The folders of the folder scope (see <see cref="FolderScope"/>), or an empty list for the whole project.
        /// </summary>
        [SerializeField]
        private List<string> _folders = new List<string>();

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
        /// Checks if the rows are sorted by at least one column.
        /// </summary>
        public bool HasSort => _sort.Exists(sort => !string.IsNullOrEmpty(sort.ColumnId));

        /// <summary>
        /// Gets the display order of the columns of the table, hidden columns included.
        /// </summary>
        /// <remarks>
        /// The columns that have no settings yet get settings, right after the column that precedes them in the default order (or
        /// first if they are first), so a field added to a type appears next to its neighbors.
        /// </remarks>
        /// <param name="columnIds">The identifiers of the columns of the table, in their default order.</param>
        /// <param name="isHiddenByDefault">Checks if a column that has no settings yet is hidden, or null if all the columns are visible
        /// by default.</param>
        /// <returns>Returns the identifiers of the columns of the table, in their display order. Columns that are not in the given list are
        /// not included.</returns>
        public List<string> GetColumnOrder(IReadOnlyList<string> columnIds, Predicate<string> isHiddenByDefault = null)
        {
            for (int i = 0; i < columnIds.Count; i++)
            {
                if (IndexOf(columnIds[i]) >= 0)
                    continue;

                // The previous column has settings at this point, since the columns are processed in order
                int insertIndex = i > 0 ? IndexOf(columnIds[i - 1]) + 1 : 0;
                bool isHidden = isHiddenByDefault != null && isHiddenByDefault(columnIds[i]);
                _columns.Insert(insertIndex, new ColumnSettings(columnIds[i], isHidden));
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
            ColumnSettings column = GetOrAddColumn(columnId);
            if (Mathf.Approximately(column.Width, width))
                return false;

            column.Width = width;
            return true;
        }

        /// <summary>
        /// Checks if a column is hidden.
        /// </summary>
        /// <param name="columnId">The identifier of the column.</param>
        /// <returns>Returns true if the column is hidden, or false if it's visible or has no settings.</returns>
        public bool IsHidden(string columnId)
        {
            int index = IndexOf(columnId);
            return index >= 0 && _columns[index].IsHidden;
        }

        /// <summary>
        /// Hides or shows a column.
        /// </summary>
        /// <param name="columnId">The identifier of the column. If it has no settings yet, they are added after the other columns.</param>
        /// <param name="isHidden">If enabled, the column is hidden.</param>
        /// <returns>Returns true if the visibility of the column has changed.</returns>
        public bool SetHidden(string columnId, bool isHidden)
        {
            ColumnSettings column = GetOrAddColumn(columnId);
            if (column.IsHidden == isHidden)
                return false;

            column.IsHidden = isHidden;
            return true;
        }

        /// <summary>
        /// Removes the settings of the columns and the sort, so the columns get their default order, widths and visibility, and the
        /// rows the order of the source.
        /// </summary>
        /// <remarks>
        /// The filters and the folder scope are kept: they are cleared on their own (see <see cref="Filter"/> and
        /// <see cref="SetFolders"/>), so resetting the columns doesn't lose a filter built by hand.
        /// </remarks>
        /// <returns>Returns true if there were settings to remove.</returns>
        public bool Reset()
        {
            bool hasChanged = _columns.Count > 0 || _sort.Count > 0;
            _columns.Clear();
            ClearSort();
            return hasChanged;
        }

        /// <summary>
        /// Gets the columns used to sort the rows, by priority.
        /// </summary>
        /// <param name="canSort">Checks if a column can be sorted, for example if it exists and is displayed, or null to get all the
        /// remembered columns. The columns that can't be sorted are skipped, and the next ones take their priority.</param>
        /// <returns>Returns the sorted columns, by priority. Entries without column and the duplicates of a column (from a file edited
        /// by hand) are skipped.</returns>
        public List<ColumnSort> GetSort(Predicate<string> canSort = null)
        {
            HashSet<string> columnIds = new HashSet<string>();
            List<ColumnSort> sort = new List<ColumnSort>(_sort.Count);
            foreach (ColumnSort columnSort in _sort)
            {
                if (string.IsNullOrEmpty(columnSort.ColumnId) || !columnIds.Add(columnSort.ColumnId))
                    continue;

                if (canSort == null || canSort(columnSort.ColumnId))
                    sort.Add(columnSort);
            }

            return sort;
        }

        /// <summary>
        /// Sorts the rows by several columns, replacing the current sort.
        /// </summary>
        /// <param name="sort">The columns used to sort the rows, by priority. If empty, the sort is removed.</param>
        /// <returns>Returns true if the sort has changed.</returns>
        public bool SetSort(IReadOnlyList<ColumnSort> sort)
        {
            bool isSame = sort.Count == _sort.Count;
            for (int i = 0; isSame && i < sort.Count; i++)
                isSame = sort[i].Equals(_sort[i]);

            if (isSame)
                return false;

            _sort = new List<ColumnSort>(sort);
            return true;
        }

        /// <summary>
        /// Sorts the rows by a single column, replacing the current sort.
        /// </summary>
        /// <param name="columnId">The identifier of the column used to sort the rows. If null or empty, the sort is removed.</param>
        /// <param name="isDescending">If enabled, the rows are sorted in descending order.</param>
        /// <returns>Returns true if the sort has changed.</returns>
        public bool SetSort(string columnId, bool isDescending)
        {
            return string.IsNullOrEmpty(columnId)
                ? ClearSort()
                : SetSort(new[] { new ColumnSort(columnId, isDescending) });
        }

        /// <summary>
        /// Removes the sort, so the rows are in the order of the source.
        /// </summary>
        /// <returns>Returns true if the rows were sorted.</returns>
        public bool ClearSort()
        {
            if (_sort.Count == 0)
                return false;

            _sort.Clear();
            return true;
        }

        /// <summary>
        /// The root of the filter tree, or null if the rows are not filtered.
        /// </summary>
        /// <remarks>
        /// The tree is stored by reference: a change of its nodes is a change of the settings, so the owner must save the settings
        /// after editing the tree.
        /// </remarks>
        public FilterNode Filter
        {
            get => _filter;
            set => _filter = value;
        }

        /// <inheritdoc cref="_folders"/>
        public IReadOnlyList<string> Folders => _folders;

        /// <summary>
        /// Sets the folders of the folder scope.
        /// </summary>
        /// <param name="folders">The paths of the folders, relative to the project, or null or empty for the whole project. Stored as
        /// given: normalize them first (see <see cref="FolderScope.Folders"/>).</param>
        /// <returns>Returns true if the folders have changed.</returns>
        public bool SetFolders(IEnumerable<string> folders)
        {
            List<string> newFolders = folders != null ? new List<string>(folders) : new List<string>();
            bool isSame = newFolders.Count == _folders.Count;
            for (int i = 0; isSame && i < newFolders.Count; i++)
                isSame = newFolders[i] == _folders[i];

            if (isSame)
                return false;

            _folders = newFolders;
            return true;
        }

        #endregion


        #region Serialization

        /// <summary>
        /// Called before the settings are saved. Nothing to prepare: the single sort of <see cref="_sortColumnId"/> is moved to the list
        /// when the settings are loaded.
        /// </summary>
        public void OnBeforeSerialize()
        {
        }

        /// <summary>
        /// Called after the settings are loaded. Reads the single sort remembered before the sort on several columns as a list of one
        /// column, if the settings have no list yet.
        /// </summary>
        public void OnAfterDeserialize()
        {
            if (string.IsNullOrEmpty(_sortColumnId))
                return;

            if (_sort.Count == 0)
                _sort.Add(new ColumnSort(_sortColumnId, _isSortDescending));

            _sortColumnId = string.Empty;
            _isSortDescending = false;
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

        /// <summary>
        /// Gets the settings of a column, and adds them after the other columns if it has none yet, visible.
        /// </summary>
        /// <param name="columnId">The identifier of the column.</param>
        /// <returns>Returns the settings of the column.</returns>
        private ColumnSettings GetOrAddColumn(string columnId)
        {
            int index = IndexOf(columnId);
            if (index >= 0)
                return _columns[index];

            ColumnSettings column = new ColumnSettings(columnId, false);
            _columns.Add(column);
            return column;
        }

        #endregion

    }

}
