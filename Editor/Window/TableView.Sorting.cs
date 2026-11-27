using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UIElements;

using SideXP.Core.EditorOnly;

namespace SideXP.Database.EditorOnly
{

    /// <summary>
    /// The sort of the table view, on one or several columns: a click on a header sorts by its column only, an Alt-click adds the
    /// column to the sort or changes it, and the context menu of the headers adds and removes columns (see
    /// <see cref="MultiColumnSort"/>).
    /// </summary>
    /// <remarks>
    /// The pipeline sorts the rows. The list views only report the plain clicks on their headers, and their sort descriptions are
    /// kept in sync with the sort of the pipeline, each list with the columns it displays. Their sort indicators are hidden by the
    /// style sheet, and replaced with an icon and the priority of the column (see <see cref="UpdateSortIcons"/>).
    /// </remarks>
    internal partial class TableView
    {

        #region Fields

        private const string AddToSortText = "Add to Sort";
        private const string RemoveFromSortText = "Remove from Sort";
        private const string ClearSortText = "Clear Sort";

        #endregion


        #region Clicks

        /// <summary>
        /// Called when the user clicks the header of a sortable column without modifier, in the frozen list or in the main list. Sorts
        /// the rows by that column only (see <see cref="MultiColumnSort.SortBy"/>).
        /// </summary>
        /// <remarks>
        /// The list view has already changed its sort descriptions, which end with the clicked column. Only the clicked column is read
        /// from them: the new sort comes from the sort of the pipeline, since each list view only knows its own columns.
        /// </remarks>
        /// <param name="listView">The list view whose header has been clicked.</param>
        private void OnColumnSortingChanged(MultiColumnListView listView)
        {
            // Changing the sort descriptions raises this event again
            if (_isUpdatingSort || _pipeline == null)
                return;

            SortColumnDescriptions descriptions = listView.sortColumnDescriptions;
            int columnIndex = descriptions.Count > 0 ? GetColumnIndex(listView, descriptions[descriptions.Count - 1]) : -1;
            List<ColumnSort> sort = columnIndex >= 0
                ? MultiColumnSort.SortBy(GetSort(), _columns[columnIndex].Id)
                : new List<ColumnSort>();

            ChangeSort(sort);
        }

        /// <summary>
        /// Called when the header of a column is clicked. Alt-click adds the column to the sort, or changes its direction, or removes
        /// it (see <see cref="MultiColumnSort.Cycle"/>).
        /// </summary>
        /// <remarks>
        /// The event is stopped so the header column of the list view doesn't sort by that column only, nor drag the column, like the
        /// clicks that select columns (see <see cref="OnHeaderPointerDown(PointerDownEvent)"/>).
        /// </remarks>
        /// <param name="evt">The emitted event.</param>
        private void OnHeaderSortPointerDown(PointerDownEvent evt)
        {
            if (evt.button != (int)MouseButton.LeftMouse || !IsSortCycleClick(evt.altKey, evt.actionKey, evt.shiftKey))
                return;

            // Stopped even on the columns that can't be sorted, so an Alt-click never drags a column
            evt.StopPropagation();
            if (evt.currentTarget is VisualElement header && header.userData is TableViewColumn column && column.IsSortable)
                ChangeSort(MultiColumnSort.Cycle(GetSort(), column.Id));
        }

        /// <summary>
        /// Checks if a click on a column header changes the sort of its column and keeps the other sorted columns.
        /// </summary>
        /// <param name="isAltHeld">Checks if Alt is held.</param>
        /// <param name="isActionKeyHeld">Checks if Ctrl (Command on macOS) is held.</param>
        /// <param name="isShiftHeld">Checks if Shift is held.</param>
        /// <returns>
        /// Returns true if Alt is held alone. With Ctrl or Shift, the click selects the column (see
        /// <see cref="IsColumnSelectionClick"/>).
        /// </returns>
        private static bool IsSortCycleClick(bool isAltHeld, bool isActionKeyHeld, bool isShiftHeld)
        {
            return isAltHeld && !isActionKeyHeld && !isShiftHeld;
        }

        #endregion


        #region Menu

