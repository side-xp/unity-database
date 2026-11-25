using System;
using System.Collections.Generic;
using System.Globalization;

using UnityEngine.UIElements;

using Object = UnityEngine.Object;

namespace SideXP.Database.EditorOnly
{

    /// <summary>
    /// The cell selection of the table view: any set of cells, built with clicks, and highlighted in both lists.
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item>A click selects a single cell, which becomes the active cell.</item>
    /// <item>Shift-click selects the range between the active cell and the clicked cell. The active cell stays where it is, so it's
    /// the anchor of the next ranges.</item>
    /// <item>Ctrl-click (Command-click on macOS) selects the clicked cell, or deselects it, without changing the other cells.</item>
    /// <item>Ctrl-Shift-click adds a range to the selection.</item>
    /// <item>A double-click edits the clicked cell, and keeps the selection that existed before its first click if the cell was part of
    /// it.</item>
    /// <item>The handle at the start of a row selects the whole row, with Shift and Ctrl like the cells.</item>
    /// <item>Shift-click on a column header selects the whole column. Ctrl-click, or the context menu of the header, adds the column to
    /// the selection or removes it. A click without these keys sorts the column.</item>
    /// </list>
    /// The selection is built on the visible rows (filtered and sorted), not on the elements of the list views, so it doesn't depend
    /// on scrolling. Its cells leave it when their rows leave the table or are hidden by the search. The cells of hidden columns stay
    /// selected, since hiding a column is only a display choice: whole rows include them too.
    /// </remarks>
    internal partial class TableView
    {

        #region Fields

        private const string RowHandleContainerClass = "database-table-view__row-handle-container";
        private const string RowHandleClass = "database-table-view__row-handle";
        private const string RowNumberClass = "database-table-view__row-number";

        /// <summary>
        /// The minimum number of digits the row handles have room for, so their width doesn't change for most tables.
        /// </summary>
        private const int MinRowNumberDigits = 3;

        /// <summary>
        /// The width of a digit of the row numbers, in pixels, at the font size of the style sheet, rounded up.
        /// </summary>
        private const float RowNumberDigitWidth = 6;

        /// <summary>
        /// The horizontal space around the row numbers in their handle, in pixels, both sides together.
        /// </summary>
        private const float RowHandlePadding = 6;

        private const string RowHandleTooltip = "Selects the row. Shift selects the rows from the active cell, Ctrl adds or removes the row.";
        private const string AddColumnText = "Add Column to Selection";
        private const string RemoveColumnText = "Remove Column from Selection";

        private const string StatusSeparator = "    ";

        /// <summary>
        /// The selected cells. The active cell is usually one of them.
        /// </summary>
        private CellSelection _selection = new CellSelection();

        /// <summary>
        /// The selection before the last click, restored by a double-click inside it, or null if there is none.
        /// </summary>
        private CellSelection _selectionBeforeClick = null;

        /// <summary>
        /// The width of the row handles, in pixels, so the largest row number fits (see <see cref="UpdateRowHandleWidth"/>).
        /// </summary>
        private float _rowHandleWidth = (MinRowNumberDigits * RowNumberDigitWidth) + RowHandlePadding;

        #endregion


        #region Public API

        /// <summary>
        /// The selected cells, in no particular order.
        /// </summary>
        public IReadOnlyCollection<CellId> SelectedCells => _selection.Cells;

        #endregion


        #region Building the selection

        /// <summary>
        /// Changes the selection after a click on a cell, and moves the active cell if needed.
        /// </summary>
        /// <param name="cell">The clicked cell.</param>
        /// <param name="isAdding">If enabled (Ctrl or Command held), the clicked cell or range is added to the selection instead of
        /// replacing it.</param>
        /// <param name="isRange">If enabled (Shift held), the range between the active cell and the clicked cell is selected.</param>
        private void ClickCell(CellId cell, bool isAdding, bool isRange)
        {
            _selectionBeforeClick = _selection.Clone();

            // Without an active cell, a range has no start: the click selects the cell like a click without Shift
            if (isRange && !_activeCell.IsNone)
            {
                _selection.SelectRange(GetRange(_activeCell, cell), isAdding);
            }
            else if (isAdding)
            {
                if (_selection.Toggle(cell))
                    SetActiveCell(cell);
                // The active cell is the one that gets edited: it can't stay on a cell that is not selected anymore
                else if (_activeCell.Equals(cell))
                    SetActiveCell(CellId.None);
            }
            else
            {
                _selection.Select(cell);
                SetActiveCell(cell);
            }

            OnSelectionChanged();
        }

