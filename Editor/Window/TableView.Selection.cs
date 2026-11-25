using System;
using System.Collections.Generic;

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
    /// </list>
    /// The selection is built on the visible rows (filtered and sorted), not on the elements of the list views, so it doesn't depend
    /// on scrolling. Its cells leave it when their rows leave the table or are hidden by the search. The cells of hidden columns stay
    /// selected, since hiding a column is only a display choice.
    /// </remarks>
    internal partial class TableView
    {

        #region Fields

        private const string StatusSeparator = "    ";

        /// <summary>
        /// The selected cells. The active cell is usually one of them.
        /// </summary>
        private CellSelection _selection = new CellSelection();

        /// <summary>
        /// The selection before the last click, restored by a double-click inside it, or null if there is none.
        /// </summary>
        private CellSelection _selectionBeforeClick = null;

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
            // The user may have dragged a header since the order was last read
            SaveColumnOrder();
            List<string> columnIds = GetColumnOrder().ConvertAll(columnIndex => _columns[columnIndex].Id);
            List<Object> rowAssets = _rows.ConvertAll(row => row.Asset);
            return CellSelection.GetRange(rowAssets, columnIds, from, to);
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
