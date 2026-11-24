using System.Collections.Generic;

using UnityEngine.UIElements;

namespace SideXP.Database.EditorOnly
{

    /// <summary>
    /// The remembered settings of the table view: the order, the widths of the columns and the sort, stored as the user's default
    /// view of the table (see <see cref="UserViews"/>).
    /// </summary>
    /// <remarks>
    /// The list views have no public event for a reordered column, so the order is read from the headers after each click in the
    /// main list (dragging a header ends with one), and when the table is left. The widths are saved when the columns report a new
    /// width, which only happens when the user resizes them. Saves are grouped by <see cref="UserViews.ScheduleSave"/>.
    /// </remarks>
    internal partial class TableView
    {

        #region Fields

        /// <summary>
        /// The name of the property of <see cref="Column"/> reported when its width changes.
        /// </summary>
        private const string WidthPropertyName = nameof(Column.width);

        /// <summary>
        /// The remembered settings of the displayed table, or null if no table is displayed.
        /// </summary>
        private ViewSettings _viewSettings = null;

        #endregion


        #region Private API

        /// <summary>
        /// Gets the columns to display, in their remembered order.
        /// </summary>
        /// <returns>Returns the indices of the columns in the pipeline, in their display order.</returns>
        private List<int> GetDisplayedColumns()
        {
            List<string> columnIds = _columns.ConvertAll(column => column.Id);
            List<string> order = _viewSettings.GetColumnOrder(columnIds);
            return order.ConvertAll(columnId => columnIds.IndexOf(columnId));
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
        /// Sorts the pipeline with the remembered sort, if its column is displayed and can be sorted.
        /// </summary>
        private void ApplyRememberedSort()
        {
            int columnIndex = _columns.FindIndex(column => column.Id == _viewSettings.SortColumnId);
            if (columnIndex >= 0 && _columns[columnIndex].IsSortable)
                SetPipelineSort(columnIndex, _viewSettings.IsSortDescending);
            else
                _pipeline.ClearSort();
        }

        /// <summary>
        /// Shows the sort of the pipeline in the header of the list view that displays its column.
        /// </summary>
        private void ShowSortInHeader()
        {
            if (_pipeline.SortColumnIndex < 0)
                return;

            string columnId = _columns[_pipeline.SortColumnIndex].Id;
            MultiColumnListView listView = _frozenListView != null && _frozenListView.columns.Contains(columnId)
                ? _frozenListView
                : _listView;

            if (!listView.columns.Contains(columnId))
                return;

            // Changing the sort descriptions raises the event of a sort chosen by the user
            _isUpdatingSort = true;
            try
            {
                SortDirection direction = _pipeline.IsSortDescending ? SortDirection.Descending : SortDirection.Ascending;
                listView.sortColumnDescriptions.Add(new SortColumnDescription(columnId, direction));
            }
            finally
            {
                _isUpdatingSort = false;
            }
        }

        /// <summary>
        /// Remembers the sort of the pipeline.
        /// </summary>
        private void SaveSort()
        {
            string columnId = _pipeline.SortColumnIndex >= 0 ? _columns[_pipeline.SortColumnIndex].Id : null;
            if (_viewSettings.SetSort(columnId, _pipeline.IsSortDescending))
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
            _listView.Query<Label>(className: HeaderClass).ForEach(header =>
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