        /// <summary>
        /// Selects a single cell, and makes it the active cell.
        /// </summary>
        /// <param name="cell">The cell to select.</param>
        private void SelectCell(CellId cell)
        {
            _selection.Select(cell);
            SetActiveCell(cell);
            OnSelectionChanged();
        }

        /// <summary>
        /// Called on the second click of a double-click, before the clicked cell is edited. Restores the selection from before the
        /// first click if it contained the cell, since the first click replaced it, and makes the cell the active cell.
        /// </summary>
        /// <param name="cell">The double-clicked cell.</param>
        private void RestoreSelectionBeforeClick(CellId cell)
        {
            if (_selectionBeforeClick != null && _selectionBeforeClick.Contains(cell))
                _selection.CopyFrom(_selectionBeforeClick);
            // The edited cell must be selected, for example when the first click deselected it with Ctrl
            else if (!_selection.Contains(cell))
                _selection.Select(cell);

            _selectionBeforeClick = null;
            SetActiveCell(cell);
            OnSelectionChanged();
        }

        /// <summary>
        /// Deselects all the cells, and clears the active cell.
        /// </summary>
        private void ClearSelection()
        {
            _selection.Clear();
            _selectionBeforeClick = null;
            _activeCell = CellId.None;
            _activeCellElement = null;
        }

        /// <summary>
        /// Deselects the cells of the rows that are not visible anymore: assets deleted, out of the table, or hidden by the search.
        /// </summary>
        private void RemoveHiddenRowsFromSelection()
        {
            if (_selection.IsEmpty && _selectionBeforeClick == null)
                return;

            HashSet<Object> visibleAssets = new HashSet<Object>();
            foreach (TableRow row in _rows)
                visibleAssets.Add(row.Asset);

            Predicate<CellId> isHidden = cell => cell.Asset == null || !visibleAssets.Contains(cell.Asset);
            _selectionBeforeClick?.RemoveAll(isHidden);
            if (_selection.RemoveAll(isHidden) > 0)
                UpdateCellHighlights();
        }

        /// <summary>
        /// Gets the cells of the range between two cells, in the order of the visible rows and of the columns.
        /// </summary>
        /// <param name="from">The cell where the range starts.</param>
        /// <param name="to">The cell where the range ends.</param>
        /// <returns>Returns the cells of the range (see <see cref="CellSelection.GetRange"/>).</returns>
        private List<CellId> GetRange(CellId from, CellId to)
        {
            return CellSelection.GetRange(GetRowAssets(), GetColumnIdsInOrder(), from, to);
        }

        /// <summary>
        /// Gets the assets of the visible rows, in their display order.
        /// </summary>
        /// <returns>Returns the assets of the rows.</returns>
        private List<Object> GetRowAssets()
        {
            return _rows.ConvertAll(row => row.Asset);
        }

        /// <summary>
        /// Gets the identifiers of all the columns, hidden ones included, in their display order.
        /// </summary>
        /// <returns>Returns the identifiers of the columns.</returns>
        private List<string> GetColumnIdsInOrder()
        {
            // The user may have dragged a header since the order was last read
            SaveColumnOrder();
            return GetColumnOrder().ConvertAll(columnIndex => _columns[columnIndex].Id);
        }

        #endregion


        #region Rows

