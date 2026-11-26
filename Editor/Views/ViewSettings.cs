using System;
using System.Collections.Generic;

using UnityEngine;

namespace SideXP.Database.EditorOnly
{

    /// <summary>
    /// A layer of the display settings of a table: column order, widths, hidden columns, the sort on one or several columns, the
    /// filters and the folder scope. Each setting can be unset, so it comes from the layer below (see
    /// <see cref="LayeredViewSettings"/>).
    /// </summary>
    /// <remarks>
    /// The granularity of the settings: the column order as a whole, the width and the visibility of each column, the sort as a whole,
    /// the filter tree as a whole and the folders as a whole. A set sort, filter or folder scope can be empty (no sort, no filter, the
    /// whole project), which is not the same as an unset one, so a layer can remove the sort of the layer below.
    /// <para>
    /// Columns are identified by their id. The settings of columns that don't exist anymore are kept but ignored, so they apply again
    /// if the column comes back.
    /// </para>
    /// <para>
    /// The settings saved before the views (see <see cref="OnAfterDeserialize"/>) had every setting set, and are read as such.
    /// </para>
    /// </remarks>
    [Serializable]
    internal class ViewSettings : ISerializationCallbackReceiver
    {

        #region Fields

        /// <summary>
        /// The version of the format of the settings. The settings saved before the views have none (0).
        /// </summary>
        private const int CurrentVersion = 1;

        /// <summary>
        /// The stable identifier of the table of the view (see <see cref="TableDefinition.Id"/>).
        /// </summary>
        [SerializeField]
        private string _tableId = null;

        /// <summary>
        /// The version of the format of these settings, written when they are saved (see <see cref="OnBeforeSerialize"/>).
        /// </summary>
        /// <remarks>
        /// The settings saved before the views have no version: the field keeps its default value when they are loaded.
        /// </remarks>
        [SerializeField]
        private int _version = 0;

        /// <summary>
        /// The identifiers of the columns in their display order, hidden columns included, or an empty list if the layer doesn't set
        /// the order. May contain columns that don't exist anymore.
        /// </summary>
        [SerializeField]
        private List<string> _columnOrder = new List<string>();

        /// <summary>
        /// The width and the visibility of the columns that have one of them set, in no particular order. May contain columns that
        /// don't exist anymore.
        /// </summary>
        /// <remarks>
        /// In the settings saved before the views, this list is also the display order of the columns (see
        /// <see cref="OnAfterDeserialize"/>).
        /// </remarks>
        [SerializeField]
        private List<ColumnSettings> _columns = new List<ColumnSettings>();

        /// <summary>
        /// Checks if the layer sets the sort, even to no sort.
        /// </summary>
        [SerializeField]
        private bool _isSortSet = false;

        /// <summary>
        /// The columns used to sort the rows, by priority, or an empty list to keep the order of the source. Ignored if
        /// <see cref="_isSortSet"/> is disabled. May contain columns that don't exist anymore.
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
        /// Checks if the layer sets the filters, even to no filter.
        /// </summary>
        [SerializeField]
        private bool _isFilterSet = false;

        /// <summary>
        /// The root of the filter tree, or null if the rows are not filtered. Ignored if <see cref="_isFilterSet"/> is disabled. May
        /// contain conditions on columns that don't exist anymore, which are ignored (see <see cref="FilterNode.Compile"/>).
        /// </summary>
        [SerializeReference]
        private FilterNode _filter = null;

        /// <summary>
        /// Checks if the layer sets the folder scope, even to the whole project.
        /// </summary>
        [SerializeField]
        private bool _areFoldersSet = false;

        /// <summary>
        /// The folders of the folder scope (see <see cref="FolderScope"/>), or an empty list for the whole project. Ignored if
        /// <see cref="_areFoldersSet"/> is disabled.
        /// </summary>
        [SerializeField]
        private List<string> _folders = new List<string>();

