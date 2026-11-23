using System;

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
        /// <returns>Returns the created field, or null if the cell can't be edited. By default, a text field for strings, and a number
        /// field for numbers.</returns>
        public virtual VisualElement CreateEditField(SerializedProperty property, object value, CellContext context)
        {
            return EditFields.Create(context.ValueType, value);
        }

        /// <summary>
        /// Gets the value to write when the edit of a cell is confirmed.
        /// </summary>
        /// <param name="field">The field created by <see cref="CreateEditField"/>.</param>
        /// <param name="context">Informations about the cell.</param>
        /// <param name="value">Outputs the value to write, of the type of the property.</param>
        /// <returns>Returns true if there is a value to write. Returns false if the field writes the property itself (bound field), or if
        /// the value is rejected: nothing is then written. By default, reads the default fields, and rejects the numbers that don't fit
        /// in the type of the column.</returns>
        public virtual bool TryGetEditedValue(VisualElement field, CellContext context, out object value)
        {
            return EditFields.TryGetValue(field, context.ValueType, out value);
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

        #endregion

    }

}
