using System;
using System.Collections.Generic;

namespace SideXP.Database.EditorOnly
{

    /// <summary>
    /// The filters of the table view: a tree of conditions on the columns (see <see cref="FilterNode"/>), combined with the search. A
    /// row is visible if it meets the filters and matches the search.
    /// </summary>
    /// <remarks>
    /// The filter is compiled against the columns of the table when it's set, and when the table changes. Unlike the search, the
    /// filters also apply to the hidden columns: a filter is a deliberate choice of the user, which hiding a column doesn't undo.
    /// The <i>Controls</i> column has no value, so it can't be filtered.
    /// <para>
    /// @todo 21b: the UI, the filters remembered in the view settings (they are cleared when the table changes until then), the
    /// message of the list when the filters hide all the rows, and what happens when a new asset is hidden by the filters.
    /// </para>
    /// </remarks>
    internal partial class TableView
    {

        #region Fields

        /// <summary>
        /// The root of the filter tree, or null if the rows are not filtered.
        /// </summary>
        private FilterNode _filter = null;

        /// <summary>
        /// The condition compiled from <see cref="_filter"/> for the columns of the table, or null if no node of the filter applies.
        /// </summary>
        private Predicate<TableRow> _compiledFilter = null;

        #endregion


        #region Public API

        /// <inheritdoc cref="_filter"/>
        public FilterNode RowFilter => _filter;

        /// <summary>
        /// Checks if a filter applies to the table: at least one of its conditions applies to a column.
        /// </summary>
        public bool IsFiltering => _compiledFilter != null;

        /// <summary>
        /// Shows only the rows that meet a filter, and match the search.
        /// </summary>
        /// <remarks>
        /// The conditions on columns that don't exist, or whose values are of a type they don't support, are ignored. The filter is
        /// compiled when it's set: call this method again after changing the nodes of the tree. The active cell is cleared if its row
        /// is hidden.
        /// </remarks>
        /// <param name="filter">The root of the filter tree, or null to show all the rows that match the search.</param>
        public void SetRowFilter(FilterNode filter)
        {
            _filter = filter;
            if (_pipeline == null)
                return;

            CompileFilter();
            ApplyRowFilter();
            Refresh();
        }

        #endregion


        #region Private API

        /// <summary>
        /// Compiles the filter for the columns of the table.
        /// </summary>
        private void CompileFilter()
        {
            if (_filter == null)
            {
                _compiledFilter = null;
                return;
            }

            Dictionary<string, FilterColumn> columns = new Dictionary<string, FilterColumn>(_columns.Count);
            for (int i = 0; i < _columns.Count; i++)
            {
                TableViewColumn column = _columns[i];
                if (column.IsControls)
                    continue;

                columns[column.Id] = new FilterColumn(i, column.Accessor.ValueType, value => ToFilterText(column, value));
            }

            _compiledFilter = _filter.Compile(columns);
        }

        /// <summary>
        /// Sets the filter of the pipeline: the compiled filter and the search, both optional. The rows are filtered on the next
        /// update of the pipeline.
        /// </summary>
        private void ApplyRowFilter()
        {
            // With both, the filter is checked first: its conditions usually compare values, while the search may build the text of
            // the row
            Predicate<TableRow> filter = _compiledFilter;
            if (filter != null && IsSearching)
                _pipeline.Filter = row => filter(row) && MatchesSearch(row);
            else if (filter != null)
                _pipeline.Filter = filter;
            else if (IsSearching)
                _pipeline.Filter = MatchesSearch;
            else
                _pipeline.Filter = null;
        }

        /// <summary>
        /// Converts the value of a cell to text through the handler of its column, for the text conditions of the filters.
        /// </summary>
        /// <remarks>
        /// An error of the handler is logged once per column (see <see cref="ToCellText"/>), and the cell is then filtered as an empty
        /// text.
        /// </remarks>
        /// <param name="column">The column of the cell.</param>
        /// <param name="value">The cached value of the cell.</param>
        /// <returns>Returns the text of the cell, or null if the handler failed.</returns>
        private static string ToFilterText(TableViewColumn column, object value)
        {
            return ToCellText(column, value, "for the filters");
        }

        #endregion

    }

}
