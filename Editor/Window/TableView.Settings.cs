using System;
using System.Collections.Generic;

using UnityEngine.UIElements;

namespace SideXP.Database.EditorOnly
{

    /// <summary>
    /// The remembered settings of the table view: the order, the widths and the visibility of the columns, and the sort, stored as
    /// the user's default view of the table (see <see cref="UserViews"/>).
    /// </summary>
    /// <remarks>
    /// The list views have no public event for a reordered column, so the order is read from the headers after each click in the
    /// main list (dragging a header ends with one), and when the table is left. The widths are saved when the columns report a new
    /// width, which only happens when the user resizes them. The columns are shown and hidden from the context menu of the headers.
    /// Saves are grouped by <see cref="UserViews.ScheduleSave"/>.
    /// <para>
    /// The hidden columns stay in the pipeline (their values are still read), so showing a column again doesn't load the assets.
    /// </para>
    /// </remarks>
    internal partial class TableView
    {

        #region Fields

        /// <summary>
        /// The name of the property of <see cref="Column"/> reported when its width changes.
        /// </summary>
        private const string WidthPropertyName = nameof(Column.width);

        private const string ShowAllColumnsText = "Show All Columns";
        private const string ResetViewText = "Reset View";

        /// <summary>
        /// The remembered settings of the displayed table, or null if no table is displayed.
        /// </summary>
        private ViewSettings _viewSettings = null;

        /// <summary>
        /// The indices in the pipeline of the columns that are not hidden, in their display order.
        /// </summary>
        private List<int> _displayedColumns = new List<int>();

        /// <summary>
        /// Checks if each column is displayed, by its index in the pipeline.
        /// </summary>
        private bool[] _isColumnDisplayed = Array.Empty<bool>();

        #endregion


        #region Column visibility