        /// <summary>
        /// The number of rows per page, or 0 if the user hasn't chosen one yet: the table view then uses its default size.
        /// </summary>
        /// <remarks>
        /// @todo After the views: the page size becomes a user setting of the whole package, chosen by the user. It's not a layered
        /// setting of the views.
        /// </remarks>
        [SerializeField]
        private int _pageSize = 0;

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
        /// Checks if the layer sets no setting of the view: no column order, width or visibility, no sort, no filters and no folder
        /// scope. The page size doesn't count, since it's not a setting of the views (see <see cref="PageSize"/>).
        /// </summary>
        public bool IsEmpty => !IsColumnOrderSet && GetColumnsWithSettings().Count == 0 && !_isSortSet && !_isFilterSet && !_areFoldersSet;

        /// <summary>
        /// Gets a copy of these settings, the filter tree included, so the copy can be edited without changing them.
        /// </summary>
        /// <returns>Returns the copied settings.</returns>
        public ViewSettings Clone()
        {
            ViewSettings copy = new ViewSettings(_tableId);
            copy._version = _version;
            copy._columnOrder = new List<string>(_columnOrder);
            copy._columns = _columns.FindAll(column => column != null).ConvertAll(column => column.Clone());
            copy._isSortSet = _isSortSet;
            copy._sort = new List<ColumnSort>(_sort);
            copy._isFilterSet = _isFilterSet;
            copy._filter = _filter?.Clone();
            copy._areFoldersSet = _areFoldersSet;
            copy._folders = new List<string>(_folders);
            copy._pageSize = _pageSize;
            return copy;
        }

        /// <summary>
        /// Unsets the settings of the columns and the sort, so the columns get the order, widths and visibility of the layer below,
        /// and the rows its sort.
        /// </summary>
        /// <remarks>
        /// The filters and the folder scope are kept: they are cleared on their own (see <see cref="SetFilter"/> and
        /// <see cref="SetFolders"/>), so resetting the columns doesn't lose a filter built by hand. The page size is kept too, since
        /// it's not a setting of the columns (see <see cref="SetPageSize"/>).
        /// </remarks>
        /// <returns>Returns true if there were settings to unset.</returns>
        public bool Reset()
        {
            bool hasChanged = _columnOrder.Count > 0 || _columns.Count > 0 || _isSortSet;
            _columnOrder.Clear();
            _columns.Clear();
            UnsetSort();
            return hasChanged;
        }

        #endregion


        #region Kinds of settings

        /// <summary>
        /// All the kinds of settings of a view, in the order they are listed.
        /// </summary>
        public static readonly ViewSettingKind[] AllKinds =
        {
            ViewSettingKind.ColumnOrder,
            ViewSettingKind.ColumnWidths,
            ViewSettingKind.ColumnVisibility,
            ViewSettingKind.Sort,
            ViewSettingKind.Filters,
            ViewSettingKind.Folders,
        };

        /// <summary>
        /// Checks if the layer sets a kind of setting.
        /// </summary>
        /// <param name="kind">The kind of setting to check.</param>
        /// <returns>Returns true if the layer sets it, for at least one column for the widths and the visibility.</returns>
        public bool IsSet(ViewSettingKind kind)
        {
            return GetSetCount(kind) > 0;
        }

        /// <summary>
        /// Gets how many times the layer sets a kind of setting.
        /// </summary>
        /// <param name="kind">The kind of setting to count.</param>
        /// <returns>Returns the number of columns whose width or visibility the layer sets, or 1 if the layer sets another kind of
        /// setting, or 0.</returns>
        public int GetSetCount(ViewSettingKind kind)
        {
            switch (kind)
            {
                case ViewSettingKind.ColumnOrder:
                    return IsColumnOrderSet ? 1 : 0;

                case ViewSettingKind.ColumnWidths:
                    return GetColumnsWithSettings().FindAll(columnId => GetWidth(columnId) > 0).Count;

                case ViewSettingKind.ColumnVisibility:
                    return GetColumnsWithSettings().FindAll(columnId => GetVisibility(columnId) != ColumnVisibility.Unset).Count;

                case ViewSettingKind.Sort:
                    return _isSortSet ? 1 : 0;

                case ViewSettingKind.Filters:
                    return _isFilterSet ? 1 : 0;

                case ViewSettingKind.Folders:
                    return _areFoldersSet ? 1 : 0;

                default:
                    return 0;
            }
        }

