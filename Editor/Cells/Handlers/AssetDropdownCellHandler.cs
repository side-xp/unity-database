using System;

using UnityEngine.UIElements;
using UnityEditor;

using SideXP.Core;
using SideXP.Core.EditorOnly;

using Object = UnityEngine.Object;

namespace SideXP.Database.EditorOnly
{

    /// <summary>
    /// Displays the fields marked with <see cref="AssetDropdownAttribute"/> (from the core lib) as live dropdowns, like in the
    /// Inspector: picking an option changes the value, without selecting the cell.
    /// </summary>
    /// <remarks>
    /// The dropdown is the <see cref="AssetDropdownField"/> of the core lib. The sort and the text conversion are the ones of the other
    /// references (see <see cref="ObjectReferenceCellHandler"/>). The display doesn't know the type of its column when it's created, so
    /// the dropdown is created when the first value is bound. A field of a type the dropdown doesn't support is displayed and edited
    /// like the other references.
    /// </remarks>
    [CustomCellHandler(typeof(AssetDropdownAttribute))]
    internal class AssetDropdownCellHandler : ObjectReferenceCellHandler
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
            Type assetType = AssetDropdownMenu.GetAssetType(context.ValueType);
            if (!AssetDropdownMenu.IsSupported(assetType))
            {
                // The display of the other references, created once
                if (element.childCount == 0 || element[0] is AssetDropdownField)
                {
                    element.Clear();
                    element.Add(base.CreateDisplay());
                }

                base.BindDisplay(element[0], value, context);
                return;
            }

            // The cast keeps a missing reference (deleted asset), which is not null for C#, so the dropdown shows it as missing
            Object reference = value as Object;
            if (element.childCount > 0 && element[0] is AssetDropdownField field && field.AssetType == assetType)
            {
                field.SetValueWithoutNotify(reference);
                return;
            }

            element.Clear();
            AssetDropdownField dropdown = CreateDropdown(assetType);
            dropdown.SetValueWithoutNotify(reference);
            element.Add(dropdown);
        }

        #endregion


        #region Edit

        /// <summary>
        /// The fields that the dropdown supports have no edit mode: the dropdown of the display changes them. The others are edited
        /// like the other references.
        /// </summary>
        /// <inheritdoc/>
        public override VisualElement CreateEditField(SerializedProperty property, object value, CellContext context)
        {
            return AssetDropdownMenu.IsSupported(AssetDropdownMenu.GetAssetType(context.ValueType))
                ? null
                : base.CreateEditField(property, value, context);
        }

        #endregion


        #region Private API

        /// <summary>
        /// Creates the live dropdown of a type of asset.
        /// </summary>
        /// <param name="assetType">The type of the assets listed by the dropdown.</param>
        /// <returns>Returns the created dropdown.</returns>
        private static AssetDropdownField CreateDropdown(Type assetType)
        {
            AssetDropdownField dropdown = new AssetDropdownField(null, assetType);
            dropdown.AddToClassList(EditFieldClass);
            SetLiveControl(dropdown);
            dropdown.RegisterValueChangedCallback(evt => SubmitValue(dropdown, evt.newValue));
            return dropdown;
        }

        #endregion

    }

}
