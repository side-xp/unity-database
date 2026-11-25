using UnityEngine.UIElements;
using UnityEditor;

namespace SideXP.Database.EditorOnly
{

    /// <summary>
    /// Displays booleans as live checkboxes: clicking a checkbox changes the value, without selecting the cell.
    /// </summary>
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
            Toggle toggle = new Toggle();
            toggle.AddToClassList(ToggleClass);
            SetLiveControl(toggle);
            toggle.RegisterValueChangedCallback(evt => SubmitValue(toggle, evt.newValue));
            AllowAltClick(toggle);
            return toggle;
        }

        /// <inheritdoc/>
        public override void BindDisplay(VisualElement element, object value, CellContext context)
        {
            ((Toggle)element).SetValueWithoutNotify(value is bool isChecked && isChecked);
        }

        #endregion


        #region Edit

        /// <summary>
        /// Booleans have no edit mode: the checkbox of the display changes them.
        /// </summary>
        /// <inheritdoc/>
        public override VisualElement CreateEditField(SerializedProperty property, object value, CellContext context)
        {
            return null;
        }

        #endregion


        #region Private API

        /// <summary>
        /// Makes a toggle react to Alt+click, which applies its value to the selected cells of the same type.
        /// </summary>
        /// <remarks>
        /// The click manipulators of Unity only activate without the modifiers they don't expect, so a toggle may ignore an Alt+click.
        /// The value is toggled here if the click didn't toggle it, which also works if Unity does handle it.
        /// </remarks>
        /// <param name="toggle">The toggle of the display.</param>
        private static void AllowAltClick(Toggle toggle)
        {
            // The value when the toggle was pressed with Alt, or null if it wasn't
            bool? valueOnAltPress = null;
            toggle.RegisterCallback<PointerDownEvent>(evt => valueOnAltPress = evt.altKey ? toggle.value : (bool?)null);
            toggle.RegisterCallback<ClickEvent>(evt =>
            {
                if (evt.altKey && valueOnAltPress.HasValue && toggle.value == valueOnAltPress.Value)
                    toggle.value = !toggle.value;

                valueOnAltPress = null;
            });
        }

        #endregion

    }

}
