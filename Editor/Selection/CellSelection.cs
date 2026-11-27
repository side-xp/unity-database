using System;
using System.Collections.Generic;

using Object = UnityEngine.Object;

namespace SideXP.Database.EditorOnly
{

    /// <summary>
    /// A set of cells of a table, not necessarily next to each other. The cells are identified by their asset and their column (see
    /// <see cref="CellId"/>), so the selection survives sorting, filtering and reloading.
    /// </summary>
    /// <remarks>
    /// The selection remembers the cells it had before its last range (its base), like spreadsheet apps: a second range from the same
    /// anchor replaces the previous range instead of adding to it, and a range added with Ctrl-Shift-click is added to the base.
    /// </remarks>
    internal class CellSelection
    {

        #region Fields

        /// <summary>
        /// The selected cells.
        /// </summary>
        private HashSet<CellId> _cells = new HashSet<CellId>();

        /// <summary>
        /// The cells selected before the last range, to which an added range is added.
        /// </summary>
        private HashSet<CellId> _baseCells = new HashSet<CellId>();

        /// <summary>
        /// The number of assets that have at least one selected cell, or -1 if it must be counted again.
        /// </summary>
        private int _assetCount = 0;

        #endregion


        #region Public API

        /// <summary>
        /// The number of selected cells.
        /// </summary>
        public int Count => _cells.Count;

        /// <summary>
        /// Checks if no cell is selected.
        /// </summary>
        public bool IsEmpty => _cells.Count == 0;

        /// <summary>
        /// The selected cells, in no particular order.
        /// </summary>
        public IReadOnlyCollection<CellId> Cells => _cells;

        /// <summary>
        /// The number of assets that have at least one selected cell.
        /// </summary>
        public int AssetCount
        {
            get
            {
                if (_assetCount < 0)
                {
                    HashSet<Object> assets = new HashSet<Object>();
                    foreach (CellId cell in _cells)
                        assets.Add(cell.Asset);

                    _assetCount = assets.Count;
                }

                return _assetCount;
            }
        }

        /// <summary>
        /// Checks if a cell is selected.
        /// </summary>
        /// <param name="cell">The cell to check.</param>
        /// <returns>Returns true if the cell is selected.</returns>
        public bool Contains(CellId cell)
        {
            return _cells.Contains(cell);
        }

        /// <summary>
        /// Deselects all the cells.
        /// </summary>
        public void Clear()
        {
            _cells.Clear();
            _baseCells.Clear();
            _assetCount = 0;
        }

        /// <summary>
        /// Selects a single cell, and deselects the others (a click).
        /// </summary>
        /// <param name="cell">The cell to select.</param>
        public void Select(CellId cell)
        {
            _cells.Clear();
            _cells.Add(cell);
            SetBase();
        }

        /// <summary>
        /// Selects a group of cells, such as a whole row or a whole column, and deselects the others.
        /// </summary>
        /// <param name="cells">The cells to select.</param>
        public void Select(IEnumerable<CellId> cells)
        {
            _cells.Clear();
            _cells.UnionWith(cells);
            SetBase();
        }

        /// <summary>
        /// Checks if all the cells of a group are selected.
        /// </summary>
        /// <param name="cells">The cells to check.</param>
        /// <returns>Returns true if all the cells are selected, or if the group is empty.</returns>
        public bool ContainsAll(IEnumerable<CellId> cells)
        {
            return _cells.IsSupersetOf(cells);
        }

        /// <summary>
        /// Selects a group of cells, without changing the other cells.
        /// </summary>
        /// <param name="cells">The cells to select.</param>
        public void Add(IEnumerable<CellId> cells)
        {
            _cells.UnionWith(cells);
            SetBase();
        }

        /// <summary>
        /// Deselects a group of cells, without changing the other cells.
        /// </summary>
        /// <param name="cells">The cells to deselect.</param>
        public void Remove(IEnumerable<CellId> cells)
        {
            _cells.ExceptWith(cells);
            SetBase();
        }

        /// <summary>
        /// Selects a cell if it's not selected, and deselects it otherwise, without changing the other cells (a Ctrl-click).
        /// </summary>
        /// <param name="cell">The cell to select or deselect.</param>
        /// <returns>Returns true if the cell is now selected, false if it has been deselected.</returns>
        public bool Toggle(CellId cell)
        {
            bool isSelected = _cells.Add(cell);
            if (!isSelected)
                _cells.Remove(cell);

            SetBase();
            return isSelected;
        }

        /// <summary>
        /// Selects a range of cells (see <see cref="GetRange"/>), which replaces the previous range.
        /// </summary>
        /// <param name="range">The cells of the range.</param>
        /// <param name="isAdded">If enabled, the range is added to the cells selected before the last range (a Ctrl-Shift-click).
        /// Otherwise, it replaces all the selected cells (a Shift-click).</param>
        public void SelectRange(IEnumerable<CellId> range, bool isAdded)
        {
            if (isAdded)
            {
                _cells = new HashSet<CellId>(_baseCells);
            }
            else
            {
                _cells.Clear();
                _baseCells.Clear();
            }

            _cells.UnionWith(range);
            _assetCount = -1;
        }

