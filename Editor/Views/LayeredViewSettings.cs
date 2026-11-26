using System;
using System.Collections.Generic;

using UnityEngine;

namespace SideXP.Database.EditorOnly
{

    /// <summary>
    /// The display settings of a table, resolved from layers of settings (see <see cref="ViewSettings"/>): each setting comes from the
    /// first layer that sets it, from the top, or from the defaults of the table if no layer sets it.
    /// </summary>
    /// <remarks>
    /// The changes are written to the top layer only. A change that gives a setting the value of the layers below unsets it in the top
    /// layer instead, so the top layer only holds what differs from the layers below (which the personal overrides of a project view
    /// need). For now the only layer is a personal view, above the defaults of the table.
    /// <para>
    /// The defaults of the table: the default order of the columns, their default widths, every column visible except the ones the
    /// table hides by default, no sort, no filter, and the whole project as the folder scope.
    /// </para>
    /// </remarks>
    internal class LayeredViewSettings
    {

        #region Fields

        /// <summary>
        /// The layers, from the top. The first one is the layer that receives the changes.
        /// </summary>
        private readonly List<ViewSettings> _layers = null;

        /// <summary>
        /// Checks if a column is hidden when no layer sets its visibility, or null if all the columns are visible by default.
        /// </summary>
        private readonly Predicate<string> _isHiddenByDefault = null;

        #endregion


        #region Lifecycle

        /// <inheritdoc cref="LayeredViewSettings"/>
        /// <param name="layers"><inheritdoc cref="_layers" path="/summary"/></param>
        /// <param name="isHiddenByDefault"><inheritdoc cref="_isHiddenByDefault" path="/summary"/></param>
        /// <exception cref="ArgumentException">Thrown if there is no layer, or if a layer is null.</exception>
        public LayeredViewSettings(IReadOnlyList<ViewSettings> layers, Predicate<string> isHiddenByDefault = null)
        {
            if (layers == null || layers.Count == 0)
                throw new ArgumentException("The settings need at least one layer.", nameof(layers));

            _layers = new List<ViewSettings>(layers);
            if (_layers.Contains(null))
                throw new ArgumentException("A layer of the settings can't be null.", nameof(layers));

            _isHiddenByDefault = isHiddenByDefault;
        }

        #endregion


        #region Public API

        /// <summary>
        /// The layer that receives the changes.
        /// </summary>
        public ViewSettings Top => _layers[0];

        /// <summary>
        /// Unsets the settings of the columns and the sort in the top layer, so they come from the layers below (see
        /// <see cref="ViewSettings.Reset"/>).
        /// </summary>
        /// <returns>Returns true if the top layer had settings to unset.</returns>
        public bool Reset()
        {
            return Top.Reset();
        }

        #endregion


        #region Columns

        /// <summary>
        /// Gets the display order of the columns of the table, hidden columns included.
        /// </summary>
        /// <remarks>
        /// The order comes from the first layer that sets it. The columns it doesn't contain (for example a field added to a type)
        /// come right after the column that precedes them in the default order (or first if they are first), so they appear next to
        /// their neighbors.
        /// </remarks>
        /// <param name="columnIds">The identifiers of the columns of the table, in their default order.</param>
        /// <returns>Returns the identifiers of the columns of the table, in their display order. Columns that are not in the given list are
        /// not included.</returns>
        public List<string> GetColumnOrder(IReadOnlyList<string> columnIds)
        {
            HashSet<string> remainingIds = new HashSet<string>(columnIds);
            // Removing the id also skips the columns that don't exist anymore
            return GetFullOrder(columnIds, 0).FindAll(columnId => remainingIds.Remove(columnId));
        }

        /// <summary>
        /// Changes the order of some columns: they take the places of the same columns in the current order, so the other columns (for
        /// example the frozen column, or columns that don't exist anymore) keep theirs.
        /// </summary>
        /// <example>
        /// With the order <c>A, B, C, D</c>, reordering <c>D, B</c> gives <c>A, D, C, B</c>.
        /// </example>
        /// <param name="columnIds">The identifiers of the columns of the table, in their default order.</param>
        /// <param name="reorderedIds">The identifiers of the reordered columns, in their new order. Columns that are not in the current
        /// order are ignored.</param>
        /// <returns>Returns true if the order has changed.</returns>
        public bool SetColumnOrder(IReadOnlyList<string> columnIds, IReadOnlyList<string> reorderedIds)
        {
            List<string> order = GetFullOrder(columnIds, 0);
            List<int> places = new List<int>(reorderedIds.Count);
            List<string> reorderedColumns = new List<string>(reorderedIds.Count);
            foreach (string columnId in reorderedIds)
            {
                int index = order.IndexOf(columnId);
                if (index < 0 || places.Contains(index))
                    continue;

                places.Add(index);
                reorderedColumns.Add(columnId);
            }

            // The places are filled in the order of the list, so they must be sorted
            places.Sort();
            bool hasChanged = false;
            for (int i = 0; i < places.Count; i++)
            {
                if (order[places[i]] == reorderedColumns[i])
                    continue;

                order[places[i]] = reorderedColumns[i];
                hasChanged = true;
            }

            if (!hasChanged)
                return false;

            bool isSameAsBelow = ViewSettings.AreSame(order, GetFullOrder(columnIds, 1));
            Top.SetColumnOrder(isSameAsBelow ? null : order);
            return true;
        }

