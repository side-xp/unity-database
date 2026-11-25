using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;

using Object = UnityEngine.Object;

namespace SideXP.Database.EditorOnly
{

    /// <summary>
    /// The copy and paste of the table view: Ctrl+C and Ctrl+V (Command on macOS) copy and paste the selected cells as tab-separated
    /// text, compatible with spreadsheet apps.
    /// </summary>
    /// <remarks>
    /// The values go through the text conversion of the cell handlers (<see cref="CellHandler.ToText(object)"/> and
    /// <see cref="CellHandler.TryParse(string, CellContext, out object)"/>). The rules of the selections that can be copied and of the
    /// cells that receive the pasted values are in <see cref="CellClipboard"/>. The <i>Controls</i> column has no value, so it's never
    /// copied nor pasted. A paste is a single Undo step.
    /// <para>
    /// The keys reach the view as the <c>Copy</c> and <c>Paste</c> commands of the editor, and only while the view has the focus: an
    /// edit field handles them itself.
    /// </para>
    /// </remarks>
    internal partial class TableView
    {

        #region Fields

        private const string CopyCommandName = "Copy";
        private const string PasteCommandName = "Paste";
        private const string PasteUndoName = "Paste Cells";

        #endregion


        #region Public API

        /// <summary>
        /// Copies the selected cells to the clipboard, as tab-separated text. Refused with a message if the selection doesn't form a
        /// grid (see <see cref="CellClipboard.TryGetGrid"/>).
        /// </summary>
        /// <returns>Returns true if the cells have been copied.</returns>
        public bool CopySelection()
        {
            if (_pipeline == null || _selection.IsEmpty)
                return false;

            if (!CellClipboard.TryGetGrid(_selection.Cells, GetRowAssets(), GetValueColumnIds(), out List<Object> gridRows,
                out List<string> gridColumns))
            {
                ShowMessage(CellClipboard.NotGridMessage);
                return false;
            }

            List<int> columnIndices = gridColumns.ConvertAll(columnId => _columns.FindIndex(column => column.Id == columnId));
            List<List<string>> values = new List<List<string>>(gridRows.Count);
            foreach (Object asset in gridRows)
            {
                if (!_pipeline.TryGetRow(asset, out TableRow row))
                    continue;

                List<string> rowValues = new List<string>(columnIndices.Count);
                foreach (int columnIndex in columnIndices)
                    rowValues.Add(ToCellText(_columns[columnIndex], row.GetValue(columnIndex), "to copy it") ?? string.Empty);

                values.Add(rowValues);
            }

            EditorGUIUtility.systemCopyBuffer = DelimitedText.Write(values, DelimitedText.Tab);
            return true;
        }

        /// <summary>
        /// Pastes the tab-separated text of the clipboard into the selected cells, as a single Undo step. Refused with a message if
        /// the values don't match the selection (see <see cref="CellClipboard.TryGetPasteTargets"/>).
        /// </summary>
        /// <remarks>
        /// Each cell converts its text with the handler of its column. The texts that can't be converted, and the cells that can't be
        /// edited (the <i>Asset Name</i>), are skipped and reported. When a grid is pasted from a single cell, the pasted cells become
        /// the selection.
        /// </remarks>
        /// <returns>Returns true if the values have been pasted, even if some cells have been skipped.</returns>
        public bool PasteClipboard()
        {
            if (_pipeline == null)
                return false;

            List<List<string>> values = DelimitedText.Parse(EditorGUIUtility.systemCopyBuffer, DelimitedText.Tab);
            if (!CellClipboard.TryGetPasteTargets(values, _selection.Cells, GetRowAssets(), GetValueColumnIds(),
                out List<CellClipboard.PasteTarget> targets, out string error))
            {
                ShowMessage(error);
                return false;
            }

            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName(PasteUndoName);
            int undoGroup = Undo.GetCurrentGroup();

            int readOnlyCount = 0;
            int rejectedCount = 0;
            foreach (CellClipboard.PasteTarget target in targets)
            {
                int columnIndex = _columns.FindIndex(column => column.Id == target.Cell.ColumnId);
                if (columnIndex < 0 || !_pipeline.TryGetRow(target.Cell.Asset, out TableRow row) || row.Asset == null)
                    continue;

                TableViewColumn column = _columns[columnIndex];
                if (!column.HasProperty)
                {
                    readOnlyCount++;
                    continue;
                }

                if (!TryParsePastedText(row, column, target.Text, out object value))
                {
                    rejectedCount++;
                    continue;
                }

                try
                {
                    CellEditSession.WriteValue(row.Asset, column.PropertyPath, value);
                }
                catch (Exception exception)
                {
                    rejectedCount++;
                    Debug.LogWarning($"{DatabaseConstants.LogPrefix}The value {TextConversion.ToText(value)} can't be written to the " +
                        $"column \"{column.Name}\" of \"{row.Asset.name}\": {exception.Message}");
                }

                _pipeline.InvalidateRow(row.Asset);
            }

            Undo.CollapseUndoOperations(undoGroup);
            string cellsText = targets.Count == 1 ? "cell" : "cells";
            ReportUnchangedCells("The paste", $"the {targets.Count} pasted {cellsText}", rejectedCount, "can't convert their text",
                readOnlyCount);

            // The pasted grid becomes the selection, like in spreadsheet apps. The active cell stays on its first cell
            if (_selection.Count == 1 && targets.Count > 1)
            {
                _selection.Select(targets.ConvertAll(target => target.Cell));
                OnSelectionChanged();
            }

            Refresh();
            return true;
        }

