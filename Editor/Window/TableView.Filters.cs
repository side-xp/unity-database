using System;
using System.Collections.Generic;

using UnityEngine.UIElements;
using UnityEditor;

using SideXP.Core.EditorOnly;

using Object = UnityEngine.Object;

namespace SideXP.Database.EditorOnly
{

    /// <summary>
    /// The filters of the table view: a tree of conditions on the columns (see <see cref="FilterNode"/>), combined with the search, and
    /// the folder scope (see <see cref="FolderScope"/>). A row is visible if its asset is in the scope, meets the filters and matches
    /// the search.
    /// </summary>
    /// <remarks>
    /// The filter is compiled against the columns of the table when it's set, and when the table changes. Unlike the search, the
    /// filters also apply to the hidden columns: a filter is a deliberate choice of the user, which hiding a column doesn't undo.
    /// The <i>Controls</i> column has no value, so it can't be filtered.
    /// <para>
    /// The folder scope limits the source of the pipeline: the assets outside of its folders are not loaded, so they are not rows at
    /// all, and the status line counts the rows of the scope only.
    /// </para>
    /// <para>
    /// Both are remembered in the settings of the table (see <see cref="ViewSettings"/>), and kept when an asset shown by the view (a
    /// new asset, or an asset of <i>Open In Database</i>) is hidden by them: the view warns instead (see
    /// <see cref="ShowAsset(Object)"/>).
    /// </para>
    /// </remarks>
    internal partial class TableView
    {

        #region Fields

        private const string FilterByColumnText = "Filter by This Column";
        private const string NoAssetInScopeMessage = "There is no asset of this table in the folders of the scope.";
        private const string NoFilterMatchMessage = "No row meets the filters.";
        private const string NoSearchAndFilterMatchMessage = "No row matches the search and the filters.";

        /// <summary>
        /// The root of the filter tree, or null if the rows are not filtered.
        /// </summary>
        private FilterNode _filter = null;

        /// <summary>
        /// The condition compiled from <see cref="_filter"/> for the columns of the table, or null if no node of the filter applies.
        /// </summary>
        private Predicate<TableRow> _compiledFilter = null;

        /// <summary>
        /// The filter of the first pass of the pipeline (see <see cref="TablePipeline.Filter"/>), before the computed values are
        /// computed: <see cref="_compiledFilter"/>, or, if it uses computed columns, the filter compiled with their conditions counted
        /// as met. Null if it keeps all the rows.
        /// </summary>
        private Predicate<TableRow> _firstPassFilter = null;

        /// <summary>
        /// The filter of the second pass of the pipeline (see <see cref="TablePipeline.PostFilter"/>), after the computed values are
        /// computed: <see cref="_compiledFilter"/> if it uses computed columns, null otherwise (the first pass already applied it).
        /// </summary>
        private Predicate<TableRow> _secondPassFilter = null;

        /// <summary>
        /// The identifiers of the columns used by the conditions of the filter that apply to the table, marked in their header.
        /// </summary>
        private HashSet<string> _filteredColumnIds = new HashSet<string>();

        /// <summary>
        /// The folders the table is limited to.
        /// </summary>
        private FolderScope _folderScope = FolderScope.WholeProject;

        /// <summary>
        /// The source of the assets of the table, which applies the folder scope, or null if no table is displayed.
        /// </summary>
        private ProjectAssetSource _source = null;

        /// <summary>
        /// Called when the user asks to filter the rows by a column from the menu of its header, with the identifier of the column.
        /// The owner of the view adds the condition in its filter editor (see <see cref="ViewPanel"/>).
        /// </summary>
        public event Action<string> FilterByColumnRequested;

        #endregion


        #region Public API

        /// <inheritdoc cref="_filter"/>
        public FilterNode RowFilter => _filter;

        /// <summary>
        /// Checks if a filter applies to the table: at least one of its conditions applies to a column.
        /// </summary>
        public bool IsFiltering => _compiledFilter != null;

        /// <summary>
        /// Shows only the rows that meet a filter, and match the search. The filter is remembered with the settings of the table.
        /// </summary>
        /// <remarks>
        /// The conditions on columns that don't exist, or whose values are of a type they don't support, are ignored. The filter is
        /// compiled when it's set: call this method again after changing the nodes of the tree, which also saves the settings. The
        /// active cell is cleared if its row is hidden, and the first page is shown.
        /// </remarks>
        /// <param name="filter">The root of the filter tree, or null to show all the rows that match the search.</param>
        public void SetRowFilter(FilterNode filter)
        {
            _filter = filter;
            if (_pipeline == null)
                return;

            // Always saved, since the nodes of the same tree may have changed
            _viewSettings.SetFilter(filter);
            OnViewSettingsChanged();

            ResetPage();
            CompileFilter();
            ApplyRowFilter();
            UpdateFilterIcons();
            Refresh();
        }

