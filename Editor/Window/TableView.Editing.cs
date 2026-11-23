using System;

using UnityEngine;
using UnityEngine.UIElements;

namespace SideXP.Database.EditorOnly
{

    /// <summary>
    /// The edit mode of the table view: a single cell at a time shows the edit field of its handler instead of its display.
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item>A double-click, Enter or F2 edits the active cell. Typing a character edits it too, starting with that character.</item>
    /// <item>Enter confirms the edit, as well as clicking outside of the edited cell (the field loses the focus), and picking a value
    /// in a toggle or an enum field.</item>
    /// <item>Escape cancels the edit, and reverts its writes.</item>
    /// </list>
    /// </remarks>
    internal partial class TableView
    {

        #region Subclasses

        /// <summary>
        /// The state of the edit in progress.
        /// </summary>
        private class EditState
        {

            /// <summary>
            /// The element of the edited cell, which holds the edit field while the edit is in progress.
            /// </summary>
            public readonly VisualElement CellElement;

            /// <summary>
            /// The field created by the handler of the column.
            /// </summary>
            public readonly VisualElement Field;

            /// <summary>
            /// The row of the edited cell.
            /// </summary>
            public readonly TableRow Row;

            /// <summary>
            /// The column of the edited cell.
            /// </summary>
            public readonly TableViewColumn Column;

            /// <summary>
            /// Informations about the edited cell, given to the handler.
            /// </summary>
            public readonly CellContext Context;

            /// <summary>
            /// The write path of the edit.
            /// </summary>
            public readonly CellEditSession Session;

            /// <inheritdoc cref="EditState"/>
            /// <param name="cellElement"><inheritdoc cref="CellElement" path="/summary"/></param>
            /// <param name="field"><inheritdoc cref="Field" path="/summary"/></param>
            /// <param name="row"><inheritdoc cref="Row" path="/summary"/></param>
            /// <param name="column"><inheritdoc cref="Column" path="/summary"/></param>
            /// <param name="context"><inheritdoc cref="Context" path="/summary"/></param>
            /// <param name="session"><inheritdoc cref="Session" path="/summary"/></param>
            public EditState(VisualElement cellElement, VisualElement field, TableRow row, TableViewColumn column, CellContext context,
                CellEditSession session)
            {
                CellElement = cellElement;
                Field = field;
                Row = row;
                Column = column;
                Context = context;
                Session = session;
            }

        }

        #endregion


        #region Fields

        /// <summary>
        /// The edit in progress, or null if no cell is edited.
        /// </summary>
        private EditState _edit = null;

        #endregion


        #region Public API

        /// <summary>
        /// Checks if a cell is being edited.
        /// </summary>
        public bool IsEditing => _edit != null;

        #endregion


        #region Edit mode

        /// <summary>
        /// Edits a cell: replaces its display with the edit field of its handler, and focuses the field.
        /// </summary>
        /// <remarks>
        /// An edit already in progress is confirmed first. Nothing happens if the column doesn't store its values in a serialized
        /// property (built-in columns), or if its handler has no edit field for the cell.
        /// </remarks>
        /// <param name="cell">The element created by <see cref="MakeCell(TableViewColumn, bool)"/>.</param>
        /// <param name="typedCharacter">The character typed to start the edit, which replaces the text of the field, or <c>'\0'</c> to
        /// keep the current value.</param>
        /// <returns>Returns true if the edit has started.</returns>
        private bool BeginEdit(VisualElement cell, char typedCharacter)
        {
            EndEdit(true, false);

            if (cell?.userData is not CellBinding binding || binding.Row == null || !binding.Column.HasProperty)
                return false;

            TableRow row = binding.Row;
            TableViewColumn column = binding.Column;
            if (row.Asset == null)
                return false;

            CellEditSession session;
            try
            {
                session = new CellEditSession(row.Asset, column.PropertyPath, $"Edit {column.Name}");
            }
            catch (ArgumentException exception)
            {
                Debug.LogWarning($"{DatabaseConstants.LogPrefix}The cell can't be edited: {exception.Message}");
                return false;
            }

            CellContext context = new CellContext(row.Asset, column.Field, column.Accessor.ValueType);
            VisualElement field = null;
            try
            {
                field = column.Handler.CreateEditField(session.Property, row.GetValue(_columns.IndexOf(column)), context);
            }
            catch (Exception exception)
            {
                Debug.LogError($"{DatabaseConstants.LogPrefix}The cell handler {column.Handler.GetType()} failed to create the edit " +
                    $"field of the column \"{column.Name}\".");
                Debug.LogException(exception);
            }

            if (field == null)
            {
                session.Dispose();
                return false;
            }

            _edit = new EditState(cell, field, row, column, context, session);
            cell[0].SetDisplayed(false);
            cell.Add(field);
            field.RegisterCallback<FocusOutEvent>(OnEditFieldFocusOut);
            field.RegisterCallback<ChangeEvent<bool>>(OnEditFieldChoiceMade);
            field.RegisterCallback<ChangeEvent<Enum>>(OnEditFieldChoiceMade);
            FocusEditField(field, typedCharacter);
            return true;
        }

