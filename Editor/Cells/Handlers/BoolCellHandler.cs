using UnityEngine.UIElements;

namespace SideXP.Database.EditorOnly
{

    /// <summary>
    /// Displays booleans as checkboxes.
    /// </summary>
    /// <remarks>
    /// @note The checkboxes are read-only (and so look disabled) until the live controls of step 10 of the implementation.
    /// </remarks>
    [CustomCellHandler(typeof(bool))]
    internal class BoolCellHandler : CellHandler
    {

        #region Fields

        private const string ToggleClass = "database-cell__toggle";

        #endregion


        #region Display

        /// <inheritdoc/>
        public override VisualElement CreateDisplay()
        {
            Toggle toggle = new Toggle { focusable = false };
            toggle.AddToClassList(ToggleClass);
            toggle.SetEnabled(false);
            return toggle;
        }

        /// <inheritdoc/>
        public override void BindDisplay(VisualElement element, object value, CellContext context)
        {
            ((Toggle)element).SetValueWithoutNotify(value is bool isChecked && isChecked);
        }

        #endregion

    }

}