        #endregion


        #region Commands

        /// <summary>
        /// Called when the editor asks if the view handles a command. Accepts the copy and the paste, unless a cell is edited.
        /// </summary>
        /// <param name="evt">The emitted event.</param>
        private void OnValidateCommand(ValidateCommandEvent evt)
        {
            if (IsClipboardCommand(evt.commandName))
                evt.StopPropagation();
        }

        /// <summary>
        /// Called when the editor sends a command to the view. Copies or pastes the selected cells, unless a cell is edited.
        /// </summary>
        /// <param name="evt">The emitted event.</param>
        private void OnExecuteCommand(ExecuteCommandEvent evt)
        {
            if (!IsClipboardCommand(evt.commandName))
                return;

            if (evt.commandName == CopyCommandName)
                CopySelection();
            else
                PasteClipboard();

            evt.StopPropagation();
        }

        /// <summary>
        /// Checks if a command is a copy or a paste that the view handles.
        /// </summary>
        /// <param name="commandName">The name of the command.</param>
        /// <returns>Returns true if the command is handled by the view, false if a cell is edited (the edit field handles it) or if no
        /// table is displayed.</returns>
        private bool IsClipboardCommand(string commandName)
        {
            return _edit == null
                && _pipeline != null
                && (commandName == CopyCommandName || commandName == PasteCommandName);
        }

        #endregion


        #region Private API

        /// <summary>
        /// Gets the identifiers of the columns that hold values, hidden ones included, in their display order: all the columns except
        /// <i>Controls</i>.
        /// </summary>
        /// <returns>Returns the identifiers of the columns.</returns>
        private List<string> GetValueColumnIds()
        {
            List<string> columnIds = GetColumnIdsInOrder();
            columnIds.Remove(TableViewColumn.ControlsId);
            return columnIds;
        }

        /// <summary>
        /// Converts a pasted text to the value of a cell, through the handler of its column.
        /// </summary>
        /// <remarks>
        /// An error of the handler is logged once per column, and the cell is then skipped.
        /// </remarks>
        /// <param name="row">The row of the cell.</param>
        /// <param name="column">The column of the cell.</param>
        /// <param name="text">The pasted text.</param>
        /// <param name="value">Outputs the converted value.</param>
        /// <returns>Returns true if the text has been converted.</returns>
        private static bool TryParsePastedText(TableRow row, TableViewColumn column, string text, out object value)
        {
            try
            {
                CellContext context = new CellContext(row.Asset, column.Field, column.Accessor.ValueType);
                return column.Handler.TryParse(text, context, out value);
            }
            catch (Exception exception)
            {
                if (!column.HasLoggedError)
                {
                    column.HasLoggedError = true;
                    Debug.LogError($"{DatabaseConstants.LogPrefix}The cell handler {column.Handler.GetType()} failed to convert a " +
                        $"pasted text for the column \"{column.Name}\". The next errors of this column are not logged.");
                    Debug.LogException(exception);
                }

                value = null;
                return false;
            }
        }

        /// <summary>
        /// Converts the value of a cell to text through the handler of its column.
        /// </summary>
        /// <remarks>
        /// An error of the handler is logged once per column, so a broken custom handler doesn't break the table.
        /// </remarks>
        /// <param name="column">The column of the cell.</param>
        /// <param name="value">The cached value of the cell.</param>
        /// <param name="purpose">Why the value is converted, at the end of the error message (for example "for the search").</param>
        /// <returns>Returns the text of the cell, or null if the handler failed.</returns>
        private static string ToCellText(TableViewColumn column, object value, string purpose)
        {
            try
            {
                return column.Handler.ToText(value);
            }
            catch (Exception exception)
            {
                if (!column.HasLoggedError)
                {
                    column.HasLoggedError = true;
                    Debug.LogError($"{DatabaseConstants.LogPrefix}The cell handler {column.Handler.GetType()} failed to convert a " +
                        $"value of the column \"{column.Name}\" to text {purpose}. The next errors of this column are not logged.");
                    Debug.LogException(exception);
                }
                return null;
            }
        }

        /// <summary>
        /// Shows the reason why an operation is refused: as a notification in the Database window if it has the focus, and in the
        /// console.
        /// </summary>
        /// <param name="message">The message to show.</param>
        private void ShowMessage(string message)
        {
            Debug.LogWarning($"{DatabaseConstants.LogPrefix}{message}");

            EditorWindow window = EditorWindow.focusedWindow;
            if (window != null && window.rootVisualElement.panel == panel)
                window.ShowNotification(new GUIContent(message));
        }

        #endregion

    }

}