        /// <summary>
        /// Gets the columns that the filters can use: all the columns except <i>Controls</i>, hidden ones included, since the filters
        /// also apply to them.
        /// </summary>
        /// <returns>Returns the columns in their display order, or an empty list if no table is displayed.</returns>
        public List<TableViewColumn> GetFilterableColumns()
        {
            if (_viewSettings == null)
                return new List<TableViewColumn>();

            List<TableViewColumn> columns = GetColumnOrder().ConvertAll(columnIndex => _columns[columnIndex]);
            columns.RemoveAll(column => !IsFilterable(column));
            return columns;
        }

        /// <inheritdoc cref="_folderScope"/>
        public FolderScope FolderScope => _folderScope;

        /// <summary>
        /// Limits the table to the assets of some folders and of their subfolders, and loads the assets again. The folders are
        /// remembered with the settings of the table.
        /// </summary>
        /// <remarks>
        /// The folders that don't exist are ignored: if none of them exists, the table has no row. The active cell and the selected
        /// cells of the assets that leave the table are cleared, and the first page is shown.
        /// </remarks>
        /// <param name="folders">The paths of the folders, relative to the project (for example <c>Assets/Items</c>), or null or empty
        /// for the whole project.</param>
        public void SetFolderScope(IEnumerable<string> folders)
        {
            _folderScope = new FolderScope(folders);
            if (_pipeline == null)
                return;

            if (_viewSettings.SetFolders(_folderScope.Folders))
                OnViewSettingsChanged();

            ResetPage();
            ApplyFolderScope();
            _pipeline.InvalidateSource();
            Refresh();
        }

        #endregion


        #region Private API

        /// <summary>
        /// Gives the folder scope to the source of the pipeline, for its next loads, and to the change tracker.
        /// </summary>
        private void ApplyFolderScope()
        {
            _source.Scope = _folderScope;
            if (_changeTracker != null)
                _changeTracker.Scope = _folderScope;
        }

        /// <summary>
        /// Compiles the filter for the columns of the table, and splits it in the two passes of the pipeline if it uses computed columns.
        /// </summary>
        /// <remarks>
        /// The first pass runs before the computed values are computed: the conditions on computed columns count as met, so it keeps
        /// every row that may meet the whole filter, and these rows are the visible assets of the computed columns. The second pass
        /// checks the whole filter on these rows, once their computed values are known.
        /// </remarks>
        private void CompileFilter()
        {
            _firstPassFilter = null;
            _secondPassFilter = null;
            if (_filter == null)
            {
                _compiledFilter = null;
                _filteredColumnIds.Clear();
                return;
            }

            // A condition on a column left out is ignored, like a condition on a column that doesn't exist anymore
            Dictionary<string, FilterColumn> columns = new Dictionary<string, FilterColumn>(_columns.Count);
            Dictionary<string, FilterColumn> firstPassColumns = new Dictionary<string, FilterColumn>(_columns.Count);
            HashSet<string> computedColumnIds = new HashSet<string>();
            for (int i = 0; i < _columns.Count; i++)
            {
                TableViewColumn column = _columns[i];
                if (!IsFilterable(column))
                    continue;

                Type valueType = column.Accessor.ValueType;
                Func<object, string> toText = value => ToFilterText(column, value);
                columns[column.Id] = new FilterColumn(i, valueType, toText);
                firstPassColumns[column.Id] = new FilterColumn(i, valueType, toText, column.IsComputed);
                if (column.IsComputed)
                    computedColumnIds.Add(column.Id);
            }

            _compiledFilter = _filter.Compile(columns);
            _filteredColumnIds = FilterConditions.GetFilteredColumnIds(_filter, columns);

            if (!_filteredColumnIds.Overlaps(computedColumnIds))
            {
                _firstPassFilter = _compiledFilter;
                return;
            }

            Predicate<TableRow> firstPassFilter = _filter.Compile(firstPassColumns);
            _firstPassFilter = firstPassFilter == FilterNode.Deferred ? null : firstPassFilter;
            _secondPassFilter = _compiledFilter;
        }

        /// <summary>
        /// Adds the item that filters the rows by the clicked column to the context menu of a header (see
        /// <see cref="FilterByColumnRequested"/>).
        /// </summary>
        /// <param name="menu">The context menu of the header.</param>
        /// <param name="clickedColumn">The column of the list view under the pointer, or null if there is none.</param>
        /// <returns>Returns true if the item has been added.</returns>
        private bool AppendFilterItem(DropdownMenu menu, Column clickedColumn)
        {
            // The columns of the list views are named after the identifiers of the columns of the table
            string columnId = clickedColumn?.name;
            TableViewColumn column = string.IsNullOrEmpty(columnId) ? null : _columns.Find(item => item.Id == columnId);
            if (column == null)
                return false;

            DropdownMenuAction.Status status = IsFilterable(column)
                ? DropdownMenuAction.Status.Normal
                : DropdownMenuAction.Status.Disabled;
            menu.AppendAction(FilterByColumnText, action => FilterByColumnRequested?.Invoke(column.Id), status);
            return true;
        }

