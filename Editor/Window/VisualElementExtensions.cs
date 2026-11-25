using UnityEngine.UIElements;

namespace SideXP.Database.EditorOnly
{

    /// <summary>
    /// Extension functions for <see cref="VisualElement"/>.
    /// </summary>
    internal static class VisualElementExtensions
    {

        /// <summary>
        /// Shows or hides an element, removing it from the layout when it's hidden.
        /// </summary>
        /// <param name="element">The element to show or hide.</param>
        /// <param name="isDisplayed">If enabled, the element is shown.</param>
        public static void SetDisplayed(this VisualElement element, bool isDisplayed)
        {
            element.style.display = isDisplayed ? DisplayStyle.Flex : DisplayStyle.None;
        }

        /// <summary>
        /// Checks if an element is shown by <see cref="SetDisplayed"/>: its inline display style isn't <see cref="DisplayStyle.None"/>.
        /// </summary>
        /// <param name="element">The element to check.</param>
        /// <returns>Returns true if the element is shown, even if one of its parents is hidden.</returns>
        public static bool IsDisplayed(this VisualElement element)
        {
            return element.style.display != DisplayStyle.None;
        }

    }

}