        /// <summary>
        /// Gets the width of a column chosen by the user.
        /// </summary>
        /// <param name="columnId">The identifier of the column.</param>
        /// <returns>Returns the width of the column in pixels from the first layer that sets it, or 0 if the column has its default
        /// width.</returns>
        public float GetWidth(string columnId)
        {
            return GetWidth(columnId, 0);
        }

        /// <summary>
        /// Sets the width of a column chosen by the user.
        /// </summary>
        /// <param name="columnId">The identifier of the column.</param>
        /// <param name="width">The width of the column, in pixels. If 0 or negative, the column gets the width of the layers below.</param>
        /// <returns>Returns true if the width has changed.</returns>
        public bool SetWidth(string columnId, float width)
        {
            width = Mathf.Max(0, width);
            if (Mathf.Approximately(GetWidth(columnId), width))
                return false;

            bool isSameAsBelow = Mathf.Approximately(GetWidth(columnId, 1), width);
            return Top.SetWidth(columnId, isSameAsBelow ? 0 : width);
        }

        /// <summary>
        /// Checks if a column is hidden.
        /// </summary>
        /// <param name="columnId">The identifier of the column.</param>
        /// <returns>Returns true if the first layer that sets the visibility of the column hides it, or if no layer sets it and the table
        /// hides the column by default.</returns>
        public bool IsHidden(string columnId)
        {
            return IsHidden(columnId, 0);
        }

        /// <summary>
        /// Hides or shows a column.
        /// </summary>
        /// <param name="columnId">The identifier of the column.</param>
        /// <param name="isHidden">If enabled, the column is hidden.</param>
        /// <returns>Returns true if the visibility of the column has changed.</returns>
        public bool SetHidden(string columnId, bool isHidden)
        {
            if (IsHidden(columnId) == isHidden)
                return false;

            ColumnVisibility visibility = isHidden ? ColumnVisibility.Hidden : ColumnVisibility.Visible;
            bool isSameAsBelow = IsHidden(columnId, 1) == isHidden;
            return Top.SetVisibility(columnId, isSameAsBelow ? ColumnVisibility.Unset : visibility);
        }

        #endregion


        #region Sort

        /// <summary>
        /// Gets the columns used to sort the rows, by priority, from the first layer that sets the sort.
        /// </summary>
        /// <param name="canSort"><inheritdoc cref="ViewSettings.GetSort" path="/param[@name='canSort']"/></param>
        /// <returns>Returns the sorted columns, by priority, or an empty list if the rows keep the order of the source.</returns>
        public List<ColumnSort> GetSort(Predicate<string> canSort = null)
        {
            return GetSort(canSort, 0);
        }

        /// <summary>
        /// Sorts the rows by several columns, replacing the current sort.
        /// </summary>
        /// <param name="sort">The columns used to sort the rows, by priority. If empty, the rows keep the order of the source.</param>
        /// <returns>Returns true if the sort has changed.</returns>
        public bool SetSort(IReadOnlyList<ColumnSort> sort)
        {
            if (ViewSettings.AreSame(sort, GetSort()))
                return false;

            if (ViewSettings.AreSame(sort, GetSort(null, 1)))
                return Top.UnsetSort();

            return Top.SetSort(sort);
        }

        #endregion


        #region Filters

        /// <summary>
        /// The root of the filter tree, from the first layer that sets the filters, or null if the rows are not filtered.
        /// </summary>
        /// <remarks>
        /// The tree is stored by reference: call <see cref="SetFilter"/> after editing its nodes, then save the settings.
        /// </remarks>
        public FilterNode Filter
        {
            get
            {
                ViewSettings layer = _layers.Find(item => item.IsFilterSet);
                return layer?.Filter;
            }
        }

        /// <summary>
        /// Sets the filters in the top layer, replacing the current ones.
        /// </summary>
        /// <remarks>
        /// The filters are always set, never unset: the trees of two layers are not compared, and the given tree may be the current
        /// one, edited in place.
        /// <para>
        /// @todo Step 23b: the filter editor edits the tree in place, so the tree of a project view must be copied into the top layer
        /// (see <see cref="FilterNode.Clone"/>) before it's edited, or the project view would change.
        /// </para>
        /// </remarks>
        /// <param name="filter">The root of the filter tree, or null to show all the rows.</param>
        public void SetFilter(FilterNode filter)
        {
            Top.SetFilter(filter);
        }

        #endregion


        #region Folders

        /// <summary>
        /// The folders of the folder scope, from the first layer that sets them, or an empty list for the whole project.
        /// </summary>
        public IReadOnlyList<string> Folders => GetFolders(0);