        /// <summary>
        /// Checks if the filters can use a column.
        /// </summary>
        /// <param name="column">The column to check.</param>
        /// <returns>Returns false for the <i>Controls</i> column, which has no value.</returns>
        private static bool IsFilterable(TableViewColumn column)
        {
            return !column.IsControls;
        }

        /// <summary>
        /// Shows the mark of the filtered columns on the headers of both list views.
        /// </summary>
        private void UpdateFilterIcons()
        {
            _listView?.Query(className: HeaderClass).ForEach(UpdateFilterIcon);
            _frozenListView?.Query(className: HeaderClass).ForEach(UpdateFilterIcon);
        }

        /// <summary>
        /// Shows the mark of the filters on the header of a column if a condition of the filter applies to it, or hides it otherwise.
        /// </summary>
        /// <param name="header">The header of the column, created by <see cref="MakeHeader"/>.</param>
        private void UpdateFilterIcon(VisualElement header)
        {
            Icon filterIcon = header.Q<Icon>(className: HeaderFilterIconClass);
            if (filterIcon != null)
                filterIcon.SetDisplayed(header.userData is TableViewColumn column && _filteredColumnIds.Contains(column.Id));
        }

        /// <summary>
        /// Sets the filters of the pipeline: the first pass of the filter, then the second pass of the filter and the search, all
        /// optional. The rows are filtered on the next update of the pipeline.
        /// </summary>
        /// <remarks>
        /// The search is part of the second pass, so it can search the computed columns, and typing a search doesn't compute their
        /// values again: they only depend on the filter (the visible assets of their context are the rows of the first pass).
        /// </remarks>
        private void ApplyRowFilter()
        {
            // Set only if it changed, since a new filter of the first pass computes the computed values again
            if (_pipeline.Filter != _firstPassFilter)
                _pipeline.Filter = _firstPassFilter;

            // With both, the filter is checked first: its conditions usually compare values, while the search may build the text of
            // the row
            Predicate<TableRow> filter = _secondPassFilter;
            if (filter != null && IsSearching)
                _pipeline.PostFilter = row => filter(row) && MatchesSearch(row);
            else if (filter != null)
                _pipeline.PostFilter = filter;
            else if (IsSearching)
                _pipeline.PostFilter = MatchesSearch;
            else
                _pipeline.PostFilter = null;
        }

        /// <summary>
        /// Checks if the filters hide a row, whatever the search.
        /// </summary>
        /// <param name="row">The row to check.</param>
        /// <returns>Returns true if a filter applies and the row doesn't meet it, in either pass.</returns>
        private bool IsHiddenByFilters(TableRow row)
        {
            // The second pass reads the computed values, which only the rows of the first pass have
            if (_firstPassFilter != null && !_firstPassFilter(row))
                return true;

            return _secondPassFilter != null && !_secondPassFilter(row);
        }

        /// <summary>
        /// Warns that an asset the view was asked to show has no visible row, with the reason: outside of the folder scope, hidden by
        /// the filters, or not in the table.
        /// </summary>
        /// <param name="asset">The asset that can't be shown.</param>
        /// <param name="row">The row of the asset, or null if it has none.</param>
        private void WarnAssetNotShown(Object asset, TableRow row)
        {
            string reason;
            if (row != null)
                reason = "is hidden by the filters of";
            else if (!_folderScope.Contains(AssetDatabase.GetAssetPath(asset)))
                reason = "is outside of the folder scope of";
            else
                reason = "has no row in";

            ShowMessage($"The asset \"{asset.name}\" {reason} the table \"{_table.Name}\".", asset);
        }

        /// <summary>
        /// Gets the message of a table without any asset, in the project or in the folders of the scope.
        /// </summary>
        /// <returns>Returns the message to display.</returns>
        private string GetNoAssetMessage()
        {
            return _folderScope.IsWholeProject ? NoAssetMessage : NoAssetInScopeMessage;
        }

        /// <summary>
        /// Gets the message of the list when the search, the filters or both hide all the rows.
        /// </summary>
        /// <returns>Returns the message to display.</returns>
        private string GetNoMatchMessage()
        {
            if (IsFiltering && IsSearching)
                return NoSearchAndFilterMatchMessage;

            return IsFiltering ? NoFilterMatchMessage : NoSearchMatchMessage;
        }

        /// <summary>
        /// Converts the value of a cell to text through the handler of its column, for the text conditions of the filters.
        /// </summary>
        /// <remarks>
        /// An error of the handler is logged once per column (see <see cref="TableViewColumn.ToText(object, string)"/>), and the cell is
        /// then filtered as an empty text.
        /// </remarks>
        /// <param name="column">The column of the cell.</param>
        /// <param name="value">The cached value of the cell.</param>
        /// <returns>Returns the text of the cell, or null if the handler failed.</returns>
        private static string ToFilterText(TableViewColumn column, object value)
        {
            return column.ToText(value, "for the filters");
        }

        #endregion

    }

}
