using System;

using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;

namespace SideXP.Database.EditorOnly
{

    /// <summary>
    /// Defines how the values of a column are handled by the Database window: how they are displayed, edited, compared for sorting,
    /// and converted to text and back. Register a handler with <see cref="CustomCellHandlerAttribute"/>.
    /// </summary>
    /// <remarks>
    /// Each part has a default implementation, so a handler only overrides what it changes. A single instance of each handler is shared
    /// by all the cells of all the tables, so a handler must not keep any state about a cell. Display elements are reused for other
    /// rows when the table scrolls, so <see cref="BindDisplay"/> must set everything that depends on the value.
    /// <para>
    /// Displays and edit fields fill their cell. To adapt them to the width of the cell (for example to hide a part of them in narrow
    /// columns), register a <see cref="GeometryChangedEvent"/> callback on the element, and read the width of its parent (the cell),
    /// which changes when the column is resized. The built-in handler of <see cref="RangeAttribute"/> hides its slider that way.
    /// </para>
    /// </remarks>
    public abstract class CellHandler
    {

        #region Fields

        /// <summary>
        /// The USS class of the label created by the default display. Use it on the labels of custom displays, so they look like the
        /// other cells.
        /// </summary>
        public const string DisplayLabelClass = "database-cell__label";

        /// <summary>
        /// The USS class of the default edit fields, which fill their cell. Use it on custom edit fields, so they fit in the cell like
        /// the others.
        /// </summary>
        public const string EditFieldClass = "database-cell__edit-field";

        #endregion


        #region Display

        /// <summary>
        /// Creates the element that displays the values of a cell. The element shows a single line, and is reused for other rows of the
        /// same column.
        /// </summary>
        /// <returns>Returns the created element. By default, a label.</returns>
        public virtual VisualElement CreateDisplay()
        {
            Label label = new Label();
            label.AddToClassList(DisplayLabelClass);
            return label;
        }

        /// <summary>
        /// Shows the value of a cell in an element created by <see cref="CreateDisplay"/>.
        /// </summary>
        /// <param name="element">The element created by <see cref="CreateDisplay"/>.</param>
        /// <param name="value">The value of the cell, read from the value cache.</param>
        /// <param name="context">Informations about the cell.</param>
        public virtual void BindDisplay(VisualElement element, object value, CellContext context)
        {
            if (element is Label label)
                label.text = ToText(value);
        }

        #endregion


        #region Live controls

        /// <summary>
        /// Makes an element of a display a live control, like the built-in checkboxes and dropdowns: the user changes the value of the
        /// cell with it, without entering the edit mode. Clicking the control doesn't select the cell, and doesn't change the focus.
        /// </summary>
        /// <remarks>
        /// Call it in <see cref="CreateDisplay"/>, then call <see cref="SubmitValue(VisualElement, object)"/> when the value of the
        /// control changes. A handler whose display is a live control usually has no edit field (<see cref="CreateEditField"/> returns
        /// null).
        /// </remarks>
        /// <param name="control">The element that changes the value.</param>
        protected internal static void SetLiveControl(VisualElement control)
        {
            // Not focusable, so the keys keep going to the table, and a reused element never keeps the focus of another row
            control.focusable = false;
            control.RegisterCallback<PointerDownEvent>(OnLiveControlPointerDown);
        }

        /// <summary>
        /// Writes a value chosen with a live control to the asset of its cell, as a single Undo step. If the value can't be written, the
        /// cell shows its previous value again.
        /// </summary>
        /// <param name="control">The live control, in the display of the cell.</param>
        /// <param name="value">The chosen value, of the type of the column.</param>
        protected static void SubmitValue(VisualElement control, object value)
        {
            using (CellValueSubmitEvent evt = CellValueSubmitEvent.GetPooled(value))
            {
                // SendEvent() doesn't set the target: without one, the event has no propagation path and never reaches the cell
                evt.target = control;
                control.SendEvent(evt);
            }
        }

        /// <summary>
        /// Called when a live control is clicked. Prevents the click from selecting the cell and from changing the focus.
        /// </summary>
        /// <param name="evt">The emitted event.</param>
        private static void OnLiveControlPointerDown(PointerDownEvent evt)
        {
            if (evt.currentTarget is VisualElement control)
                control.panel?.focusController?.IgnoreEvent(evt);

            // The control handles the click on itself or its children before this callback, so stopping here only stops the cell
            evt.StopPropagation();
        }

        #endregion


        #region Edit

        /// <summary>
        /// Creates the field that edits a cell, in edit mode. Only the edited cell has a field, which replaces its display until the
        /// edit ends.
        /// </summary>
        /// <remarks>
        /// A field can either be left unbound, and give its value to <see cref="TryGetEditedValue"/> when the edit is confirmed, or be
        /// bound to the property (<c>BindProperty()</c>) and write it itself. In both cases, the writes of an edit form a single Undo
        /// step, and are reverted if the edit is cancelled.
        /// </remarks>
        /// <param name="property">The serialized property of the cell, valid until the edit ends.</param>
        /// <param name="value">The current value of the cell, read from the value cache.</param>
        /// <param name="context">Informations about the cell.</param>
        /// <returns>Returns the created field, or null if the cell can't be edited. By default, the UI Toolkit field that matches the type
        /// of the value (text, number, toggle, enum, object, color, vector, curve, ...). For the other types, a text field that goes
        /// through <see cref="ToText"/> and <see cref="TryParse(string, CellContext, out object)"/>, if the text of the current value
        /// can be converted back. Otherwise, the cell can't be edited. Property drawers are never used: a handler registered for the
        /// attribute of a field gives the cells a GUI designed for them.</returns>
        public virtual VisualElement CreateEditField(SerializedProperty property, object value, CellContext context)
        {
            VisualElement field = EditFields.Create(context.ValueType, value, property);
            if (field != null)
                return field;

            // The other types are edited as text, if the text conversion of this handler supports them
            string text = ToText(value);
            return TryParse(text, context, out _)
                ? EditFields.CreateTextConversionField(text)
                : null;
        }

