using System;

using UnityEngine.UIElements;

namespace SideXP.Database.EditorOnly
{

    /// <summary>
    /// Defines how the values of a column are handled by the Database window: how they are displayed, and how they are converted to
    /// text and back. Register a handler with <see cref="CustomCellHandlerAttribute"/>.
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
