using System.Collections.Generic;

using UnityEngine.UIElements;
using UnityEditor;

using Object = UnityEngine.Object;

namespace SideXP.Database.EditorOnly
{

    /// <summary>
    /// The selection sync of the table view, an option disabled by default: selecting cells selects the assets of their rows in Unity,
    /// so the Inspector shows them, and the rows whose assets are selected elsewhere are highlighted.
    /// </summary>
    /// <remarks>
    /// The selection of the table is given to Unity on the next update of the view, so several changes cause a single selection
    /// change, and only when a drag selection ends, so the Inspector isn't rebuilt on each move of the pointer. Selecting assets
    /// elsewhere never changes the selection of the table: their rows are only highlighted.
    /// </remarks>
    internal partial class TableView
    {

        #region Fields

        private const string SyncedRowCellClass = "database-table-view__cell--synced";

        /// <summary>
        /// Checks if the selection of the table is synced with the selection of Unity.
        /// </summary>
        private bool _isSelectionSynced = false;

        /// <summary>
        /// Checks if the view listens to the changes of the selection of Unity: while the sync is enabled and the view is attached to a
        /// panel.
        /// </summary>
        private bool _isListeningToUnitySelection = false;

        /// <summary>
        /// Checks if the selection of the table is given to Unity on the next update of the view.
        /// </summary>
        private bool _isUnitySelectionUpdateScheduled = false;

        /// <summary>
        /// The assets selected in Unity, whose rows are highlighted while the sync is enabled.
        /// </summary>
        private HashSet<Object> _unitySelectedAssets = new HashSet<Object>();

        #endregion


        #region Public API

        /// <inheritdoc cref="_isSelectionSynced"/>
        public bool IsSelectionSynced => _isSelectionSynced;

        /// <summary>
        /// Enables or disables the selection sync. Enabling it gives the selection of the table to Unity right away, if cells are
        /// selected.
        /// </summary>
        /// <param name="isSynced">If enabled, the selection of the table is synced with the selection of Unity.</param>
        public void SetSelectionSynced(bool isSynced)
        {
            if (isSynced == _isSelectionSynced)
                return;

            _isSelectionSynced = isSynced;
            UpdateUnitySelectionListening(panel != null);
            if (isSynced)
                ScheduleUnitySelectionUpdate();

            UpdateCellHighlights();
        }

        #endregion


        #region Private API

        /// <summary>
        /// Starts or stops listening to the selection of Unity, depending on the option and on the panel of the view. Reads the
        /// selection of Unity when it starts.
        /// </summary>
        /// <param name="isAttached">Checks if the view is attached to a panel. Given by the caller, since the view still has its panel
        /// when it receives <see cref="DetachFromPanelEvent"/>.</param>
        private void UpdateUnitySelectionListening(bool isAttached)
        {
            bool shouldListen = _isSelectionSynced && isAttached;
            if (shouldListen == _isListeningToUnitySelection)
                return;

            _isListeningToUnitySelection = shouldListen;
            if (shouldListen)
            {
                Selection.selectionChanged += OnUnitySelectionChanged;
                ReadUnitySelection();
            }
            else
            {
                Selection.selectionChanged -= OnUnitySelectionChanged;
                _unitySelectedAssets.Clear();
            }
        }

        /// <summary>
        /// Called when the selection of Unity changes, from this view or elsewhere. Highlights the rows of the selected assets.
        /// </summary>
        private void OnUnitySelectionChanged()
        {
            ReadUnitySelection();
            UpdateCellHighlights();
        }

        /// <summary>
        /// Stores the assets selected in Unity.
        /// </summary>
        private void ReadUnitySelection()
        {
            _unitySelectedAssets.Clear();
            foreach (Object selected in Selection.objects)
            {
                if (selected != null)
                    _unitySelectedAssets.Add(selected);
            }
        }

        /// <summary>
        /// Gives the selection of the table to Unity on the next update of the view, if the sync is enabled. Several calls before that
        /// update cause a single selection change.
        /// </summary>
        private void ScheduleUnitySelectionUpdate()
        {
            if (!_isSelectionSynced || _isUnitySelectionUpdateScheduled)
                return;

            _isUnitySelectionUpdateScheduled = true;
            schedule.Execute(() =>
            {
                _isUnitySelectionUpdateScheduled = false;
                // The drag gives its selection when it ends
                if (_dragMode == DragSelectionMode.None)
                    UpdateUnitySelection();
            });
        }

        /// <summary>
        /// Selects in Unity the assets of the rows that have at least one selected cell, in the order of the rows. Does nothing if no
        /// cell is selected, so leaving a table doesn't clear the selection of Unity.
        /// </summary>
        private void UpdateUnitySelection()
        {
            if (!_isSelectionSynced || _pipeline == null || _selection.IsEmpty)
                return;

            HashSet<Object> selectedAssets = new HashSet<Object>();
            foreach (CellId cell in _selection.Cells)
            {
                if (cell.Asset != null)
                    selectedAssets.Add(cell.Asset);
            }

            List<Object> assets = new List<Object>(selectedAssets.Count);
            foreach (TableRow row in _rows)
            {
                if (selectedAssets.Contains(row.Asset))
                    assets.Add(row.Asset);
            }

            // Unity raises a selection change even for the same objects, which rebuilds the Inspector
            if (assets.Count == _unitySelectedAssets.Count && _unitySelectedAssets.SetEquals(assets))
                return;

            Selection.objects = assets.ToArray();
        }

        /// <summary>
        /// Checks if the row of a cell is highlighted because its asset is selected in Unity.
        /// </summary>
        /// <param name="row">The row of the cell.</param>
        /// <returns>Returns true if the sync is enabled and the asset of the row is selected.</returns>
        private bool IsSyncedRow(TableRow row)
        {
            return _isSelectionSynced && row.Asset != null && _unitySelectedAssets.Contains(row.Asset);
        }

        #endregion

    }

}