        /// <summary>
        /// Called when the header of a list view is right-clicked. Replaces the items of Unity with the item that adds the clicked
        /// column to the selection (see <see cref="AppendColumnSelectionItem"/>), the items of the sort (see
        /// <see cref="AppendSortItems"/>), and the items that show and hide the columns of the table.
        /// </summary>
        /// <remarks>
        /// The items of Unity only know the columns of their own list view, and hiding a column directly in a list view would ignore
        /// the frozen column and the remembered settings.
        /// </remarks>
        /// <param name="evt">The emitted event.</param>
        /// <param name="clickedColumn">The column under the pointer, or null if there is none.</param>
        private void OnHeaderContextMenuPopulate(ContextualMenuPopulateEvent evt, Column clickedColumn)
        {
            evt.menu.ClearItems();
            if (_viewSettings == null)
                return;

            if (AppendColumnSelectionItem(evt.menu, clickedColumn))
                evt.menu.AppendSeparator();

            if (AppendSortItems(evt.menu, clickedColumn))
                evt.menu.AppendSeparator();

            // The hidden columns are listed at their place, so the menu shows the whole order of the table
            bool hasHiddenColumns = _displayedColumns.Count < _columns.Count;
            foreach (int columnIndex in GetColumnOrder())
            {
                TableViewColumn column = _columns[columnIndex];
                bool isDisplayed = _isColumnDisplayed[columnIndex];

                // The last displayed column can't be hidden, since the table would have nothing to show
                DropdownMenuAction.Status status = isDisplayed ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal;
                if (isDisplayed && _displayedColumns.Count == 1)
                    status |= DropdownMenuAction.Status.Disabled;

                evt.menu.AppendAction(column.Name, action => SetColumnHidden(column, isDisplayed), status);
            }

            evt.menu.AppendSeparator();
            evt.menu.AppendAction(ShowAllColumnsText, action => ShowAllColumns(),
                hasHiddenColumns ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
            evt.menu.AppendAction(ResetViewText, action => ResetView(), DropdownMenuAction.Status.Normal);
        }

        /// <summary>
        /// Hides or shows a column, and remembers it.
        /// </summary>
        /// <param name="column">The column to hide or show.</param>
        /// <param name="isHidden">If enabled, the column is hidden.</param>
        private void SetColumnHidden(TableViewColumn column, bool isHidden)
        {
            // Read before the list views are rebuilt, in case the order changed without a release over the main list
            SaveColumnOrder();
            if (_viewSettings.SetHidden(column.Id, isHidden))
                ApplyColumnChanges();
        }

        /// <summary>
        /// Shows all the hidden columns, and remembers it.
        /// </summary>
        private void ShowAllColumns()
        {
            SaveColumnOrder();
            bool hasChanged = false;
            foreach (TableViewColumn column in _columns)
                hasChanged |= _viewSettings.SetHidden(column.Id, false);

            if (hasChanged)
                ApplyColumnChanges();
        }

        /// <summary>
        /// Gives the columns their default order, widths and visibility, and removes the sort.
        /// </summary>
        private void ResetView()
        {
            _viewSettings.Reset();
            ApplyColumnChanges();
        }

        /// <summary>
        /// Applies the remembered columns after they changed: rebuilds the list views, and updates what depends on the displayed
        /// columns (active cell, sort, search). Saves the settings.
        /// </summary>
        /// <remarks>
        /// The list views are always rebuilt, since the frozen column is the first displayed column: hiding it, or showing a column
        /// before it, moves another column to the frozen list. The scroll position is kept.
        /// </remarks>
        private void ApplyColumnChanges()
        {
            // The elements of the cells are destroyed with the list views
            EndEdit(true, false);
            UpdateDisplayedColumns();

            if (!_activeCell.IsNone && !IsColumnDisplayed(_activeCell.ColumnId))
                SetActiveCell(CellId.None);

            // The active cell gets its new element when it's bound again
            _activeCellElement = null;

            // A sorted column without header can't show its sort nor be changed: it leaves the sort, and the next columns take its
            // priority
            ApplyRememberedSort();
            SaveSort();
            // The texts of the rows contain the displayed columns only
            ClearSearchTexts();
            if (IsSearching)
                _pipeline.InvalidateFilter();

            RebuildListViews(true);
            Refresh();
            UserViews.instance.ScheduleSave();
        }

        #endregion


        #region Private API

        /// <summary>
        /// Updates the displayed columns from the remembered settings: the columns that are not hidden, in their remembered order.
        /// </summary>
        /// <remarks>
        /// If all the columns are hidden (which the menu prevents, but a file edited by hand can contain), the first one is shown.
        /// </remarks>
        private void UpdateDisplayedColumns()
        {
            List<int> order = GetColumnOrder();
            _displayedColumns = order.FindAll(columnIndex => !_viewSettings.IsHidden(_columns[columnIndex].Id));
            if (_displayedColumns.Count == 0 && order.Count > 0)
            {
                _viewSettings.SetHidden(_columns[order[0]].Id, false);
                _displayedColumns.Add(order[0]);
            }

            _isColumnDisplayed = new bool[_columns.Count];
            foreach (int columnIndex in _displayedColumns)
                _isColumnDisplayed[columnIndex] = true;
        }

        /// <summary>
        /// Gets all the columns of the table, hidden ones included, in their remembered order.
        /// </summary>
        /// <returns>Returns the indices of the columns in the pipeline, in their display order.</returns>
        private List<int> GetColumnOrder()
        {
            List<string> columnIds = _columns.ConvertAll(column => column.Id);
            List<string> order = _viewSettings.GetColumnOrder(columnIds, IsHiddenByDefault);
            return order.ConvertAll(columnId => columnIds.IndexOf(columnId));
        }

        /// <summary>
        /// Checks if a column is hidden until the user shows it: the <i>Asset Name</i> column of a table that hides it.
        /// </summary>
        /// <param name="columnId">The identifier of the column.</param>
        /// <returns>Returns true if the column is hidden by default.</returns>
        private bool IsHiddenByDefault(string columnId)
        {
            return columnId == TableViewColumn.AssetNameId && _table != null && _table.HideAssetName;
        }

        /// <summary>
        /// Checks if a column is displayed.
        /// </summary>
        /// <param name="columnIndex">The index of the column in the pipeline.</param>
        /// <returns>Returns true if the column is displayed.</returns>
        private bool IsColumnDisplayed(int columnIndex)
        {
            return columnIndex >= 0 && columnIndex < _isColumnDisplayed.Length && _isColumnDisplayed[columnIndex];
        }

        /// <summary>
        /// Checks if a column is displayed.
        /// </summary>
        /// <param name="columnId">The identifier of the column.</param>
        /// <returns>Returns true if the column exists and is displayed.</returns>
        private bool IsColumnDisplayed(string columnId)
        {
            return IsColumnDisplayed(_columns.FindIndex(column => column.Id == columnId));
        }

        /// <summary>
        /// Gets the width of a column when the list views are built: the remembered width, or its default width.
        /// </summary>
        /// <param name="column">The column of which to get the width.</param>
        /// <returns>Returns the width of the column, in pixels.</returns>
        private float GetInitialWidth(TableViewColumn column)
        {
            float width = _viewSettings.GetWidth(column.Id);
            if (width > 0)
                return width;

            // A flexible column has no width of its own: it starts with this width, then grows to fill the list
            return column.IsFlexible ? FlexibleColumnWidth : column.Width;
        }

        /// <summary>
        /// Sorts the pipeline with the remembered sort. The columns that are missing, hidden or can't be sorted are skipped, and the
        /// next ones take their priority.
        /// </summary>
        private void ApplyRememberedSort()
        {
            SetPipelineSort(_viewSettings.GetSort(CanSortColumn));
        }

        /// <summary>
        /// Remembers the sort of the pipeline, replacing the remembered sort.
        /// </summary>
        private void SaveSort()
        {
            if (_viewSettings.SetSort(GetSort()))
                UserViews.instance.ScheduleSave();
        }

        /// <summary>
        /// Remembers the width of a column of a list view when the user resizes it.
        /// </summary>
        /// <param name="column">The column of the table displayed by the column of the list view.</param>
        /// <param name="listColumn">The column of the list view.</param>
        private void TrackColumnWidth(TableViewColumn column, Column listColumn)
        {
            listColumn.propertyChanged += (sender, args) =>
            {
                // The list views only write the width when the user resizes a column (the stretched widths are internal). The tool
                // never uses widths in percent
                bool isPixelWidth = args.propertyName.ToString() == WidthPropertyName && listColumn.width.unit == LengthUnit.Pixel;
                if (_viewSettings == null || !isPixelWidth)
                    return;

                if (_viewSettings.SetWidth(column.Id, listColumn.width.value))
                    UserViews.instance.ScheduleSave();
            };
        }

        /// <summary>
        /// Called when a pointer is released over the main list, which ends the drag of a header. Remembers the order of the columns
        /// once the list view has applied it.
        /// </summary>
        /// <param name="evt">The emitted event.</param>
        private void OnMainListPointerUp(PointerUpEvent evt)
        {
            // Trickle down: the header applies the new order after this callback
            schedule.Execute(SaveColumnOrder);
        }

        /// <summary>
        /// Remembers the order of the columns of the main list, as the user arranged them by dragging their headers.
        /// </summary>
        /// <remarks>
        /// The order is read from the hierarchy of the headers, which the list view rearranges when a column is moved. The frozen
        /// column can't be moved, so it keeps its place.
        /// </remarks>
        private void SaveColumnOrder()
        {
            if (_listView == null || _viewSettings == null)
                return;

            List<string> order = new List<string>(_listView.columns.Count);
            _listView.Query(className: HeaderClass).ForEach(header =>
            {
                if (header.userData is TableViewColumn column)
                    order.Add(column.Id);
            });

            // Something else than the headers of the columns has been found: the order can't be trusted
            if (order.Count != _listView.columns.Count)
                return;

            if (_viewSettings.SetColumnOrder(order))
                UserViews.instance.ScheduleSave();
        }

        #endregion

    }

}
