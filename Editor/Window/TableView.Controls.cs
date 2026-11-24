using System;

using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;

using SideXP.Core.EditorOnly;

using Object = UnityEngine.Object;

namespace SideXP.Database.EditorOnly
{

    /// <summary>
    /// The <i>Controls</i> column of the table view: the buttons of the operations on the asset of each row (select, duplicate,
    /// delete).
    /// </summary>
    /// <remarks>
    /// The buttons are live controls (see <see cref="CellHandler.SetLiveControl(VisualElement)"/>): clicking them doesn't change the
    /// active cell nor the focus. Their cell element is reused for other rows when the table scrolls, so a button reads the current
    /// row of its cell when it's clicked.
    /// </remarks>
    internal partial class TableView
    {

        #region Fields

        private const string ControlsClass = "database-cell__controls";
        private const string ControlButtonClass = "database-cell__control-button";

        private const string SelectTooltip = "Selects the asset, and shows it in the Project window.";
        private const string DuplicateTooltip = "Duplicates the asset: next to the original for a main asset, in the same file for a " +
            "sub-asset.";
        private const string DeleteTooltip = "Deletes the asset, after a confirmation: a main asset is moved to the trash, a sub-asset " +
            "is removed from its file.";

        private const string DeleteDialogTitle = "Delete Asset";
        private const string DeleteDialogConfirm = "Delete";
        private const string DeleteDialogCancel = "Cancel";

        #endregion


        #region UI

        /// <summary>
        /// Creates the buttons of a cell of the <i>Controls</i> column.
        /// </summary>
        /// <param name="cell">The element of the cell, which holds the buttons.</param>
        /// <returns>Returns the element that holds the buttons.</returns>
        private VisualElement CreateControls(VisualElement cell)
        {
            VisualElement controls = new VisualElement();
            controls.AddToClassList(ControlsClass);
            controls.Add(CreateControlButton(cell, DatabaseIcons.Select, SelectTooltip, SelectAsset));
            controls.Add(CreateControlButton(cell, DatabaseIcons.Duplicate, DuplicateTooltip, DuplicateAsset));
            controls.Add(CreateControlButton(cell, DatabaseIcons.Delete, DeleteTooltip, DeleteAsset));
            return controls;
        }

        /// <summary>
        /// Creates a button of the <i>Controls</i> column.
        /// </summary>
        /// <param name="cell">The element of the cell of the button.</param>
        /// <param name="iconName">The name of the icon of the button, one of the constants of <see cref="DatabaseIcons"/>.</param>
        /// <param name="tooltip">The tooltip of the button, which says what it does.</param>
        /// <param name="operation">The operation to apply to the row of the cell when the button is clicked.</param>
        /// <returns>Returns the created button.</returns>
        private Button CreateControlButton(VisualElement cell, string iconName, string tooltip, Action<TableRow> operation)
        {
            Button button = new Button(() => OnControlClicked(cell, operation)) { tooltip = tooltip };
            button.AddToClassList(ControlButtonClass);
            DatabaseElements.SetIcon(button, iconName);
            CellHandler.SetLiveControl(button);
            return button;
        }

        #endregion


        #region Operations

        /// <summary>
        /// Called when a button of the <i>Controls</i> column is clicked. Applies its operation to the current row of its cell.
        /// </summary>
        /// <param name="cell">The element of the cell of the button.</param>
        /// <param name="operation">The operation of the button.</param>
        private void OnControlClicked(VisualElement cell, Action<TableRow> operation)
        {
            // Read before the edit ends: ending it refreshes the rows, which can rebind this element to another row
            if (cell.userData is not CellBinding binding || binding.Row == null || binding.Row.Asset == null)
                return;

            TableRow row = binding.Row;
            // Like a click elsewhere: an edit in progress is confirmed first, since the operation may move or delete its row
            EndEdit(true, false);
            if (row.Asset != null)
                operation(row);
        }

        /// <summary>
        /// Selects the asset of a row, and shows it in the Project window.
        /// </summary>
        /// <param name="row">The row of the asset.</param>
        private void SelectAsset(TableRow row)
        {
            EditorHelpers.FocusObject(row.Asset);
        }

