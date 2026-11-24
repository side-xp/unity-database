using UnityEngine;
using UnityEngine.UIElements;

using SideXP.Core.EditorOnly;

namespace SideXP.Database.EditorOnly
{

    /// <summary>
    /// Creates the small elements shared by the pages of the Database window.
    /// </summary>
    internal static class DatabaseElements
    {

        #region Fields

        private const string IconClass = "database-icon";
        private const string IconButtonClass = "database-icon-button";
        private const string IconButtonLabelClass = "database-icon-button__label";
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
        /// Shows a Tabler icon on a button, alone or followed by a label. Without label, the tooltip of the button says what it does.
        /// </summary>
        /// <remarks>
        /// The icon shows a loading placeholder until it's downloaded, the first time it's used on the computer (see the
        /// <c>EditorIcons</c> feature of the Core package).
        /// </remarks>
        /// <param name="button">The button on which to show the icon, toolbar buttons included.</param>
        /// <param name="iconName">The name of the icon, one of the constants of <see cref="DatabaseIcons"/>.</param>
        /// <param name="label">The text shown after the icon, or null to show the icon alone.</param>
        public static void SetIcon(Button button, string iconName, string label = null)
        {
            // The text of a button is drawn over its whole content, not beside its children: the label is an element of its own
            button.text = string.Empty;
            button.AddToClassList(IconButtonClass);

            // The button is the target of the clicks and of the tooltip, not its content
            Icon icon = new Icon(iconName) { pickingMode = PickingMode.Ignore };
            button.Add(icon);

            if (string.IsNullOrEmpty(label))
                return;

            Label labelElement = new Label(label) { pickingMode = PickingMode.Ignore };
            labelElement.AddToClassList(IconButtonLabelClass);
            button.Add(labelElement);
        }

        #endregion

    }

}