        /// <summary>
        /// Ends the edit in progress, and shows the display of the cell again. Does nothing if no cell is edited.
        /// </summary>
        /// <param name="isConfirmed">If enabled, the value of the field is written and the edit becomes a single Undo step. Otherwise,
        /// the writes of the edit are reverted.</param>
        /// <param name="shouldFocusView">If enabled, the table view gets the focus back, so the keyboard keeps working on the table
        /// (after Enter or Escape). Disabled when the edit ends because the user clicked elsewhere.</param>
        private void EndEdit(bool isConfirmed, bool shouldFocusView)
        {
            if (_edit == null)
                return;

            // Cleared first, since removing the field below makes it lose the focus, which ends the edit
            EditState edit = _edit;
            _edit = null;

            if (isConfirmed)
            {
                WriteEditedValue(edit);
                edit.Session.Confirm();
            }
            else
            {
                edit.Session.Cancel();
            }
            edit.Session.Dispose();

            if (shouldFocusView)
                Focus();

            edit.Field.RemoveFromHierarchy();
            edit.CellElement[0].SetDisplayed(true);

            // The change is also reported by ObjectChangeEvents, but the cell must not show the previous value until then
            _pipeline?.InvalidateRow(edit.Row.Asset);
            Refresh();
        }

        /// <summary>
        /// Writes the value of the edit field, if its handler has one to write.
        /// </summary>
        /// <param name="edit">The edit being confirmed.</param>
        private static void WriteEditedValue(EditState edit)
        {
            CellHandler handler = edit.Column.Handler;
            object value = null;
            bool hasValue = false;
            try
            {
                hasValue = handler.TryGetEditedValue(edit.Field, edit.Context, out value);
            }
            catch (Exception exception)
            {
                Debug.LogError($"{DatabaseConstants.LogPrefix}The cell handler {handler.GetType()} failed to read the edited value of " +
                    $"the column \"{edit.Column.Name}\".");
                Debug.LogException(exception);
            }

            if (!hasValue)
                return;

            try
            {
                edit.Session.Write(value);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"{DatabaseConstants.LogPrefix}The value {TextConversion.ToText(value)} can't be written to the " +
                    $"column \"{edit.Column.Name}\": {exception.Message}");
            }
        }

        /// <summary>
        /// Ends the edit in progress if the element of the edited cell now displays another row.
        /// </summary>
        /// <remarks>
        /// Elements are reused for other rows when the table scrolls, so the edit can't stay in its element. The field is hidden right
        /// away, and the edit is confirmed on the next update of the view, since the rows can't be refreshed while the list view binds
        /// its elements.
        /// </remarks>
        /// <param name="cell">The element created by <see cref="MakeCell(TableViewColumn, bool)"/>, just bound or released.</param>
        /// <param name="row">The row now displayed by the element, or null if it has been released.</param>
        private void EndEditIfRebound(VisualElement cell, TableRow row)
        {
            if (_edit == null || _edit.CellElement != cell || _edit.Row == row)
                return;

            _edit.Field.SetDisplayed(false);
            cell[0].SetDisplayed(true);

            EditState edit = _edit;
            // The focus goes back to the view, since the hidden field can't receive the keys anymore
            schedule.Execute(() =>
            {
                if (_edit == edit)
                    EndEdit(true, true);
            });
        }