        /// <summary>
        /// Wraps the element of a cell of the first displayed column in a container that starts with the handle of its row, which shows
        /// the number of the row.
        /// </summary>
        /// <remarks>
        /// The container and the handle hold the element of the cell in their <see cref="VisualElement.userData"/>, so the list views
        /// can bind the cell (see <see cref="GetCellElement(VisualElement)"/>) and the handle knows its row.
        /// </remarks>
        /// <param name="cell">The element created by <see cref="MakeCell(TableViewColumn, bool)"/>.</param>
        /// <returns>Returns the container, which the list view displays instead of the cell.</returns>
        private VisualElement WrapWithRowHandle(VisualElement cell)
        {
            VisualElement container = new VisualElement { userData = cell };
            container.AddToClassList(RowHandleContainerClass);

            VisualElement handle = new VisualElement { userData = cell, tooltip = RowHandleTooltip };
            handle.AddToClassList(RowHandleClass);
            handle.style.width = _rowHandleWidth;
            handle.RegisterCallback<PointerDownEvent>(OnRowHandlePointerDown);

            // The handle is the target of the clicks and of the tooltip, not its number
            Label number = new Label { pickingMode = PickingMode.Ignore };
            number.AddToClassList(RowNumberClass);
            handle.Add(number);

            container.Add(handle);
            container.Add(cell);
            return container;
        }

