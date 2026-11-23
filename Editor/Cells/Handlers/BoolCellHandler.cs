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

    }

}