        /// <summary>
        /// Unsets a kind of setting, so it comes from the layer below: the widths and the visibility of all the columns at once.
        /// </summary>
        /// <param name="kind">The kind of setting to unset.</param>
        /// <returns>Returns true if the layer did set it.</returns>
        public bool Unset(ViewSettingKind kind)
        {
            switch (kind)
            {
                case ViewSettingKind.ColumnOrder:
                    return SetColumnOrder(null);

                case ViewSettingKind.ColumnWidths:
                {
                    bool hasChanged = false;
                    foreach (string columnId in GetColumnsWithSettings())
                        hasChanged |= SetWidth(columnId, 0);

                    return hasChanged;
                }

                case ViewSettingKind.ColumnVisibility:
                {
                    bool hasChanged = false;
                    foreach (string columnId in GetColumnsWithSettings())
                        hasChanged |= SetVisibility(columnId, ColumnVisibility.Unset);

                    return hasChanged;
                }

                case ViewSettingKind.Sort:
                    return UnsetSort();

                case ViewSettingKind.Filters:
                    return UnsetFilter();

                case ViewSettingKind.Folders:
                    return UnsetFolders();

                default:
                    return false;
            }
        }

        /// <summary>
        /// Unsets all the settings of the view, so they all come from the layer below. The page size is kept, since it's not a setting
        /// of the views (see <see cref="PageSize"/>).
        /// </summary>
        /// <returns>Returns true if the layer did set at least one setting.</returns>
        public bool UnsetAll()
        {
            bool hasChanged = false;
            foreach (ViewSettingKind kind in AllKinds)
                hasChanged |= Unset(kind);

            return hasChanged;
        }