        /// <summary>
        /// Adds the sort items to the context menu of a header: <i>Add to Sort</i> or <i>Remove from Sort</i> for the clicked column,
        /// and <i>Clear Sort</i>.
        /// </summary>
        /// <param name="menu">The context menu of the header.</param>
        /// <param name="clickedColumn">The column of the list view under the pointer, or null if there is none.</param>
        /// <returns>Returns true if items have been added.</returns>
        private bool AppendSortItems(DropdownMenu menu, Column clickedColumn)
        {
            if (_pipeline == null)
                return false;

            List<ColumnSort> sort = GetSort();

            // The columns of the list views are named after the identifiers of the columns of the table
            string columnId = clickedColumn?.name;
            TableViewColumn column = string.IsNullOrEmpty(columnId) ? null : _columns.Find(item => item.Id == columnId);
            if (column != null)
            {
                // The actions read the sort again, in case it changed while the menu was open
                if (MultiColumnSort.IndexOf(sort, column.Id) >= 0)
                {
                    menu.AppendAction(RemoveFromSortText, action => ChangeSort(MultiColumnSort.Remove(GetSort(), column.Id)),
                        DropdownMenuAction.Status.Normal);
                }
                else
                {
                    menu.AppendAction(AddToSortText, action => ChangeSort(MultiColumnSort.Add(GetSort(), column.Id)),
                        column.IsSortable ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
                }
            }

            menu.AppendAction(ClearSortText, action => ChangeSort(new List<ColumnSort>()),
                sort.Count > 0 ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
            return true;
        }

        #endregion


        #region Private API

        /// <summary>
        /// Gets the sort of the pipeline, as the identifiers of its columns.
        /// </summary>
        /// <returns>Returns the sorted columns by priority, or an empty list if no table is displayed or the rows are not sorted.</returns>
        private List<ColumnSort> GetSort()
        {
            List<ColumnSort> sort = new List<ColumnSort>();
            if (_pipeline == null)
                return sort;

            foreach (SortKey key in _pipeline.SortKeys)
                sort.Add(new ColumnSort(_columns[key.ColumnIndex].Id, key.IsDescending));

            return sort;
        }

        /// <summary>
        /// Sorts the rows by the given columns, shows the sort in the headers, remembers it and refreshes the view, on the first page.
        /// </summary>
        /// <param name="sort">The columns used to sort the rows, by priority. If empty, the rows are in the order of the source.</param>
        private void ChangeSort(IReadOnlyList<ColumnSort> sort)
        {
            if (_pipeline == null)
                return;

            // The rows of the displayed page are other rows once sorted again
            ResetPage();
            SetPipelineSort(sort);
            ShowSortInHeaders();
            UpdateSortIcons();
            SaveSort();
            Refresh();
        }

        /// <summary>
        /// Sorts the rows of the pipeline by the given columns, through their handlers.
        /// </summary>
        /// <param name="sort">The columns used to sort the rows, by priority. The columns that are missing, hidden or can't be sorted
        /// are skipped (see <see cref="CanSortColumn(int)"/>), and so are the duplicates.</param>
        private void SetPipelineSort(IReadOnlyList<ColumnSort> sort)
        {
            List<SortKey> keys = new List<SortKey>(sort.Count);
            foreach (ColumnSort columnSort in sort)
            {
                int columnIndex = _columns.FindIndex(column => column.Id == columnSort.ColumnId);
                if (!CanSortColumn(columnIndex) || keys.Exists(key => key.ColumnIndex == columnIndex))
                    continue;

                TableViewColumn column = _columns[columnIndex];
                keys.Add(new SortKey(columnIndex, columnSort.IsDescending, (a, b) => Compare(column, a, b)));
            }

            _pipeline.SetSort(keys);
        }

        /// <summary>
        /// Checks if the rows can be sorted by a column: it must be sortable, and displayed, since a column without header can't show
        /// its sort.
        /// </summary>
        /// <param name="columnIndex">The index of the column in the pipeline.</param>
        /// <returns>Returns true if the column exists, is displayed and can be sorted.</returns>
        private bool CanSortColumn(int columnIndex)
        {
            return IsColumnDisplayed(columnIndex) && _columns[columnIndex].IsSortable;
        }

        /// <inheritdoc cref="CanSortColumn(int)"/>
        /// <param name="columnId">The identifier of the column.</param>
        private bool CanSortColumn(string columnId)
        {
            return CanSortColumn(_columns.FindIndex(column => column.Id == columnId));
        }

        /// <summary>
        /// Shows the sort of the pipeline in the sort descriptions of both list views, each with the columns it displays.
        /// </summary>
        /// <remarks>
        /// The descriptions are not displayed (see <see cref="UpdateSortIcons"/>), but the list views read them when a header is
        /// clicked.
        /// </remarks>
        private void ShowSortInHeaders()
        {
            // Changing the sort descriptions raises the event of a sort chosen by the user
            _isUpdatingSort = true;
            try
            {
                ShowSortInHeader(_frozenListView);
                ShowSortInHeader(_listView);
            }
            finally
            {
                _isUpdatingSort = false;
            }
        }

        /// <summary>
        /// Shows the sort of the pipeline in the sort descriptions of a list view: the sorted columns it displays, by priority.
        /// </summary>
        /// <param name="listView">The list view of which to set the sort descriptions, or null to do nothing.</param>
        private void ShowSortInHeader(MultiColumnListView listView)
        {
            if (listView == null)
                return;

            SortColumnDescriptions descriptions = listView.sortColumnDescriptions;
            descriptions.Clear();
            foreach (SortKey key in _pipeline.SortKeys)
            {
                string columnId = _columns[key.ColumnIndex].Id;
                if (!listView.columns.Contains(columnId))
                    continue;

                SortDirection direction = key.IsDescending ? SortDirection.Descending : SortDirection.Ascending;
                descriptions.Add(new SortColumnDescription(columnId, direction));
            }
        }

        /// <summary>
        /// Shows the sort of the pipeline on the headers of both list views.
        /// </summary>
        private void UpdateSortIcons()
        {
            _listView?.Query(className: HeaderClass).ForEach(UpdateSortIcon);
            _frozenListView?.Query(className: HeaderClass).ForEach(UpdateSortIcon);
        }

        /// <summary>
        /// Shows the icon of the sort on the header of a column if the pipeline is sorted by that column, or hides it otherwise. When
        /// the rows are sorted by several columns, the icon is preceded by the priority of the column, from 1.
        /// </summary>
        /// <param name="header">The header of the column, created by <see cref="MakeHeader"/>.</param>
        private void UpdateSortIcon(VisualElement header)
        {
            Icon sortIcon = header.Q<Icon>(className: HeaderSortIconClass);
            Label priorityLabel = header.Q<Label>(className: HeaderSortPriorityClass);
            if (sortIcon == null || priorityLabel == null)
                return;

            int priority = -1;
            bool isDescending = false;
            IReadOnlyList<SortKey> keys = _pipeline != null ? _pipeline.SortKeys : Array.Empty<SortKey>();
            for (int i = 0; i < keys.Count; i++)
            {
                if (header.userData == _columns[keys[i].ColumnIndex])
                {
                    priority = i;
                    isDescending = keys[i].IsDescending;
                    break;
                }
            }

            bool isSorted = priority >= 0;
            sortIcon.SetDisplayed(isSorted);
            if (isSorted)
                sortIcon.IconName = isDescending ? DatabaseIcons.SortDescending : DatabaseIcons.SortAscending;

            // The priority only matters when there are several sorted columns
            bool isPriorityShown = isSorted && keys.Count > 1;
            priorityLabel.SetDisplayed(isPriorityShown);
            if (isPriorityShown)
                priorityLabel.text = (priority + 1).ToString();
        }

        /// <summary>
        /// Gets the index of the column of the table described by a sort description of a list view.
        /// </summary>
        /// <param name="listView">The list view that owns the description.</param>
        /// <param name="description">The sort description.</param>
        /// <returns>Returns the index of the column in the pipeline, or -1 if it's not found.</returns>
        private int GetColumnIndex(MultiColumnListView listView, SortColumnDescription description)
        {
            // The columns of the list views are named after the identifiers of the columns of the table
            string columnId = description.column?.name;
            if (string.IsNullOrEmpty(columnId))
                columnId = description.columnName;

            if (string.IsNullOrEmpty(columnId) && description.columnIndex >= 0 && description.columnIndex < listView.columns.Count)
                columnId = listView.columns[description.columnIndex].name;

            return _columns.FindIndex(column => column.Id == columnId);
        }

        /// <summary>
        /// Compares two values of a column through its handler.
        /// </summary>
        /// <remarks>
        /// An error of the handler is logged once per column, and the values are then considered equal, so a broken custom handler
        /// doesn't prevent the table from being displayed.
        /// </remarks>
        /// <param name="column">The column of the values.</param>
        /// <param name="a">The first value.</param>
        /// <param name="b">The second value.</param>
        /// <returns>Returns the result of the handler, or 0 if it fails.</returns>
        private static int Compare(TableViewColumn column, object a, object b)
        {
            try
            {
                return column.Handler.Compare(a, b);
            }
            catch (Exception exception)
            {
                if (!column.HasLoggedError)
                {
                    column.HasLoggedError = true;
                    Debug.LogError($"{DatabaseConstants.LogPrefix}The cell handler {column.Handler.GetType()} failed to compare " +
                        $"values of the column \"{column.Name}\". The next errors of this column are not logged.");
                    Debug.LogException(exception);
                }
                return 0;
            }
        }

        #endregion

    }

}