        /// <summary>
        /// Shows the number of a row in its handle, and gives the handle the width of the largest number.
        /// </summary>
        /// <param name="container">The container created by <see cref="WrapWithRowHandle(VisualElement)"/>.</param>
        /// <param name="rowIndex">The index of the row in the visible rows.</param>
        private void BindRowHandle(VisualElement container, int rowIndex)
        {
            VisualElement handle = container[0];
            handle.style.width = _rowHandleWidth;
            if (handle[0] is Label number)
                number.text = (rowIndex + 1).ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Updates the width of the row handles from the number of visible rows, so the largest row number fits. The handles get it
        /// when they are bound.
        /// </summary>
        private void UpdateRowHandleWidth()
        {
            int digitCount = Math.Max(MinRowNumberDigits, _rows.Count.ToString(CultureInfo.InvariantCulture).Length);
            _rowHandleWidth = (digitCount * RowNumberDigitWidth) + RowHandlePadding;
        }

        /// <summary>
        /// Gets the element of a cell from an element of a list view.
        /// </summary>
        /// <param name="element">The element created by the list view for the cell: the cell itself, or the container of its row handle
        /// (see <see cref="WrapWithRowHandle(VisualElement)"/>).</param>
        /// <returns>Returns the element created by <see cref="MakeCell(TableViewColumn, bool)"/>.</returns>
        private static VisualElement GetCellElement(VisualElement element)
        {
            return element.userData is VisualElement cell ? cell : element;
        }

        /// <summary>
        /// Called when the handle of a row is clicked. Selects the row (see <see cref="ClickRow"/>).
        /// </summary>
        /// <param name="evt">The emitted event.</param>
        private void OnRowHandlePointerDown(PointerDownEvent evt)
        {
            if (evt.button != (int)MouseButton.LeftMouse)
                return;

            if (evt.currentTarget is not VisualElement handle || handle.userData is not VisualElement cell
                || cell.userData is not CellBinding binding || binding.Row == null)
            {
                return;
            }

            // Like a click on a cell: the view handles the focus itself
            handle.panel?.focusController?.IgnoreEvent(evt);

            // Read before the edit ends, since ending it refreshes the rows and can rebind the handle to another row
            Object asset = binding.Row.Asset;
            EndEdit(true, false);
            Focus();
            ClickRow(asset, evt.actionKey, evt.shiftKey);
        }

        /// <summary>
        /// Changes the selection after a click on the handle of a row. The active cell moves to the first displayed column of the row,
        /// except for a range, which starts from the active cell.
        /// </summary>
        /// <param name="asset">The asset of the clicked row.</param>
        /// <param name="isAdding">If enabled (Ctrl or Command held), the row or the range of rows is added to the selection, or the row is
        /// removed from it if it's already selected.</param>
        /// <param name="isRange">If enabled (Shift held), the rows between the row of the active cell and the clicked row are selected.
        /// </param>
        private void ClickRow(Object asset, bool isAdding, bool isRange)
        {
            List<string> columnIds = GetColumnIdsInOrder();
            if (columnIds.Count == 0 || _displayedColumns.Count == 0)
                return;

            _selectionBeforeClick = _selection.Clone();
            List<Object> rowAssets = GetRowAssets();
            string firstColumnId = columnIds[0];
            string lastColumnId = columnIds[columnIds.Count - 1];
            CellId rowEnd = new CellId(asset, lastColumnId);

            if (isRange && !_activeCell.IsNone)
            {
                CellId rangeStart = new CellId(_activeCell.Asset, firstColumnId);
                _selection.SelectRange(CellSelection.GetRange(rowAssets, columnIds, rangeStart, rowEnd), isAdding);
                OnSelectionChanged();
                return;
            }

            List<CellId> rowCells = CellSelection.GetRange(rowAssets, columnIds, new CellId(asset, firstColumnId), rowEnd);
            CellId rowActiveCell = new CellId(asset, _columns[_displayedColumns[0]].Id);
            if (isAdding && _selection.ContainsAll(rowCells))
            {
                _selection.Remove(rowCells);
                // The active cell can't stay on a cell that is not selected anymore
                if (!_activeCell.IsNone && ReferenceEquals(_activeCell.Asset, asset))
                    SetActiveCell(CellId.None);
            }
            else
            {
                if (isAdding)
                    _selection.Add(rowCells);
                else
                    _selection.Select(rowCells);

                SetActiveCell(rowActiveCell);
            }

            OnSelectionChanged();
        }

        #endregion


        #region Columns

        /// <summary>
        /// Called when the header of a column is clicked. Shift-click selects the column, Ctrl-click (Command-click on macOS) adds it to
        /// the selection or removes it. A click without these keys is left to the list view, which sorts the column.
        /// </summary>
        /// <remarks>
        /// The event is stopped so the header column of the list view doesn't sort nor drag the column: its click is handled by a
        /// manipulator on a parent of this header.
        /// </remarks>
        /// <param name="evt">The emitted event.</param>
        private void OnHeaderPointerDown(PointerDownEvent evt)
        {
            if (evt.button != (int)MouseButton.LeftMouse || !IsColumnSelectionClick(evt.actionKey, evt.shiftKey))
                return;

            if (evt.currentTarget is not VisualElement header || header.userData is not TableViewColumn column)
                return;

            evt.StopPropagation();
            header.panel?.focusController?.IgnoreEvent(evt);
            EndEdit(true, false);
            Focus();

            if (evt.actionKey)
                ToggleColumnInSelection(column.Id);
            else
                SelectColumn(column.Id);
        }

        /// <summary>
        /// Called when the header of a column receives the mouse event sent after its pointer event, for compatibility. Stops it on the
        /// clicks that select the column, like the pointer event (see <see cref="OnHeaderPointerDown(PointerDownEvent)"/>), since the
        /// manipulators of the list view also listen to mouse events.
        /// </summary>
        /// <param name="evt">The emitted event.</param>
        private void OnHeaderMouseDown(MouseDownEvent evt)
        {
            if (evt.button == (int)MouseButton.LeftMouse && IsColumnSelectionClick(evt.actionKey, evt.shiftKey))
                evt.StopPropagation();
        }

        /// <summary>
        /// Checks if a click on a column header selects the column instead of sorting it.
        /// </summary>
        /// <param name="isActionKeyHeld">Checks if Ctrl (Command on macOS) is held.</param>
        /// <param name="isShiftHeld">Checks if Shift is held.</param>
        /// <returns>Returns true if one of the keys is held.</returns>
        private static bool IsColumnSelectionClick(bool isActionKeyHeld, bool isShiftHeld)
        {
            return isActionKeyHeld || isShiftHeld;
        }

        /// <summary>
        /// Selects all the cells of a column, in the visible rows, and deselects the others. The active cell moves to the first row.
        /// </summary>
        /// <param name="columnId">The identifier of the column.</param>
        private void SelectColumn(string columnId)
        {
            _selectionBeforeClick = null;
            List<CellId> columnCells = GetColumnCells(columnId);
            _selection.Select(columnCells);
            SetActiveCell(columnCells.Count > 0 ? columnCells[0] : CellId.None);
            OnSelectionChanged();
        }

        /// <summary>
        /// Adds all the cells of a column to the selection, or removes them if they are all selected already. The active cell moves to
        /// the first row of an added column.
        /// </summary>
        /// <param name="columnId">The identifier of the column.</param>
        private void ToggleColumnInSelection(string columnId)
        {
            _selectionBeforeClick = null;
            List<CellId> columnCells = GetColumnCells(columnId);
            if (_selection.ContainsAll(columnCells))
            {
                _selection.Remove(columnCells);
                if (!_activeCell.IsNone && _activeCell.ColumnId == columnId)
                    SetActiveCell(CellId.None);
            }
            else
            {
                _selection.Add(columnCells);
                SetActiveCell(columnCells[0]);
            }

            OnSelectionChanged();
        }

        /// <summary>
        /// Adds the item that adds the clicked column to the selection, or removes it, to the context menu of the headers.
        /// </summary>
        /// <param name="menu">The context menu of the headers.</param>
        /// <param name="clickedColumn">The column of the list view under the pointer, or null if there is none.</param>
        /// <returns>Returns true if an item has been added.</returns>
        private bool AppendColumnSelectionItem(DropdownMenu menu, Column clickedColumn)
        {
            // The columns of the list views are named after the identifiers of the columns of the table
            string columnId = clickedColumn?.name;
            if (string.IsNullOrEmpty(columnId) || !_columns.Exists(column => column.Id == columnId))
                return false;

            // Without rows, a column has no cell to select
            if (_rows.Count == 0)
            {
                menu.AppendAction(AddColumnText, action => { }, DropdownMenuAction.Status.Disabled);
                return true;
            }

            string text = _selection.ContainsAll(GetColumnCells(columnId)) ? RemoveColumnText : AddColumnText;
            menu.AppendAction(text, action => ToggleColumnInSelection(columnId), DropdownMenuAction.Status.Normal);
            return true;
        }

        /// <summary>
        /// Gets the cells of a column, in the visible rows.
        /// </summary>
        /// <param name="columnId">The identifier of the column.</param>
        /// <returns>Returns the cells of the column, in the order of the rows.</returns>
        private List<CellId> GetColumnCells(string columnId)
        {
            return _rows.ConvertAll(row => new CellId(row.Asset, columnId));
        }

        #endregion


        #region Feedback

        /// <summary>
        /// Updates what shows the selection: the highlight of the cells and the status line.
        /// </summary>
        private void OnSelectionChanged()
        {
            UpdateCellHighlights();
            if (_pipeline != null)
                _statusLabel.text = GetStatusText();
        }

        /// <summary>
        /// Updates the highlight of all the cell elements of both lists. The elements that are not visible get their highlight when
        /// they are bound.
        /// </summary>
        private void UpdateCellHighlights()
        {
            UpdateAltTargetType();
            _body.Query(className: CellClass).ForEach(UpdateCellHighlight);
        }

        /// <summary>
        /// Gets the text of the selection in the status line: the number of selected cells and of their assets.
        /// </summary>
        /// <returns>Returns the text, or an empty string if there are less than two selected cells (a single cell is the active cell,
        /// which has its own highlight).</returns>
        private string GetSelectionStatusText()
        {
            if (_selection.Count < 2)
                return string.Empty;

            int assetCount = _selection.AssetCount;
            string assetsText = assetCount == 1 ? "asset" : "assets";
            return $"{StatusSeparator}{_selection.Count} cells selected ({assetCount} {assetsText})";
        }

        #endregion

    }

}
