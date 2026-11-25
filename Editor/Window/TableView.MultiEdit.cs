using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UIElements;

namespace SideXP.Database.EditorOnly
{

    /// <summary>
    /// The edits that apply to several cells: an edit of a selected cell also changes the other selected cells of its column, or of
    /// the same type with Alt (see <see cref="MultiCellEdit"/>).
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item>Text and number fields: Enter applies the value to the selected cells of the column, Alt+Enter to the selected cells of the
    /// same type.</item>
    /// <item>Live controls (checkboxes and dropdowns): a click applies to the selected cells of the column, an Alt+click to the
    /// selected cells of the same type. The modifier is read when the control is pressed, since a dropdown gives its value after its
    /// menu closes.</item>
    /// </list>
    /// Each cell receives the value through its handler (<see cref="CellHandler.TryAcceptValue"/>), which can reject it. The rejected
    /// cells and the read-only cells (built-in columns) are skipped and reported. All the writes form a single Undo step.
    /// <para>
    /// While Alt is held, the selected cells that an edit of the active cell would change get an outline.
    /// </para>
    /// </remarks>
    internal partial class TableView
    {

        #region Subclasses

        /// <summary>
        /// A cell that receives the value of an edit of another cell.
        /// </summary>
        private readonly struct EditTarget
        {

            /// <summary>
            /// The row of the cell.
            /// </summary>
            public readonly TableRow Row;

            /// <summary>
            /// The column of the cell.
            /// </summary>
            public readonly TableViewColumn Column;

            /// <summary>
            /// The index of the column in the pipeline.
            /// </summary>
            public readonly int ColumnIndex;

            /// <inheritdoc cref="EditTarget"/>
            /// <param name="row"><inheritdoc cref="Row" path="/summary"/></param>
            /// <param name="column"><inheritdoc cref="Column" path="/summary"/></param>
            /// <param name="columnIndex"><inheritdoc cref="ColumnIndex" path="/summary"/></param>
            public EditTarget(TableRow row, TableViewColumn column, int columnIndex)
            {
                Row = row;
                Column = column;
                ColumnIndex = columnIndex;
            }

        }

        #endregion


        #region Fields

        private const string AltTargetCellClass = "database-table-view__cell--alt-target";

        /// <summary>
        /// Checks if Alt is held, as reported by the last key or pointer event of the view.
        /// </summary>
        private bool _isAltHeld = false;

        /// <summary>
        /// Checks if Alt was held when a cell was last pressed, which applies the value of a live control to the cells of the same type.
        /// </summary>
        private bool _isAltHeldOnPress = false;

        /// <summary>
        /// The type of the values that an edit of the active cell gives to the other selected cells with Alt, or null if Alt is not held
        /// or if the active cell can't give its value (see <see cref="UpdateAltTargetType"/>).
        /// </summary>
        private Type _altTargetType = null;

        #endregion


        #region Edit targets

        /// <summary>
        /// Gets the other cells that receive the value of an edit.
        /// </summary>
        /// <param name="editedRow">The row of the edited cell.</param>
        /// <param name="editedColumn">The column of the edited cell.</param>
        /// <param name="isSameTypeEdit">If enabled (Alt held), the edit applies to the selected cells of the same type in any column.
        /// Otherwise, it stays in the column of the edited cell.</param>
        /// <returns>Returns the cells that receive the value, without the edited cell. Empty if the edited cell is not selected.</returns>
        private List<EditTarget> GetEditTargets(TableRow editedRow, TableViewColumn editedColumn, bool isSameTypeEdit)
        {
            List<EditTarget> targets = new List<EditTarget>();
            CellId editedCell = new CellId(editedRow.Asset, editedColumn.Id);
            if (_pipeline == null || !_selection.Contains(editedCell))
                return targets;

            Dictionary<string, int> columnIndices = new Dictionary<string, int>(_columns.Count);
            for (int i = 0; i < _columns.Count; i++)
                columnIndices[_columns[i].Id] = i;

            Type editedType = editedColumn.Accessor.ValueType;
            foreach (CellId cell in _selection.Cells)
            {
                if (cell.Equals(editedCell) || !columnIndices.TryGetValue(cell.ColumnId, out int columnIndex))
                    continue;

                TableViewColumn column = _columns[columnIndex];
                // The Controls column has no value to receive
                if (column.IsControls)
                    continue;

                if (!MultiCellEdit.IsTarget(editedColumn.Id, editedType, column.Id, column.Accessor.ValueType, isSameTypeEdit))
                    continue;

                if (cell.Asset != null && _pipeline.TryGetRow(cell.Asset, out TableRow row))
                    targets.Add(new EditTarget(row, column, columnIndex));
            }

            return targets;
        }

