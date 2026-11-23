using System;

using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.UIElements;

namespace SideXP.Database.EditorOnly
{

    /// <summary>
    /// Displays enums as live dropdowns: picking an option changes the value, without selecting the cell. Enums marked with
    /// <see cref="FlagsAttribute"/> get a dropdown of flags.
    /// </summary>
    /// <remarks>
    /// The display doesn't know the enum type of its column when it's created, so the dropdown is created when the first value is
    /// bound, and created again if a value of another enum type is bound (a field of type <see cref="Enum"/>).
    /// </remarks>
    [CustomCellHandler(typeof(Enum), UseForDerivedTypes = true)]
    internal class EnumCellHandler : CellHandler
    {

        #region Fields

        private const string DropdownClass = "database-cell__dropdown";

        #endregion


        #region Display

        /// <inheritdoc/>
        public override VisualElement CreateDisplay()
        {
            VisualElement container = new VisualElement();
            container.AddToClassList(DropdownClass);
            return container;
        }

        /// <inheritdoc/>
        public override void BindDisplay(VisualElement element, object value, CellContext context)
        {
            if (value is not Enum enumValue)
            {
                element.Clear();
                return;
            }

            Type enumType = enumValue.GetType();
            if (element.childCount > 0 && element[0] is BaseField<Enum> field && Equals(field.userData, enumType))
            {
                field.SetValueWithoutNotify(enumValue);
                return;
            }

            element.Clear();
            element.Add(CreateDropdown(enumValue));
        }

        #endregion


        #region Edit

        /// <summary>
        /// Enums have no edit mode: the dropdown of the display changes them.
        /// </summary>
        /// <inheritdoc/>
        public override VisualElement CreateEditField(SerializedProperty property, object value, CellContext context)
        {
            return null;
        }

        #endregion


        #region Private API

        /// <summary>
        /// Creates the live dropdown of an enum type.
        /// </summary>
        /// <param name="value">The current value, whose type is the enum type of the dropdown.</param>
        /// <returns>Returns the created dropdown, with its enum type in its <see cref="VisualElement.userData"/>.</returns>
        private static BaseField<Enum> CreateDropdown(Enum value)
        {
            Type enumType = value.GetType();
            BaseField<Enum> dropdown = enumType.IsDefined(typeof(FlagsAttribute), false)
                ? (BaseField<Enum>)new EnumFlagsField(value)
                : new EnumField(value);

            dropdown.userData = enumType;
            dropdown.AddToClassList(EditFieldClass);
            SetLiveControl(dropdown);
            dropdown.RegisterValueChangedCallback(evt => SubmitValue(dropdown, evt.newValue));
            return dropdown;
        }

        #endregion

    }

}