        /// <summary>
        /// Writes the settings this layer sets to another layer, replacing them there. The settings this layer doesn't set are not
        /// changed in the other layer, nor the page size.
        /// </summary>
        /// <remarks>
        /// The widths and the visibility are written per column, so the other columns keep their settings in the other layer. The
        /// filter tree is copied, so the two layers never share it.
        /// </remarks>
        /// <param name="target">The layer to which the settings are written, for example the settings of a project view under the
        /// overrides of the user.</param>
        /// <returns>Returns true if the other layer has changed.</returns>
        /// <exception cref="ArgumentNullException">Thrown if the other layer is null.</exception>
        public bool ApplyTo(ViewSettings target)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));

            bool hasChanged = false;
            if (IsColumnOrderSet)
                hasChanged |= target.SetColumnOrder(_columnOrder);

            foreach (string columnId in GetColumnsWithSettings())
            {
                float width = GetWidth(columnId);
                if (width > 0)
                    hasChanged |= target.SetWidth(columnId, width);

                ColumnVisibility visibility = GetVisibility(columnId);
                if (visibility != ColumnVisibility.Unset)
                    hasChanged |= target.SetVisibility(columnId, visibility);
            }

            if (_isSortSet)
                hasChanged |= target.SetSort(_sort);

            if (_isFilterSet)
            {
                // The trees are not compared: a set filter is always a change
                target.SetFilter(_filter?.Clone());
                hasChanged = true;
            }

            if (_areFoldersSet)
                hasChanged |= target.SetFolders(_folders);

            return hasChanged;
        }

        #endregion


        #region Columns

        /// <summary>
        /// Checks if the layer sets the order of the columns.
        /// </summary>
        public bool IsColumnOrderSet => _columnOrder.Count > 0;

        /// <inheritdoc cref="_columnOrder"/>
        public IReadOnlyList<string> ColumnOrder => _columnOrder;

        /// <summary>
        /// Sets the order of the columns.
        /// </summary>
        /// <param name="columnIds">The identifiers of the columns in their display order, or null or empty to unset the order. Stored as
        /// given, so the columns that don't exist anymore keep their places.</param>
        /// <returns>Returns true if the order has changed.</returns>
        public bool SetColumnOrder(IEnumerable<string> columnIds)
        {
            List<string> order = columnIds != null ? new List<string>(columnIds) : new List<string>();
            if (AreSame(order, _columnOrder))
                return false;

            _columnOrder = order;
            return true;
        }

        /// <summary>
        /// Gets the columns whose width or visibility the layer sets.
        /// </summary>
        /// <returns>Returns the identifiers of the columns, columns that don't exist anymore included.</returns>
        public List<string> GetColumnsWithSettings()
        {
            List<string> columnIds = new List<string>(_columns.Count);
            foreach (ColumnSettings column in _columns)
            {
                if (column != null && !column.IsEmpty && !columnIds.Contains(column.Id))
                    columnIds.Add(column.Id);
            }

            return columnIds;
        }

        /// <summary>
        /// Gets the width of a column chosen by the user.
        /// </summary>
        /// <param name="columnId">The identifier of the column.</param>
        /// <returns>Returns the width of the column in pixels, or 0 if the layer doesn't set it.</returns>
        public float GetWidth(string columnId)
        {
            ColumnSettings column = FindColumn(columnId);
            return column != null ? Mathf.Max(0, column.Width) : 0;
        }

        /// <summary>
        /// Sets the width of a column chosen by the user.
        /// </summary>
        /// <param name="columnId">The identifier of the column.</param>
        /// <param name="width">The width of the column, in pixels. If 0 or negative, the width is unset.</param>
        /// <returns>Returns true if the width has changed.</returns>
        public bool SetWidth(string columnId, float width)
        {
            width = Mathf.Max(0, width);
            if (Mathf.Approximately(GetWidth(columnId), width))
                return false;

            GetOrAddColumn(columnId).Width = width;
            RemoveIfEmpty(columnId);
            return true;
        }

        /// <summary>
        /// Gets the visibility of a column.
        /// </summary>
        /// <param name="columnId">The identifier of the column.</param>
        /// <returns>Returns the visibility of the column, or <see cref="ColumnVisibility.Unset"/> if the layer doesn't set it.</returns>
        public ColumnVisibility GetVisibility(string columnId)
        {
            ColumnSettings column = FindColumn(columnId);
            return column != null ? column.Visibility : ColumnVisibility.Unset;
        }

        /// <summary>
        /// Sets the visibility of a column.
        /// </summary>
        /// <param name="columnId">The identifier of the column.</param>
        /// <param name="visibility">The visibility of the column, or <see cref="ColumnVisibility.Unset"/> to unset it.</param>
        /// <returns>Returns true if the visibility has changed.</returns>
        public bool SetVisibility(string columnId, ColumnVisibility visibility)
        {
            if (GetVisibility(columnId) == visibility)
                return false;

            GetOrAddColumn(columnId).Visibility = visibility;
            RemoveIfEmpty(columnId);
            return true;
        }

        #endregion


        #region Sort

        /// <inheritdoc cref="_isSortSet"/>
        public bool IsSortSet => _isSortSet;

        /// <summary>
        /// Gets the columns used to sort the rows, by priority.
        /// </summary>
        /// <param name="canSort">Checks if a column can be sorted, for example if it exists and is displayed, or null to get all the
        /// remembered columns. The columns that can't be sorted are skipped, and the next ones take their priority.</param>
        /// <returns>Returns the sorted columns, by priority, or an empty list if the layer doesn't set the sort. Entries without column
        /// and the duplicates of a column (from a file edited by hand) are skipped.</returns>
        public List<ColumnSort> GetSort(Predicate<string> canSort = null)
        {
            List<ColumnSort> sort = new List<ColumnSort>(_sort.Count);
            if (!_isSortSet)
                return sort;

            HashSet<string> columnIds = new HashSet<string>();
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
        /// Sets the sort, replacing the current one.
        /// </summary>
        /// <param name="sort">The columns used to sort the rows, by priority. If empty, the layer sets no sort: the rows keep the order
        /// of the source, whatever the layer below sorts.</param>
        /// <returns>Returns true if the sort has changed.</returns>
        /// <exception cref="ArgumentNullException">Thrown if the sort is null (see <see cref="UnsetSort"/>).</exception>
        public bool SetSort(IReadOnlyList<ColumnSort> sort)
        {
            if (sort == null)
                throw new ArgumentNullException(nameof(sort), "Unset the sort instead of setting a null sort.");

            if (_isSortSet && AreSame(sort, _sort))
                return false;

            _isSortSet = true;
            _sort = new List<ColumnSort>(sort);
            return true;
        }

        /// <summary>
        /// Unsets the sort, so it comes from the layer below.
        /// </summary>
        /// <returns>Returns true if the layer did set the sort.</returns>
        public bool UnsetSort()
        {
            bool wasSet = _isSortSet;
            _isSortSet = false;
            _sort.Clear();
            return wasSet;
        }

        #endregion


        #region Filters

        /// <inheritdoc cref="_isFilterSet"/>
        public bool IsFilterSet => _isFilterSet;

        /// <summary>
        /// The root of the filter tree, or null if the rows are not filtered or if the layer doesn't set the filters.
        /// </summary>
        /// <remarks>
        /// The tree is stored by reference: a change of its nodes is a change of the settings, so the owner must save the settings
        /// after editing the tree.
        /// </remarks>
        public FilterNode Filter => _isFilterSet ? _filter : null;

        /// <summary>
        /// Sets the filters, replacing the current ones.
        /// </summary>
        /// <param name="filter">The root of the filter tree, or null to set no filter: all the rows are shown, whatever the layer below
        /// filters.</param>
        public void SetFilter(FilterNode filter)
        {
            _isFilterSet = true;
            _filter = filter;
        }

        /// <summary>
        /// Unsets the filters, so they come from the layer below.
        /// </summary>
        /// <returns>Returns true if the layer did set the filters.</returns>
        public bool UnsetFilter()
        {
            bool wasSet = _isFilterSet;
            _isFilterSet = false;
            _filter = null;
            return wasSet;
        }

        #endregion


        #region Folders

        /// <inheritdoc cref="_areFoldersSet"/>
        public bool AreFoldersSet => _areFoldersSet;

        /// <summary>
        /// The folders of the folder scope (see <see cref="FolderScope"/>), or an empty list for the whole project or if the layer
        /// doesn't set the folders.
        /// </summary>
        public IReadOnlyList<string> Folders => _areFoldersSet ? _folders : Array.Empty<string>();

        /// <summary>
        /// Sets the folders of the folder scope.
        /// </summary>
        /// <param name="folders">The paths of the folders, relative to the project, or null or empty for the whole project, whatever the
        /// layer below sets. Stored as given: normalize them first (see <see cref="FolderScope.Folders"/>).</param>
        /// <returns>Returns true if the folders have changed.</returns>
        public bool SetFolders(IEnumerable<string> folders)
        {
            List<string> newFolders = folders != null ? new List<string>(folders) : new List<string>();
            if (_areFoldersSet && AreSame(newFolders, _folders))
                return false;

            _areFoldersSet = true;
            _folders = newFolders;
            return true;
        }

        /// <summary>
        /// Unsets the folders of the folder scope, so they come from the layer below.
        /// </summary>
        /// <returns>Returns true if the layer did set the folders.</returns>
        public bool UnsetFolders()
        {
            bool wasSet = _areFoldersSet;
            _areFoldersSet = false;
            _folders.Clear();
            return wasSet;
        }

        #endregion


        #region Pages

        /// <summary>
        /// Gets the number of rows per page, or 0 if the user hasn't chosen one yet. A negative size (from a file edited by hand) is
        /// read as 0.
        /// </summary>
        /// <remarks>
        /// The settings don't check the minimum size: the table view applies its own limits (see
        /// <see cref="TableView.SetPageSize(int)"/>).
        /// </remarks>
        public int PageSize => Mathf.Max(0, _pageSize);

        /// <summary>
        /// Sets the number of rows per page.
        /// </summary>
        /// <param name="pageSize">The number of rows per page, or 0 or less to forget the chosen size, so the table view uses its default
        /// size.</param>
        /// <returns>Returns true if the page size has changed.</returns>
        public bool SetPageSize(int pageSize)
        {
            pageSize = Mathf.Max(0, pageSize);
            if (pageSize == PageSize)
                return false;

            _pageSize = pageSize;
            return true;
        }

        #endregion


        #region Serialization

        /// <summary>
        /// Called before the settings are saved. Writes the version of the format.
        /// </summary>
        public void OnBeforeSerialize()
        {
            _version = CurrentVersion;
        }

        /// <summary>
        /// Called after the settings are loaded. Reads the settings saved before the views, which had every setting set.
        /// </summary>
        /// <remarks>
        /// Before the views, the list of the columns was also their display order, every column had an explicit visibility, and an
        /// empty sort, filter or folder scope meant none (the same as the defaults of the table). The single sort remembered before the
        /// sort on several columns is also read as a list of one column, if the settings have no list yet.
        /// </remarks>
        public void OnAfterDeserialize()
        {
            if (_version >= CurrentVersion)
                return;

            if (!string.IsNullOrEmpty(_sortColumnId) && _sort.Count == 0)
                _sort.Add(new ColumnSort(_sortColumnId, _isSortDescending));

            _sortColumnId = string.Empty;
            _isSortDescending = false;

            if (_columnOrder.Count == 0)
            {
                foreach (ColumnSettings column in _columns)
                {
                    if (column != null && !_columnOrder.Contains(column.Id))
                        _columnOrder.Add(column.Id);
                }
            }

            foreach (ColumnSettings column in _columns)
                column?.ReadVisibilityOfFirstPass();

            _columns.RemoveAll(column => column == null || column.IsEmpty);
            _isSortSet = _sort.Count > 0;
            _isFilterSet = _filter != null;
            _areFoldersSet = _folders.Count > 0;
            _version = CurrentVersion;
        }

        #endregion


        #region Private API

        /// <summary>
        /// Gets the settings of a column.
        /// </summary>
        /// <param name="columnId">The identifier of the column.</param>
        /// <returns>Returns the first settings of the column, or null if it has none.</returns>
        private ColumnSettings FindColumn(string columnId)
        {
            return _columns.Find(column => column != null && column.Id == columnId);
        }

        /// <summary>
        /// Gets the settings of a column, and adds them if it has none yet.
        /// </summary>
        /// <param name="columnId">The identifier of the column.</param>
        /// <returns>Returns the settings of the column.</returns>
        private ColumnSettings GetOrAddColumn(string columnId)
        {
            ColumnSettings column = FindColumn(columnId);
            if (column != null)
                return column;

            column = new ColumnSettings(columnId);
            _columns.Add(column);
            return column;
        }

        /// <summary>
        /// Removes the settings of a column if they set nothing anymore, so the file only holds what the layer sets.
        /// </summary>
        /// <param name="columnId">The identifier of the column.</param>
        private void RemoveIfEmpty(string columnId)
        {
            _columns.RemoveAll(column => column != null && column.Id == columnId && column.IsEmpty);
        }

        /// <summary>
        /// Checks if two lists hold the same items in the same order. Also used by <see cref="LayeredViewSettings"/>.
        /// </summary>
        /// <typeparam name="T">The type of the items.</typeparam>
        /// <param name="a">The first list.</param>
        /// <param name="b">The second list.</param>
        /// <returns>Returns true if both lists hold the same items in the same order.</returns>
        internal static bool AreSame<T>(IReadOnlyList<T> a, IReadOnlyList<T> b)
        {
            if (a.Count != b.Count)
                return false;

            EqualityComparer<T> comparer = EqualityComparer<T>.Default;
            for (int i = 0; i < a.Count; i++)
            {
                if (!comparer.Equals(a[i], b[i]))
                    return false;
            }

            return true;
        }

        #endregion

    }

}