        /// <summary>
        /// Checks if the cells that an edit would change, without Alt, have values that differ from the value of the edited cell.
        /// </summary>
        /// <param name="editedRow">The row of the edited cell.</param>
        /// <param name="editedColumn">The column of the edited cell.</param>
        /// <param name="editedValue">The value of the edited cell.</param>
        /// <returns>Returns true if the edit field must show the mixed value indicator.</returns>
        private bool IsEditMixed(TableRow editedRow, TableViewColumn editedColumn, object editedValue)
        {
            List<EditTarget> targets = GetEditTargets(editedRow, editedColumn, false);
            List<object> values = targets.ConvertAll(target => target.Row.GetValue(target.ColumnIndex));
            return MultiCellEdit.AreMixed(editedValue, values);
        }

        /// <summary>
        /// Writes the value of an edit to other cells, through their handlers, in the current Undo group. Reports the cells that are
        /// not changed.
        /// </summary>
        /// <remarks>
        /// Call it after the edited cell is written, and before its Undo group is collapsed, so the writes form a single Undo step.
        /// </remarks>
        /// <param name="targets">The cells that receive the value (see <see cref="GetEditTargets"/>).</param>
        /// <param name="value">The value of the edited cell.</param>
        /// <param name="editedColumn">The column of the edited cell, named in the report.</param>
        private void ApplyToTargets(List<EditTarget> targets, object value, TableViewColumn editedColumn)
        {
            int readOnlyCount = 0;
            int rejectedCount = 0;
            foreach (EditTarget target in targets)
            {
                // Deleted during the edit: there is nothing to write
                if (target.Row.Asset == null)
                    continue;

                // Built-in columns, such as the asset name, which can't be renamed in several assets at once
                if (!target.Column.HasProperty)
                {
                    readOnlyCount++;
                    continue;
                }

                if (!TryAcceptValue(target, value, out object acceptedValue))
                {
                    rejectedCount++;
                    continue;
                }

                try
                {
                    CellEditSession.WriteValue(target.Row.Asset, target.Column.PropertyPath, acceptedValue);
                }
                catch (Exception exception)
                {
                    rejectedCount++;
                    Debug.LogWarning($"{DatabaseConstants.LogPrefix}The value {TextConversion.ToText(acceptedValue)} can't be written " +
                        $"to the column \"{target.Column.Name}\" of \"{target.Row.Asset.name}\": {exception.Message}");
                }

                _pipeline.InvalidateRow(target.Row.Asset);
            }

            int skippedCount = readOnlyCount + rejectedCount;
            if (skippedCount == 0)
                return;

            List<string> reasons = new List<string>(2);
            if (rejectedCount > 0)
                reasons.Add($"{rejectedCount} rejected the value");

            if (readOnlyCount > 0)
                reasons.Add($"{readOnlyCount} can't be edited");

            string cellsText = targets.Count == 1 ? "cell" : "cells";
            Debug.LogWarning($"{DatabaseConstants.LogPrefix}The edit of \"{editedColumn.Name}\" left {skippedCount} of the " +
                $"{targets.Count} other selected {cellsText} unchanged: {string.Join(", ", reasons)}.");
        }