        /// <summary>
        /// Duplicates the asset of a row. The copy becomes the active cell (see <see cref="ShowNewAsset(Object)"/>).
        /// </summary>
        /// <param name="row">The row of the asset.</param>
        private void DuplicateAsset(TableRow row)
        {
            if (!AssetOperations.TryDuplicate(row.Asset, out Object copy, out string error))
            {
                Debug.LogWarning($"{DatabaseConstants.LogPrefix}The asset \"{row.Asset.name}\" can't be duplicated: {error}");
                return;
            }

            ShowNewAsset(copy);
        }

        /// <summary>
        /// Shows an asset just added to the table (duplicated or created): clears the search if it hides the asset, makes it the active
        /// cell, on its name if the <i>Asset Name</i> column is displayed, scrolls to it, and focuses the view, so it can be renamed
        /// right away with F2 or by typing.
        /// </summary>
        /// <param name="asset">The new asset.</param>
        private void ShowNewAsset(Object asset)
        {
            // The change tracker also reports the new asset, but it must be in the table to become the active cell
            Reload();
            // A new asset whose name doesn't match the search would be hidden right away
            ResetSearchIfHidden(asset);

            string columnId = _columns.Exists(column => column.IsAssetName)
                ? TableViewColumn.AssetNameId
                : TableViewColumn.ControlsId;

            SetActiveCell(new CellId(asset, columnId));
            ScrollToAsset(asset);
            Focus();
        }

        /// <summary>
        /// Deletes the asset of a row, after a confirmation dialog.
        /// </summary>
        /// <param name="row">The row of the asset.</param>
        private void DeleteAsset(TableRow row)
        {
            Object asset = row.Asset;
            if (!EditorUtility.DisplayDialog(DeleteDialogTitle, GetDeleteMessage(asset), DeleteDialogConfirm, DeleteDialogCancel))
                return;

            string assetName = asset.name;
            if (!AssetOperations.TryDelete(asset, out string error))
            {
                Debug.LogWarning($"{DatabaseConstants.LogPrefix}The asset \"{assetName}\" can't be deleted: {error}");
                return;
            }

            // Removes the row right away, and the active cell if it was on the deleted asset
            Reload();
        }

        /// <summary>
        /// Gets the message of the confirmation dialog of a deletion, which says what happens to the asset and its file.
        /// </summary>
        /// <param name="asset">The asset to delete.</param>
        /// <returns>Returns the message of the dialog.</returns>
        private static string GetDeleteMessage(Object asset)
        {
            string path = AssetDatabase.GetAssetPath(asset);
            if (AssetDatabase.IsSubAsset(asset))
                return $"Delete the sub-asset \"{asset.name}\" from {path}?\n\nThis can be undone with Edit > Undo.";

            int subAssetCount = AssetOperations.CountSubAssets(asset);
            string subAssetsText = subAssetCount switch
            {
                0 => string.Empty,
                1 => "\n\nIts sub-asset is deleted with it.",
                _ => $"\n\nIts {subAssetCount} sub-assets are deleted with it.",
            };

            return $"Delete \"{asset.name}\"?\n\nThe file {path} is moved to the trash.{subAssetsText}";
        }

        /// <summary>
        /// Scrolls the table so the row of an asset is visible. Does nothing if the asset has no visible row.
        /// </summary>
        /// <remarks>
        /// Only the main list is scrolled: the frozen list follows it (see <see cref="FrozenColumnLink"/>).
        /// </remarks>
        /// <param name="asset">The asset of the row to show.</param>
        private void ScrollToAsset(Object asset)
        {
            if (_pipeline == null || !_pipeline.TryGetRow(asset, out TableRow row))
                return;

            int index = _rows.IndexOf(row);
            if (index < 0)
                return;

            // Deferred, since the list view hasn't laid out its refreshed rows yet
            schedule.Execute(() =>
            {
                if (_listView != null && index < _rows.Count && _rows[index] == row)
                    _listView.ScrollToItem(index);
            });
        }

        #endregion

    }

}
