using System;
using System.Linq;

using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;

using Object = UnityEngine.Object;

namespace SideXP.Database.EditorOnly
{

    /// <summary>
    /// Draws the fields marked with <see cref="AssetDropdownAttribute"/> as dropdowns of assets, with UI Toolkit (Inspector) and IMGUI
    /// (custom editors that still use it).
    /// </summary>
    /// <remarks>
    /// A field of a type the dropdown doesn't support (see <see cref="AssetDropdownMenu.IsSupported(Type)"/>) shows a message instead,
    /// like the built-in drawers of Unity on fields of the wrong type.
    /// </remarks>
    [CustomPropertyDrawer(typeof(AssetDropdownAttribute))]
    internal class AssetDropdownDrawer : PropertyDrawer
    {

        #region Fields

        /// <summary>
        /// The size of the icon of the referenced asset in the IMGUI dropdown, in pixels.
        /// </summary>
        private static readonly Vector2 IconSize = new Vector2(16, 16);

        #endregion


        #region UI Toolkit

        /// <inheritdoc/>
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            if (!TryGetAssetType(property, out Type assetType))
            {
                // Unity's built-in drawers show the same kind of message on fields of the wrong type
                TextField message = new TextField(property.displayName) { value = AssetDropdownMenu.UnsupportedTypeMessage };
                message.SetEnabled(false);
                message.AddToClassList(BaseField<string>.alignedFieldUssClassName);
                return message;
            }

            AssetDropdownField field = new AssetDropdownField(property.displayName, assetType)
            {
                bindingPath = property.propertyPath,
                tooltip = property.tooltip,
            };
            field.AddToClassList(BaseField<Object>.alignedFieldUssClassName);
            return field;
        }

        #endregion


        #region IMGUI

        /// <inheritdoc/>
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (!TryGetAssetType(property, out Type assetType))
            {
                EditorGUI.LabelField(position, label.text, AssetDropdownMenu.UnsupportedTypeMessage);
                return;
            }

            label = EditorGUI.BeginProperty(position, label, property);
            Rect fieldPosition = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

            // BeginProperty() sets the mixed value of several selected objects
            bool isMixed = EditorGUI.showMixedValue;
            Object value = isMixed ? null : property.objectReferenceValue;
            GUIContent content = isMixed
                ? new GUIContent(AssetDropdownField.MixedValueText)
                : new GUIContent(AssetDropdownMenu.GetValueText(value), AssetDropdownMenu.GetValueIcon(value));

            Vector2 previousIconSize = EditorGUIUtility.GetIconSize();
            EditorGUIUtility.SetIconSize(IconSize);
            bool isClicked = EditorGUI.DropdownButton(fieldPosition, content, FocusType.Keyboard, EditorStyles.popup);
            EditorGUIUtility.SetIconSize(previousIconSize);

            if (isClicked)
            {
                Object[] targets = property.serializedObject.targetObjects;
                string propertyPath = property.propertyPath;
                AssetDropdownMenu.Create(assetType, value, picked => Write(targets, propertyPath, picked)).DropDown(fieldPosition);
            }

            EditorGUI.EndProperty();
        }

        #endregion


        #region Private API

        /// <summary>
        /// Gets the type of the assets listed by the dropdown of a property.
        /// </summary>
        /// <param name="property">The drawn property.</param>
        /// <param name="assetType">Outputs the type of the assets, or null if the dropdown doesn't support the field.</param>
        /// <returns>Returns true if the dropdown supports the field: an object reference to a supported type of asset.</returns>
        private bool TryGetAssetType(SerializedProperty property, out Type assetType)
        {
            // Resolved by Unity, so it's right for private fields of base classes and fields of nested objects
            assetType = AssetDropdownMenu.GetAssetType(fieldInfo?.FieldType);
            return property.propertyType == SerializedPropertyType.ObjectReference && AssetDropdownMenu.IsSupported(assetType);
        }

        /// <summary>
        /// Writes an asset picked in the IMGUI dropdown to the property of the selected objects, as one Undo step.
        /// </summary>
        /// <remarks>
        /// The menu calls back after the GUI that opened it, when its serialized object may have been disposed (for example if the
        /// selection changed), so the property is found again on a new serialized object.
        /// </remarks>
        /// <param name="targets">The objects whose property is written.</param>
        /// <param name="propertyPath">The path of the property.</param>
        /// <param name="picked">The picked asset, or null for <i>None</i>.</param>
        private static void Write(Object[] targets, string propertyPath, Object picked)
        {
            // Some of the objects may have been destroyed while the menu was open
            Object[] existingTargets = targets.Where(target => target != null).ToArray();
            if (existingTargets.Length == 0)
                return;

            using SerializedObject serializedObject = new SerializedObject(existingTargets);
            SerializedProperty property = serializedObject.FindProperty(propertyPath);
            if (property == null)
                return;

            property.objectReferenceValue = picked;
            serializedObject.ApplyModifiedProperties();
        }

        #endregion

    }

}
