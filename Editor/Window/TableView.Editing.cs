using System;

using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

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
    /// Most edits write the serialized property of their column (see <see cref="CellEditSession"/>). The <i>Asset Name</i> column has
    /// no property: when the type of the asset allows renaming (see <see cref="CodeTables.IsRenameAllowed(Type)"/>), its edit renames
    /// the asset when it's confirmed (see <see cref="AssetOperations.TryRename(UnityEngine.Object, string, out string)"/>).
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
            /// The write path of the edit, or null if the edit renames the asset instead of writing a property.
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
        /// Edits a cell: replaces its display with its edit field, and focuses the field.
        /// </summary>
        /// <remarks>
        /// An edit already in progress is confirmed first. Nothing happens if the column doesn't store its values in a serialized
        /// property (built-in columns), except the <i>Asset Name</i> of an asset whose type allows renaming, or if the handler of the
        /// column has no edit field for the cell.
        /// </remarks>
        /// <param name="cell">The element created by <see cref="MakeCell(TableViewColumn, bool)"/>.</param>
        /// <param name="typedCharacter">The character typed to start the edit, which replaces the text of the field, or <c>'\0'</c> to
        /// keep the current value.</param>
        /// <returns>Returns true if the edit has started.</returns>
        private bool BeginEdit(VisualElement cell, char typedCharacter)
        {
            EndEdit(true, false);

            if (cell?.userData is not CellBinding binding || binding.Row == null || binding.Row.Asset == null)
                return false;

            TableRow row = binding.Row;
            TableViewColumn column = binding.Column;
            CellContext context = new CellContext(row.Asset, column.Field, column.Accessor.ValueType);
            VisualElement field;
            CellEditSession session = null;
            if (column.HasProperty)
            {
                if (!TryCreatePropertyEdit(row, column, context, out field, out session))
                    return false;
            }
            else if (column.IsAssetName && CodeTables.IsRenameAllowed(row.Asset.GetType()))
            {
                // The type of the asset decides, not the table, so an asset can be renamed in all the tables that list it, or in none.
                // Edited without handler: the name of a main asset is the name of its file, which has its own rules
                field = EditFields.Create(typeof(string), row.Asset.name);
            }
            else
            {
                return false;
            }

            _edit = new EditState(cell, field, row, column, context, session);
            cell[0].SetDisplayed(false);
            cell.Add(field);
            field.RegisterCallback<FocusOutEvent>(OnEditFieldFocusOut);
            field.RegisterCallback<PointerDownEvent>(OnEditFieldPointerDown, TrickleDown.TrickleDown);
            field.RegisterCallback<ChangeEvent<bool>>(OnEditFieldChoiceMade);
            field.RegisterCallback<ChangeEvent<Enum>>(OnEditFieldChoiceMade);
            FocusEditField(field, typedCharacter);
            return true;
        }

        /// <summary>
        /// Creates the write path and the edit field of a cell whose column stores its values in a serialized property.
        /// </summary>
        /// <param name="row">The row of the cell.</param>
        /// <param name="column">The column of the cell.</param>
        /// <param name="context">Informations about the cell.</param>
        /// <param name="field">Outputs the field created by the handler of the column, or null if the cell can't be edited.</param>
        /// <param name="session">Outputs the write path of the edit, or null if the cell can't be edited.</param>
        /// <returns>Returns true if the cell can be edited.</returns>
        private bool TryCreatePropertyEdit(TableRow row, TableViewColumn column, CellContext context, out VisualElement field,
            out CellEditSession session)
        {
            field = null;
            try
            {
                session = new CellEditSession(row.Asset, column.PropertyPath, $"Edit {column.Name}");
            }
            catch (ArgumentException exception)
            {
                Debug.LogWarning($"{DatabaseConstants.LogPrefix}The cell can't be edited: {exception.Message}");
                session = null;
                return false;
            }

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
                session = null;
                return false;
            }

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

            // The asset may have been deleted during the edit: there is nothing to write then
            bool isWritten = isConfirmed && edit.Row.Asset != null;
            if (isWritten)
            {
                if (edit.Session != null)
                    WriteEditedValue(edit);
                else
                    RenameEditedAsset(edit);
            }

            if (shouldFocusView)
                Focus();

            // Unbound and removed before the session is disposed, so a bound field never uses the disposed property
            edit.Field.Unbind();
            edit.Field.RemoveFromHierarchy();
            edit.CellElement[0].SetDisplayed(true);

            // A rename has nothing to confirm or revert: it's applied only when the edit is confirmed
            if (edit.Session != null)
            {
                if (isWritten)
                    edit.Session.Confirm();
                else
                    edit.Session.Cancel();

                edit.Session.Dispose();
            }

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
        /// Renames the asset of the edited cell with the text of the edit field. Logs a warning if the name is refused.
        /// </summary>
        /// <remarks>
        /// Renaming a main asset moves its file, which reloads the source of the table (see <see cref="TableChangeTracker"/>). The row
        /// keeps its asset, so the active cell stays on it, even if the sort moves the row.
        /// </remarks>
        /// <param name="edit">The edit being confirmed.</param>
        private static void RenameEditedAsset(EditState edit)
        {
            if (!EditFields.TryGetValue(edit.Field, typeof(string), out object value))
                return;

            string previousName = edit.Row.Asset.name;
            if (!AssetOperations.TryRename(edit.Row.Asset, value as string, out string error) && error != null)
            {
                Debug.LogWarning($"{DatabaseConstants.LogPrefix}The asset \"{previousName}\" can't be renamed to \"{value}\": " +
                    $"{error}");
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

            // A container that isn't focusable itself, like a custom edit field made of several fields: focus its first focusable element
            Focusable focusedElement = field.panel?.focusController?.focusedElement;
            if (focusedElement is not VisualElement focused || !field.Contains(focused))
                field.Query<VisualElement>().Where(element => element.focusable && element.canGrabFocus).First()?.Focus();

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


        #region Live controls

        /// <summary>
        /// Called when a live control of a cell display submits a value (see <c>CellHandler.SubmitValue()</c>). Writes it to the asset
        /// of the row.
        /// </summary>
        /// <param name="evt">The emitted event.</param>
        private void OnCellValueSubmitted(CellValueSubmitEvent evt)
        {
            evt.StopPropagation();
            if (evt.currentTarget is VisualElement cell && cell.userData is CellBinding binding && binding.Row != null)
                WriteLiveValue(binding.Row, binding.Column, evt.Value);
        }

        /// <summary>
        /// Writes a value submitted by a live control, as a single Undo step, and refreshes the row.
        /// </summary>
        /// <remarks>
        /// The row is refreshed even if the value can't be written, so the control shows the value of the asset again.
        /// </remarks>
        /// <param name="row">The row of the cell.</param>
        /// <param name="column">The column of the cell.</param>
        /// <param name="value">The submitted value.</param>
        private void WriteLiveValue(TableRow row, TableViewColumn column, object value)
        {
            if (column.HasProperty && row.Asset != null)
            {
                try
                {
                    using (CellEditSession session = new CellEditSession(row.Asset, column.PropertyPath, $"Edit {column.Name}"))
                    {
                        session.Write(value);
                        session.Confirm();
                    }
                }
                catch (Exception exception)
                {
                    Debug.LogWarning($"{DatabaseConstants.LogPrefix}The value {TextConversion.ToText(value)} can't be written " +
                        $"to the column \"{column.Name}\": {exception.Message}");
                }
            }

            _pipeline?.InvalidateRow(row.Asset);
            Refresh();
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
        /// Called when the edit field is clicked, before its parts get the click. Keeps the focus where it is if the clicked part can't
        /// take it.
        /// </summary>
        /// <remarks>
        /// Otherwise, the focus controller moves the focus out of the field (to the list view or the table view), which ends the edit:
        /// for example, the edit would end when the user starts dragging the handle of a slider that is not focusable.
        /// </remarks>
        /// <param name="evt">The emitted event.</param>
        private void OnEditFieldPointerDown(PointerDownEvent evt)
        {
            if (_edit == null || evt.currentTarget is not VisualElement field)
                return;

            for (VisualElement element = evt.target as VisualElement; element != null; element = element.parent)
            {
                // The clicked part, or one of its parents in the field, takes the focus: the focus stays in the field
                if (element.focusable && element.canGrabFocus)
                    return;

                if (element == field)
                    break;
            }

            field.panel?.focusController?.IgnoreEvent(evt);
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