        /// <summary>
        /// Focuses an edit field. If it's a text field, selects its text, and replaces it with the typed character if there is one.
        /// </summary>
        /// <param name="field">The edit field.</param>
        /// <param name="typedCharacter">The character typed to start the edit, or <c>'\0'</c> if there is none.</param>
        private static void FocusEditField(VisualElement field, char typedCharacter)
        {
            field.Focus();

            // Text fields delegate the focus to their inner text element, which handles the selection and the typed characters
            if (field.panel?.focusController?.focusedElement is not TextElement textElement || !field.Contains(textElement))
                return;

            textElement.selection.SelectAll();
            if (typedCharacter == '\0')
                return;

            // The text element processes the character like a key typed by the user, so it replaces the selected text
            using (KeyDownEvent keyEvent = KeyDownEvent.GetPooled(typedCharacter, KeyCode.None, EventModifiers.None))
                textElement.SendEvent(keyEvent);
        }

        #endregion


        #region Events

        /// <summary>
        /// Called when a key is pressed while the focus is in the table view. Starts, confirms and cancels the edits.
        /// </summary>
        /// <remarks>
        /// Registered in the trickle down phase, so the keys that confirm and cancel an edit are handled before the edit field gets
        /// them.
        /// </remarks>
        /// <param name="evt">The emitted event.</param>
        private void OnKeyDown(KeyDownEvent evt)
        {
            if (_edit != null)
            {
                OnEditKeyDown(evt);
                return;
            }

            if (_activeCellElement == null)
                return;

            bool isEditKey = evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter || evt.keyCode == KeyCode.F2;
            bool isTyping = evt.character != '\0' && !char.IsControl(evt.character) && !IsShortcut(evt);
            if (!isEditKey && !isTyping)
                return;

            if (BeginEdit(_activeCellElement, isTyping ? evt.character : '\0'))
                evt.StopPropagation();
        }

        /// <summary>
        /// Called when a key is pressed while a cell is edited.
        /// </summary>
        /// <param name="evt">The emitted event.</param>
        private void OnEditKeyDown(KeyDownEvent evt)
        {
            switch (evt.keyCode)
            {
                case KeyCode.Return:
                case KeyCode.KeypadEnter:
                    EndEdit(true, true);
                    evt.StopPropagation();
                    return;

                case KeyCode.Escape:
                    EndEdit(false, true);
                    evt.StopPropagation();
                    return;
            }

            // Enter sends a second event with its character: it must not reach the field that has just been created by Enter
            if (evt.character == '\n' || evt.character == '\r')
                evt.StopPropagation();
        }

        /// <summary>
        /// Called when the edit field loses the focus, for example when the user clicks outside of the edited cell. Confirms the edit.
        /// </summary>
        /// <param name="evt">The emitted event.</param>
        private void OnEditFieldFocusOut(FocusOutEvent evt)
        {
            if (_edit == null)
                return;

            // The focus can move between the parts of the field (for example the axes of a vector). If it goes nowhere in this window,
            // the field may have opened a picker or a menu in another window (object picker, color picker, enum dropdown): the edit
            // goes on until the user confirms it or clicks elsewhere in the window
            if (evt.relatedTarget is not VisualElement newFocus || _edit.Field.Contains(newFocus))
                return;

            EndEdit(true, false);
        }

        /// <summary>
        /// Called when the value of the edit field changes, for the fields where the user picks a single value: toggles and enums.
        /// Confirms the edit, so a pick applies right away.
        /// </summary>
        /// <remarks>
        /// Reference fields are not confirmed on change, since the object picker changes their value each time the selection changes
        /// in the picker: the following picks would be lost.
        /// </remarks>
        /// <typeparam name="T">The type of the value of the field.</typeparam>
        /// <param name="evt">The emitted event.</param>
        private void OnEditFieldChoiceMade<T>(ChangeEvent<T> evt)
        {
            // Only the edit field itself: a custom field may hold toggles or other fields of these types that are only part of its value
            if (_edit == null || evt.target != _edit.Field)
                return;

            // Deferred, since the field is still dispatching its change
            EditState edit = _edit;
            schedule.Execute(() =>
            {
                if (_edit == edit)
                    EndEdit(true, true);
            });
        }

        /// <summary>
        /// Checks if a key event is a shortcut, which must not start an edit.
        /// </summary>
        /// <remarks>
        /// Ctrl and Alt together are not a shortcut: on Windows, AltGr sends both, and is used to type characters on many keyboard
        /// layouts.
        /// </remarks>
        /// <param name="evt">The key event.</param>
        /// <returns>Returns true if Command, or Ctrl without Alt, is held.</returns>
        private static bool IsShortcut(KeyDownEvent evt)
        {
            return evt.commandKey || (evt.ctrlKey && !evt.altKey);
        }

        #endregion

    }

}