        /// <summary>
        /// Asks the handler of a cell to accept the value of an edit of another cell.
        /// </summary>
        /// <remarks>
        /// An error of the handler is logged once per column, and the cell is then skipped, so a broken custom handler doesn't stop the
        /// edit of the other cells.
        /// </remarks>
        /// <param name="target">The cell that receives the value.</param>
        /// <param name="value">The value of the edited cell.</param>
        /// <param name="acceptedValue">Outputs the value to write to the cell.</param>
        /// <returns>Returns true if the handler accepts the value.</returns>
        private static bool TryAcceptValue(EditTarget target, object value, out object acceptedValue)
        {
            TableViewColumn column = target.Column;
            try
            {
                CellContext context = new CellContext(target.Row.Asset, column.Field, column.Accessor.ValueType);
                return column.Handler.TryAcceptValue(value, context, out acceptedValue);
            }
            catch (Exception exception)
            {
                if (!column.HasLoggedError)
                {
                    column.HasLoggedError = true;
                    Debug.LogError($"{DatabaseConstants.LogPrefix}The cell handler {column.Handler.GetType()} failed to accept a value " +
                        $"of the column \"{column.Name}\". The next errors of this column are not logged.");
                    Debug.LogException(exception);
                }

                acceptedValue = null;
                return false;
            }
        }

        #endregion


        #region Modifier feedback

        /// <summary>
        /// Called when a cell is pressed, before its children (live controls included) get the event. Remembers if Alt is held, for the
        /// live control that may submit a value after this press.
        /// </summary>
        /// <param name="evt">The emitted event.</param>
        private void OnCellPointerDownTrickleDown(PointerDownEvent evt)
        {
            _isAltHeldOnPress = evt.altKey;
            SetAltHeld(evt.altKey);
        }

        /// <summary>
        /// Called when the pointer moves over the view, even if an element of the view captures the pointer. Follows the state of Alt,
        /// which can change while the view doesn't have the focus, and the drag selection in progress.
        /// </summary>
        /// <param name="evt">The emitted event.</param>
        private void OnPointerMoveTrickleDown(PointerMoveEvent evt)
        {
            SetAltHeld(evt.altKey);
            UpdateDragSelection(evt.position, evt.pressedButtons);
        }

        /// <summary>
        /// Called when a key is released while the focus is in the view. Follows the state of Alt.
        /// </summary>
        /// <param name="evt">The emitted event.</param>
        private void OnKeyUp(KeyUpEvent evt)
        {
            bool isAltReleased = evt.keyCode == KeyCode.LeftAlt || evt.keyCode == KeyCode.RightAlt;
            SetAltHeld(evt.altKey && !isAltReleased);
        }

        /// <summary>
        /// Updates the state of Alt, and the outline of the cells that an edit with Alt would change.
        /// </summary>
        /// <param name="isHeld">Checks if Alt is held.</param>
        private void SetAltHeld(bool isHeld)
        {
            if (isHeld == _isAltHeld)
                return;

            _isAltHeld = isHeld;
            UpdateCellHighlights();
        }

        /// <summary>
        /// Updates the type of the values that an edit of the active cell would give to the other selected cells with Alt. Called
        /// before the highlights of the cells are updated.
        /// </summary>
        private void UpdateAltTargetType()
        {
            _altTargetType = null;
            // An edit of a cell that is not selected only changes that cell
            if (!_isAltHeld || _activeCell.IsNone || !_selection.Contains(_activeCell))
                return;

            TableViewColumn column = _columns.Find(candidate => candidate.Id == _activeCell.ColumnId);
            if (column != null && column.HasProperty)
                _altTargetType = column.Accessor.ValueType;
        }

        /// <summary>
        /// Checks if a selected cell would be changed by an edit of the active cell with Alt, so it gets an outline while Alt is held.
        /// </summary>
        /// <param name="column">The column of the cell.</param>
        /// <param name="isSelected">Checks if the cell is selected.</param>
        /// <param name="isActive">Checks if the cell is the active cell, which has its own outline.</param>
        /// <returns>Returns true if the cell gets the outline.</returns>
        private bool IsAltTarget(TableViewColumn column, bool isSelected, bool isActive)
        {
            return _altTargetType != null
                && isSelected
                && !isActive
                && column.HasProperty
                && column.Accessor.ValueType == _altTargetType;
        }

        #endregion

    }

}