        /// <summary>
        /// Sets the folders of the folder scope.
        /// </summary>
        /// <param name="folders"><inheritdoc cref="ViewSettings.SetFolders" path="/param[@name='folders']"/></param>
        /// <returns>Returns true if the folders have changed.</returns>
        public bool SetFolders(IEnumerable<string> folders)
        {
            List<string> newFolders = folders != null ? new List<string>(folders) : new List<string>();
            if (ViewSettings.AreSame(newFolders, Folders))
                return false;

            if (ViewSettings.AreSame(newFolders, GetFolders(1)))
                return Top.UnsetFolders();

            return Top.SetFolders(newFolders);
        }

        #endregion


        #region Private API

        /// <summary>
        /// Gets the display order of the columns from a layer and the layers below it, with the columns that don't exist anymore.
        /// </summary>
        /// <param name="columnIds">The identifiers of the columns of the table, in their default order.</param>
        /// <param name="fromLayer">The index of the first layer to read. A layer out of range gives the default order.</param>
        /// <returns>Returns the order of the first layer that sets it (without its empty entries and duplicates), with the columns of the
        /// table it doesn't contain inserted after their predecessor in the default order.</returns>
        private List<string> GetFullOrder(IReadOnlyList<string> columnIds, int fromLayer)
        {
            List<string> order = new List<string>(columnIds.Count);
            ViewSettings layer = FindLayer(fromLayer, item => item.IsColumnOrderSet);
            if (layer != null)
            {
                foreach (string columnId in layer.ColumnOrder)
                {
                    // Skips the duplicates of a hand-edited file
                    if (!string.IsNullOrEmpty(columnId) && !order.Contains(columnId))
                        order.Add(columnId);
                }
            }

            for (int i = 0; i < columnIds.Count; i++)
            {
                if (order.Contains(columnIds[i]))
                    continue;

                // The previous column is in the order at this point, since the columns are processed in order
                int insertIndex = i > 0 ? order.IndexOf(columnIds[i - 1]) + 1 : 0;
                order.Insert(insertIndex, columnIds[i]);
            }

            return order;
        }

        /// <summary>
        /// Gets the width of a column from a layer and the layers below it.
        /// </summary>
        /// <param name="columnId">The identifier of the column.</param>
        /// <param name="fromLayer">The index of the first layer to read.</param>
        /// <returns>Returns the width of the first layer that sets it, or 0.</returns>
        private float GetWidth(string columnId, int fromLayer)
        {
            for (int i = fromLayer; i < _layers.Count; i++)
            {
                float width = _layers[i].GetWidth(columnId);
                if (width > 0)
                    return width;
            }

            return 0;
        }

        /// <summary>
        /// Checks if a column is hidden by a layer and the layers below it.
        /// </summary>
        /// <param name="columnId">The identifier of the column.</param>
        /// <param name="fromLayer">The index of the first layer to read.</param>
        /// <returns>Returns the visibility of the first layer that sets it, or the default visibility of the column.</returns>
        private bool IsHidden(string columnId, int fromLayer)
        {
            for (int i = fromLayer; i < _layers.Count; i++)
            {
                ColumnVisibility visibility = _layers[i].GetVisibility(columnId);
                if (visibility != ColumnVisibility.Unset)
                    return visibility == ColumnVisibility.Hidden;
            }

            return _isHiddenByDefault != null && _isHiddenByDefault(columnId);
        }

        /// <summary>
        /// Gets the sort of a layer and the layers below it.
        /// </summary>
        /// <param name="canSort"><inheritdoc cref="ViewSettings.GetSort" path="/param[@name='canSort']"/></param>
        /// <param name="fromLayer">The index of the first layer to read.</param>
        /// <returns>Returns the sort of the first layer that sets it, or an empty list.</returns>
        private List<ColumnSort> GetSort(Predicate<string> canSort, int fromLayer)
        {
            ViewSettings layer = FindLayer(fromLayer, item => item.IsSortSet);
            return layer != null ? layer.GetSort(canSort) : new List<ColumnSort>();
        }

        /// <summary>
        /// Gets the folders of a layer and the layers below it.
        /// </summary>
        /// <param name="fromLayer">The index of the first layer to read.</param>
        /// <returns>Returns the folders of the first layer that sets them, or an empty list.</returns>
        private IReadOnlyList<string> GetFolders(int fromLayer)
        {
            ViewSettings layer = FindLayer(fromLayer, item => item.AreFoldersSet);
            return layer != null ? layer.Folders : Array.Empty<string>();
        }

        /// <summary>
        /// Finds the first layer that sets a setting, from a given layer.
        /// </summary>
        /// <param name="fromLayer">The index of the first layer to check.</param>
        /// <param name="isSet">Checks if a layer sets the setting.</param>
        /// <returns>Returns the found layer, or null if no layer sets the setting.</returns>
        private ViewSettings FindLayer(int fromLayer, Predicate<ViewSettings> isSet)
        {
            for (int i = fromLayer; i < _layers.Count; i++)
            {
                if (isSet(_layers[i]))
                    return _layers[i];
            }

            return null;
        }

        #endregion

    }

}
