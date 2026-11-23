using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace SideXP.Database.EditorOnly
{

    /// <summary>
    /// Creates the small elements shared by the pages of the Database window.
    /// </summary>
    internal static class DatabaseElements
    {

        #region Fields

        private const string IconClass = "database-icon";
        private const string ObsoleteMarkClass = "database-obsolete-mark";
        private const string ObsoleteText = "Obsolete";
        private const string ObsoleteTooltip = "The type of this table is marked [Obsolete].";

        #endregion


        #region Public API

        /// <summary>
        /// Creates the mark of obsolete tables: a warning icon, or a text if the icon can't be loaded.
        /// </summary>
        /// <returns>Returns the created mark.</returns>
        public static VisualElement CreateObsoleteMark()
        {
            Texture icon = BuiltInIcons.Get(BuiltInIcons.Warning);
            VisualElement mark = icon != null
                ? new Image { image = icon, scaleMode = ScaleMode.ScaleToFit }
                : new Label(ObsoleteText);

            mark.tooltip = ObsoleteTooltip;
            mark.AddToClassList(ObsoleteMarkClass);
            if (icon != null)
                mark.AddToClassList(IconClass);

            return mark;
        }

        /// <summary>
        /// Shows a built-in icon on a toolbar button, or a text if the icon can't be loaded.
        /// </summary>
        /// <param name="button">The button on which to show the icon.</param>
        /// <param name="iconName">The name of the icon, one of the constants of <see cref="BuiltInIcons"/>.</param>
        /// <param name="fallbackText">The text shown if the icon can't be loaded.</param>
        public static void SetIcon(ToolbarButton button, string iconName, string fallbackText)
        {
            if (BuiltInIcons.Get(iconName) is Texture2D icon)
            {
                button.iconImage = Background.FromTexture2D(icon);
                button.text = string.Empty;
            }
            else
            {
                button.text = fallbackText;
            }
        }

        #endregion

    }

}