        /// <summary>
        /// Deselects the cells that match a condition, for example the cells of the rows that are not visible anymore.
        /// </summary>
        /// <param name="match">The condition of the cells to deselect.</param>
        /// <returns>Returns the number of deselected cells.</returns>
        public int RemoveAll(Predicate<CellId> match)
        {
            _baseCells.RemoveWhere(match);
            int removedCount = _cells.RemoveWhere(match);
            if (removedCount > 0)
                _assetCount = -1;

            return removedCount;
        }

        /// <summary>
        /// Creates a copy of this selection, which can be restored later with <see cref="CopyFrom(CellSelection)"/>.
        /// </summary>
        /// <returns>Returns the created copy.</returns>
        public CellSelection Clone()
        {
            CellSelection copy = new CellSelection();
            copy.CopyFrom(this);
            return copy;
        }

        /// <summary>
        /// Replaces the cells of this selection with the cells of another one.
        /// </summary>
        /// <param name="other">The selection to copy.</param>
        public void CopyFrom(CellSelection other)
        {
            _cells = new HashSet<CellId>(other._cells);
            _baseCells = new HashSet<CellId>(other._baseCells);
            _assetCount = other._assetCount;
        }

        /// <summary>
        /// Gets the cells of the range between two cells: the rectangle of the rows and the columns between them, both included.
        /// </summary>
        /// <param name="rowAssets">The assets of the rows, in their display order.</param>
        /// <param name="columnIds">The identifiers of the columns, in their display order. The hidden columns are included, since hiding
        /// a column is only a display choice: a range that spans them includes their cells.</param>
        /// <param name="from">The cell where the range starts (the active cell).</param>
        /// <param name="to">The cell where the range ends (the clicked cell).</param>
        /// <returns>Returns the cells of the range, row by row. If the start cell is not found, the range is the end cell only. If the
        /// end cell is not found, the range is empty.</returns>
        public static List<CellId> GetRange(IReadOnlyList<Object> rowAssets, IReadOnlyList<string> columnIds, CellId from, CellId to)
        {
            List<CellId> range = new List<CellId>();
            int toRow = IndexOfAsset(rowAssets, to.Asset);
            int toColumn = IndexOfColumn(columnIds, to.ColumnId);
            if (to.IsNone || toRow < 0 || toColumn < 0)
                return range;

            int fromRow = from.IsNone ? -1 : IndexOfAsset(rowAssets, from.Asset);
            int fromColumn = from.IsNone ? -1 : IndexOfColumn(columnIds, from.ColumnId);
            if (fromRow < 0 || fromColumn < 0)
            {
                range.Add(to);
                return range;
            }

            int firstRow = Math.Min(fromRow, toRow);
            int lastRow = Math.Max(fromRow, toRow);
            int firstColumn = Math.Min(fromColumn, toColumn);
            int lastColumn = Math.Max(fromColumn, toColumn);
            for (int row = firstRow; row <= lastRow; row++)
            {
                for (int column = firstColumn; column <= lastColumn; column++)
                    range.Add(new CellId(rowAssets[row], columnIds[column]));
            }

            return range;
        }

        #endregion


        #region Private API

        /// <summary>
        /// Makes the selected cells the base of the next added range.
        /// </summary>
        private void SetBase()
        {
            _baseCells = new HashSet<CellId>(_cells);
            _assetCount = -1;
        }

        /// <summary>
        /// Finds the row of an asset.
        /// </summary>
        /// <param name="rowAssets">The assets of the rows.</param>
        /// <param name="asset">The asset to find, compared by reference like in <see cref="CellId"/>.</param>
        /// <returns>Returns the index of the row, or -1 if it's not found.</returns>
        private static int IndexOfAsset(IReadOnlyList<Object> rowAssets, Object asset)
        {
            for (int i = 0; i < rowAssets.Count; i++)
            {
                if (ReferenceEquals(rowAssets[i], asset))
                    return i;
            }

            return -1;
        }

        /// <summary>
        /// Finds a column by its identifier.
        /// </summary>
        /// <param name="columnIds">The identifiers of the columns.</param>
        /// <param name="columnId">The identifier to find.</param>
        /// <returns>Returns the index of the column, or -1 if it's not found.</returns>
        private static int IndexOfColumn(IReadOnlyList<string> columnIds, string columnId)
        {
            for (int i = 0; i < columnIds.Count; i++)
            {
                if (columnIds[i] == columnId)
                    return i;
            }

            return -1;
        }

        #endregion

    }

}