        /// <summary>
        /// Gets the value to write when the edit of a cell is confirmed.
        /// </summary>
        /// <param name="field">The field created by <see cref="CreateEditField"/>.</param>
        /// <param name="context">Informations about the cell.</param>
        /// <param name="value">Outputs the value to write, of the type of the property.</param>
        /// <returns>Returns true if there is a value to write. Returns false if the field writes the property itself (bound field), or if
        /// the value is rejected: nothing is then written. By default, reads the default fields, and rejects the values that don't fit
        /// in the type of the column, or the texts that <see cref="TryParse(string, CellContext, out object)"/> can't convert.</returns>
        public virtual bool TryGetEditedValue(VisualElement field, CellContext context, out object value)
        {
            // Curves and gradients write their property themselves
            if (EditFields.IsBound(field))
            {
                value = null;
                return false;
            }

            if (!EditFields.TryGetConvertedText(field, out string text))
                return EditFields.TryGetValue(field, context.ValueType, out value);

            if (TryParse(text, context, out value))
                return true;

            Debug.LogWarning($"{DatabaseConstants.LogPrefix}The text \"{text}\" can't be converted to a {context.ValueType.Name}: the " +
                "edit is ignored.");
            return false;
        }

        #endregion


        #region Compare

        /// <summary>
        /// Checks if the values of a column can be sorted. Called once per column, when the table loads: the header of a column that
        /// can't be sorted doesn't react to clicks.
        /// </summary>
        /// <param name="valueType">The type of the values of the column.</param>
        /// <returns>Returns true if the values can be compared with <see cref="Compare"/>. By default, true for the types that implement
        /// <see cref="IComparable"/>, like strings, numbers, booleans and enums.</returns>
        public virtual bool CanCompare(Type valueType)
        {
            return ValueComparison.CanCompare(valueType);
        }

        /// <summary>
        /// Compares two values of a column, for sorting. Only called if <see cref="CanCompare"/> returns true for the column.
        /// </summary>
        /// <remarks>
        /// The sort is stable: the rows of equal values keep the order of the assets in the project.
        /// </remarks>
        /// <param name="a">The first value, read from the value cache.</param>
        /// <param name="b">The second value, read from the value cache.</param>
        /// <returns>Returns a negative number if <paramref name="a"/> comes before <paramref name="b"/> in ascending order, a positive
        /// number if it comes after, or 0 if they are equal. By default, null values come first, strings use the natural order of the
        /// Project window (<c>Sword 2</c> before <c>Sword 10</c>), and other values use <see cref="IComparable"/>.</returns>
        public virtual int Compare(object a, object b)
        {
            return ValueComparison.Compare(a, b);
        }

        #endregion


        #region Text conversion

        /// <summary>
        /// Converts a value to text, for display, search, copy and export.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <returns>Returns the converted value. By default, an empty string for null, invariant culture formats for numbers, and names
        /// for enums.</returns>
        public virtual string ToText(object value)
        {
            return TextConversion.ToText(value);
        }

        /// <summary>
        /// Converts a text to a value, for paste and import.
        /// </summary>
        /// <param name="text">The text to convert.</param>
        /// <param name="valueType">The type of the value to get.</param>
        /// <param name="value">Outputs the converted value.</param>
        /// <returns>Returns true if the text has been converted. By default, strings, numbers, booleans, characters and enums are
        /// supported.</returns>
        public virtual bool TryParse(string text, Type valueType, out object value)
        {
            return TextConversion.TryParse(text, valueType, out value);
        }

        /// <summary>
        /// Converts a text to the value of a cell, for paste, import and the edit as text. Override it instead of
        /// <see cref="TryParse(string, Type, out object)"/> when the value depends on the field of the cell, for example to apply the
        /// limits of its attributes.
        /// </summary>
        /// <param name="text">The text to convert.</param>
        /// <param name="context">Informations about the cell.</param>
        /// <param name="value">Outputs the converted value.</param>
        /// <returns>Returns true if the text has been converted. By default, calls <see cref="TryParse(string, Type, out object)"/>
        /// with the type of the column.</returns>
        public virtual bool TryParse(string text, CellContext context, out object value)
        {
            return TryParse(text, context.ValueType, out value);
        }

        #endregion


        #region Field limits

        /// <summary>
        /// Clamps a number to the limits that the attributes of the field of its cell declare (<see cref="RangeAttribute"/> and
        /// <see cref="MinAttribute"/>), like the Inspector does.
        /// </summary>
        /// <remarks>
        /// Use it in custom handlers registered for another attribute of a field, so the limits still apply: a column uses a single
        /// handler, the one of the first attribute of its field that has one.
        /// </remarks>
        /// <param name="value">The value to clamp, of the type of the column.</param>
        /// <param name="context">Informations about the cell.</param>
        /// <returns>Returns the clamped value, or the value itself if it's not a number or if the field declares no limit.</returns>
        protected static object ClampToFieldLimits(object value, CellContext context)
        {
            return FieldLimits.Clamp(value, context.Field);
        }

        #endregion

    }

}
