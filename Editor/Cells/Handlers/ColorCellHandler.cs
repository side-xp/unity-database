using System;

using UnityEngine;
using UnityEngine.UIElements;

namespace SideXP.Database.EditorOnly
{

    /// <summary>
    /// Displays colors as swatches, like the color fields of the Inspector: the opaque color, with a bar at the bottom for the alpha.
    /// Converts colors to hexadecimal text (<c>#RRGGBBAA</c>).
    /// </summary>
    [CustomCellHandler(typeof(Color))]
    [CustomCellHandler(typeof(Color32))]
    internal class ColorCellHandler : CellHandler
    {

        #region Fields

        private const string SwatchClass = "database-cell__swatch";
        private const string AlphaBarClass = "database-cell__swatch-alpha";
        private const string HexPrefix = "#";

        #endregion


        #region Display

        /// <inheritdoc/>
        public override VisualElement CreateDisplay()
        {
            VisualElement swatch = new VisualElement();
            swatch.AddToClassList(SwatchClass);

            VisualElement alphaBar = new VisualElement();
            alphaBar.AddToClassList(AlphaBarClass);
            swatch.Add(alphaBar);

            return swatch;
        }

        /// <inheritdoc/>
        public override void BindDisplay(VisualElement element, object value, CellContext context)
        {
            Color color = ToColor(value);
            element.style.backgroundColor = new Color(color.r, color.g, color.b, 1);
            element.tooltip = ToText(value);
            element[0].style.width = Length.Percent(color.a * 100);
        }

        #endregion


        #region Text conversion

        /// <inheritdoc/>
        public override string ToText(object value)
        {
            return value is Color || value is Color32
                ? $"{HexPrefix}{ColorUtility.ToHtmlStringRGBA(ToColor(value))}"
                : base.ToText(value);
        }

        /// <inheritdoc/>
        public override bool TryParse(string text, Type valueType, out object value)
        {
            value = null;
            if (text == null)
                return false;

            // ColorUtility also accepts color names (red, blue, ...), and needs the # prefix for hexadecimal colors
            string trimmedText = text.Trim();
            if (!trimmedText.StartsWith(HexPrefix) && IsHexadecimal(trimmedText))
                trimmedText = $"{HexPrefix}{trimmedText}";

            if (!ColorUtility.TryParseHtmlString(trimmedText, out Color color))
                return false;

            if (valueType == typeof(Color32))
                value = (Color32)color;
            else if (valueType == typeof(Color))
                value = color;
            else
                return false;

            return true;
        }

        #endregion


        #region Private API

        /// <summary>
        /// Converts a value of a color column to a color.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <returns>Returns the color, or a transparent color if the value is not a color.</returns>
        private static Color ToColor(object value)
        {
            switch (value)
            {
                case Color color:
                    return color;

                case Color32 color32:
                    return color32;

                default:
                    return Color.clear;
            }
        }

        /// <summary>
        /// Checks if a text only contains hexadecimal digits.
        /// </summary>
        /// <param name="text">The text to check.</param>
        /// <returns>Returns true if the text is not empty and only contains hexadecimal digits.</returns>
        private static bool IsHexadecimal(string text)
        {
            if (text.Length == 0)
                return false;

            foreach (char c in text)
            {
                if (!Uri.IsHexDigit(c))
                    return false;
            }
            return true;
        }

        #endregion

    }

}
