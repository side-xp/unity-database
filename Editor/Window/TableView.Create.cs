using System;
using System.Collections.Generic;
using System.IO;

using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;

using Object = UnityEngine.Object;

namespace SideXP.Database.EditorOnly
{

    /// <summary>
    /// The creation of assets from the table view: the operation started by the <i>Create</i> button of the toolbar, and the
    /// <i>Create Asset</i> button of the message shown when the table has no asset.
    /// </summary>
    /// <remarks>
    /// When the table has several types that can be created (an abstract table type with several concrete types, or a table type
    /// with derived types), a menu lists them, the table type first. Then a save dialog asks where to create the asset. The new asset
    /// becomes the active cell (see <see cref="ShowAsset(Object)"/>).
    /// </remarks>
    internal partial class TableView
    {

        #region Fields

        private const string EmptyCreateButtonClass = "database-table-view__empty-create";

        private const string CreateAssetText = "Create Asset";
        private const string SaveDialogTitle = "Create Asset";

        /// <summary>
        /// The folder of the save dialog when there is no asset in the table to take the folder of.
        /// </summary>
        private const string FallbackFolder = "Assets";

        /// <summary>
        /// The types of the assets that can be created in the table, the table type first. Empty if no table is displayed, or if no
        /// type of the table can be created (abstract type without concrete derived type).
        /// </summary>
        private List<Type> _creatableTypes = new List<Type>();

        #endregion


        #region Public API

        /// <summary>
        /// Checks if assets can be created in the displayed table.
        /// </summary>
        public bool CanCreateAssets => _creatableTypes.Count > 0;

        /// <summary>
        /// Updates a button that creates an asset of the displayed table: hidden if no asset can be created, with the icon of a menu if
        /// the user must choose a type first, and a tooltip that names the type to create, or the base type of the choice. Call it each
        /// time the table changes.
        /// </summary>
        /// <remarks>
        /// The button must call <see cref="StartCreateAsset(VisualElement)"/> when it's clicked.
        /// </remarks>
        /// <param name="button">The button to update.</param>
        /// <param name="label">The text of the button, shown after its icon.</param>
        public void UpdateCreateButton(Button button, string label)
        {
            button.SetDisplayed(CanCreateAssets);
            if (!CanCreateAssets)
                return;

            bool hasChoice = _creatableTypes.Count > 1;
            DatabaseElements.SetBuiltInIcon(button, hasChoice ? BuiltInIcons.AddDropdown : BuiltInIcons.Add, label);
            button.tooltip = GetCreateTooltip();
        }

        /// <summary>
        /// Starts the creation of an asset of the table. If the table has a single type that can be created, the save dialog opens
        /// right away. Otherwise, a menu lets the user choose the type first.
        /// </summary>
        /// <param name="menuAnchor">The element under which the menu of the types opens, usually the clicked button.</param>
        public void StartCreateAsset(VisualElement menuAnchor)
        {
            if (_table == null || !CanCreateAssets)
                return;

            if (_creatableTypes.Count == 1)
            {
                CreateAsset(_creatableTypes[0]);
                return;
            }

            GenericMenu menu = new GenericMenu();
            foreach (Type type in _creatableTypes)
                menu.AddItem(new GUIContent(AssetOperations.GetDisplayName(type)), false, () => CreateAsset(type));

            menu.DropDown(menuAnchor.worldBound);
        }

        #endregion


        #region Private API

        /// <summary>
        /// Creates the <i>Create Asset</i> button of the message shown when the table has no asset. Hidden until a table whose assets
        /// can be created is set (see <see cref="UpdateCreateButton(Button, string)"/>).
        /// </summary>
        /// <returns>Returns the created button.</returns>
        private Button CreateEmptyCreateButton()
        {
            Button button = new Button { text = CreateAssetText };
            button.clicked += () => StartCreateAsset(button);
            button.AddToClassList(EmptyCreateButtonClass);
            button.SetDisplayed(false);
            return button;
        }

        /// <summary>
        /// Gets the tooltip of the buttons that create an asset of the displayed table.
        /// </summary>
        /// <returns>Returns a tooltip that names the type of the asset to create if there is a single one, or the base type of the
        /// types to choose from otherwise.</returns>
        private string GetCreateTooltip()
        {
            if (_creatableTypes.Count == 1)
                return $"Creates a new {AssetOperations.GetDisplayName(_creatableTypes[0])} asset.";

            // The table type is the base type of the choice, and the first choice if it's concrete
            string baseName = AssetOperations.GetDisplayName(_table.Type);
            return _creatableTypes[0] == _table.Type
                ? $"Creates a new {baseName} asset, or an asset of a type derived from {baseName}. Choose the type first."
                : $"Creates a new asset of a type derived from {baseName}. Choose the type first.";
        }

        /// <summary>
        /// Asks where to create an asset of a type with a save dialog, then creates it.
        /// </summary>
        /// <param name="type">The type of the asset to create, one of <see cref="_creatableTypes"/>.</param>
        private void CreateAsset(Type type)
        {
            // Like a click elsewhere: an edit in progress is confirmed before the dialog opens
            EndEdit(true, false);

            string typeName = AssetOperations.GetDisplayName(type);
            string path = EditorUtility.SaveFilePanelInProject(
                SaveDialogTitle,
                $"New {typeName}",
                AssetOperations.AssetExtension.TrimStart('.'),
                $"Choose where to create the new {typeName}.",
                GetDefaultFolder());

            // Empty if the dialog has been cancelled
            if (string.IsNullOrEmpty(path))
                return;

            if (!AssetOperations.TryCreate(type, path, out Object asset, out string error))
            {
                Debug.LogWarning($"{DatabaseConstants.LogPrefix}The {typeName} can't be created at {path}: {error}");
                return;
            }

            ShowNewAsset(asset);
        }

        /// <summary>
        /// Gets the folder in which the save dialog opens: the folder of the asset of the active cell, or of the first row, so new assets
        /// go next to the others by default.
        /// </summary>
        /// <returns>Returns the folder, from the project folder.</returns>
        private string GetDefaultFolder()
        {
            Object asset = !_activeCell.IsNone ? _activeCell.Asset : null;
            if (asset == null && _rows.Count > 0)
                asset = _rows[0].Asset;

            string path = asset != null ? AssetDatabase.GetAssetPath(asset) : null;
            string folder = !string.IsNullOrEmpty(path) ? Path.GetDirectoryName(path)?.Replace('\\', '/') : null;
            return !string.IsNullOrEmpty(folder) && AssetDatabase.IsValidFolder(folder)
                ? folder
                : FallbackFolder;
        }

        #endregion

    }

}
