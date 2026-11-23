using System;

using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;

using Object = UnityEngine.Object;

namespace SideXP.Database.EditorOnly
{

    /// <summary>
    /// Displays references to Unity objects with their icon and name, like the object fields of the Inspector. Converts references to
    /// the path of their asset (<c>Assets/Items/Sword.asset</c>), followed by the name of the sub-asset if the reference is a sub-asset
    /// (<c>Assets/Items/Sword.asset#Blade</c>).
    /// </summary>
    [CustomCellHandler(typeof(Object), UseForDerivedTypes = true)]
    internal class ObjectReferenceCellHandler : CellHandler
    {

        #region Fields

        private const string ReferenceClass = "database-cell__reference";
        private const string IconClass = "database-cell__reference-icon";

        /// <summary>
        /// The character between the path of an asset and the name of a sub-asset in the text of a reference.
        /// </summary>
        private const char SubAssetSeparator = '#';

        #endregion


        #region Display

        /// <inheritdoc/>
        public override VisualElement CreateDisplay()
        {
            VisualElement reference = new VisualElement();
            reference.AddToClassList(ReferenceClass);

            Image icon = new Image { scaleMode = ScaleMode.ScaleToFit };
            icon.AddToClassList(IconClass);
            reference.Add(icon);

            Label label = new Label();
            label.AddToClassList(DisplayLabelClass);
            reference.Add(label);

            return reference;
        }

        /// <inheritdoc/>
        public override void BindDisplay(VisualElement element, object value, CellContext context)
        {
            Image icon = (Image)element[0];
            Label label = (Label)element[1];
            Object reference = value as Object;
            string typeName = context.ValueType != null ? context.ValueType.Name : nameof(Object);

            if (reference != null)
            {
                icon.image = AssetPreview.GetMiniThumbnail(reference);
                label.text = reference.name;
                element.tooltip = AssetDatabase.GetAssetPath(reference);
            }
            else
            {
                icon.image = context.ValueType != null ? AssetPreview.GetMiniTypeThumbnail(context.ValueType) : null;
                // A missing reference (deleted asset) is not null for C#, but is null for Unity
                label.text = value is Object ? $"Missing ({typeName})" : $"None ({typeName})";
                element.tooltip = null;
            }
        }

        #endregion


        #region Text conversion

        /// <inheritdoc/>
        public override string ToText(object value)
        {
            Object reference = value as Object;
            if (reference == null)
                return string.Empty;

            string path = AssetDatabase.GetAssetPath(reference);
            // An object that is not an asset (for example a scene object) has no path
            if (string.IsNullOrEmpty(path))
                return reference.name;

            return AssetDatabase.IsSubAsset(reference)
                ? $"{path}{SubAssetSeparator}{reference.name}"
                : path;
        }

        /// <summary>
        /// Converts a text to a reference. The text can be the path of an asset, with the name of a sub-asset after a <c>#</c>, or the
        /// GUID of an asset. An empty text is a null reference.
        /// </summary>
        /// <inheritdoc/>
        public override bool TryParse(string text, Type valueType, out object value)
        {
            value = null;
            if (text == null || valueType == null || !typeof(Object).IsAssignableFrom(valueType))
                return false;

            string trimmedText = text.Trim();
            if (trimmedText.Length == 0)
                return true;

            string path = trimmedText;
            string subAssetName = null;
            int separatorIndex = trimmedText.LastIndexOf(SubAssetSeparator);
            if (separatorIndex >= 0)
            {
                path = trimmedText.Substring(0, separatorIndex);
                subAssetName = trimmedText.Substring(separatorIndex + 1);
            }

            // Not a path of the project: try it as a GUID
            if (AssetDatabase.AssetPathToGUID(path).Length == 0)
                path = AssetDatabase.GUIDToAssetPath(path);

            if (string.IsNullOrEmpty(path))
                return false;

            if (subAssetName == null)
            {
                Object mainAsset = AssetDatabase.LoadMainAssetAtPath(path);
                value = valueType.IsInstanceOfType(mainAsset) ? mainAsset : null;
                return value != null;
            }

            foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(path))
            {
                if (asset != null && asset.name == subAssetName && valueType.IsInstanceOfType(asset) && AssetDatabase.IsSubAsset(asset))
                {
                    value = asset;
                    return true;
                }
            }

            return false;
        }

        #endregion

    }

}
