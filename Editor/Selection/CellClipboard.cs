using System.Collections.Generic;

using Object = UnityEngine.Object;

namespace SideXP.Database.EditorOnly
{

    /// <summary>
    /// The rules of the copy and paste of cells: which selections can be copied, and which cells receive pasted values.
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item>A copy needs a grid: all the selected rows have the same selected columns. The copied values are the selected rows and
    /// columns in display order, without the unselected ones between them.</item>
    /// <item>A single pasted value goes to all the selected cells.</item>
    /// <item>A grid of pasted values goes to a grid selection of the same size, or starts from the selected cell when a single cell is
    /// selected (filling the next columns and rows in display order).</item>
    /// </list>
    /// The rows and the columns are given in display order: the visible rows (filtered and sorted), and all the columns that hold
    /// values, hidden ones included, since hiding a column is only a display choice.
    /// </remarks>
    internal static class CellClipboard
    {

        #region Subclasses

        /// <summary>
        /// A cell that receives a pasted value.
        /// </summary>
        public readonly struct PasteTarget
        {

            /// <summary>
            /// The cell that receives the value.
            /// </summary>
            public readonly CellId Cell;

            /// <summary>
            /// The pasted text, converted by the handler of the column of the cell.
            /// </summary>
            public readonly string Text;

            /// <inheritdoc cref="PasteTarget"/>
            /// <param name="cell"><inheritdoc cref="Cell" path="/summary"/></param>
            /// <param name="text"><inheritdoc cref="Text" path="/summary"/></param>
            public PasteTarget(CellId cell, string text)
            {
                Cell = cell;
                Text = text;
            }

        }

        #endregion


        #region Fields

        /// <summary>
        /// The message of a copy refused because the selection doesn't form a grid.
        /// </summary>
        public const string NotGridMessage = "The selection can't be copied: all its rows must have the same selected columns.";

        /// <summary>
        /// The message of a paste refused because the clipboard is empty.
        /// </summary>
        public const string NothingToPasteMessage = "The clipboard has no value to paste.";

        /// <summary>
        /// The message of a paste refused because no cell that can receive a value is selected.
        /// </summary>
        public const string NoSelectionMessage = "Select the cells that receive the pasted values first.";

        /// <summary>
        /// The message of a paste refused because the pasted grid goes beyond the last row or column from the selected cell.
        /// </summary>
        public const string NotFittingMessage = "The copied values don't fit in the table from the selected cell.";

        #endregion


        #region Public API

        /// <summary>
        /// Gets the rows and the columns of a selection that forms a grid: all its rows have the same selected columns.
        /// </summary>
        /// <param name="cells">The selected cells. The cells of the rows and columns that are not given are ignored.</param>
        /// <param name="rowAssets">The assets of the rows, in display order.</param>
        /// <param name="columnIds">The identifiers of the columns, in display order.</param>
        /// <param name="gridRows">Outputs the selected rows, in display order.</param>
        /// <param name="gridColumns">Outputs the selected columns, in display order.</param>
        /// <returns>Returns true if the selection forms a grid, false if it doesn't or if it's empty.</returns>
        public static bool TryGetGrid(IEnumerable<CellId> cells, IReadOnlyList<Object> rowAssets, IReadOnlyList<string> columnIds,
            out List<Object> gridRows, out List<string> gridColumns)
        {
            gridRows = new List<Object>();
            gridColumns = new List<string>();

            // The selected columns of each row, by index in the display order
            Dictionary<int, HashSet<int>> columnsPerRow = new Dictionary<int, HashSet<int>>();
            Dictionary<Object, int> rowIndices = GetRowIndices(rowAssets);
            Dictionary<string, int> columnIndices = GetColumnIndices(columnIds);
            foreach (CellId cell in cells)
            {
                if (cell.Asset == null || !rowIndices.TryGetValue(cell.Asset, out int rowIndex)
                    || !columnIndices.TryGetValue(cell.ColumnId, out int columnIndex))
                {
                    continue;
                }

                if (!columnsPerRow.TryGetValue(rowIndex, out HashSet<int> columns))
                {
                    columns = new HashSet<int>();
                    columnsPerRow.Add(rowIndex, columns);
                }

                columns.Add(columnIndex);
            }

            if (columnsPerRow.Count == 0)
                return false;

            HashSet<int> firstColumns = null;
            foreach (HashSet<int> columns in columnsPerRow.Values)
            {
                if (firstColumns == null)
                    firstColumns = columns;
                else if (!firstColumns.SetEquals(columns))
                    return false;
            }

            List<int> sortedRows = new List<int>(columnsPerRow.Keys);
            sortedRows.Sort();
            foreach (int rowIndex in sortedRows)
                gridRows.Add(rowAssets[rowIndex]);

            List<int> sortedColumns = new List<int>(firstColumns);
            sortedColumns.Sort();
            foreach (int columnIndex in sortedColumns)
                gridColumns.Add(columnIds[columnIndex]);

            return true;
        }

        /// <summary>
        /// Gets the cells that receive pasted values.
        /// </summary>
        /// <param name="values">The pasted values, row by row. Rows shorter than the longest one are completed with empty values.</param>
        /// <param name="cells">The selected cells.</param>
        /// <param name="rowAssets">The assets of the rows, in display order.</param>
        /// <param name="columnIds">The identifiers of the columns that can receive values, in display order.</param>
        /// <param name="targets">Outputs the cells that receive a value, with their value.</param>
        /// <param name="error">Outputs the reason why the values can't be pasted, or null if they can.</param>
        /// <returns>Returns true if the values can be pasted.</returns>
        public static bool TryGetPasteTargets(IReadOnlyList<IReadOnlyList<string>> values, IReadOnlyCollection<CellId> cells,
            IReadOnlyList<Object> rowAssets, IReadOnlyList<string> columnIds, out List<PasteTarget> targets, out string error)
        {
            targets = new List<PasteTarget>();
            error = null;

            int height = values.Count;
            int width = 0;
            foreach (IReadOnlyList<string> row in values)
                width = row.Count > width ? row.Count : width;

            if (height == 0 || width == 0)
            {
                error = NothingToPasteMessage;
                return false;
            }

            // Only the cells of the given rows and columns: the Controls column has no value to receive
            List<CellId> selectedCells = new List<CellId>();
            Dictionary<Object, int> rowIndices = GetRowIndices(rowAssets);
            Dictionary<string, int> columnIndices = GetColumnIndices(columnIds);
            foreach (CellId cell in cells)
            {
                if (cell.Asset != null && rowIndices.ContainsKey(cell.Asset) && columnIndices.ContainsKey(cell.ColumnId))
                    selectedCells.Add(cell);
            }

            if (selectedCells.Count == 0)
            {
                error = NoSelectionMessage;
                return false;
            }

            // A single value goes to all the selected cells
            if (height == 1 && width == 1)
            {
                foreach (CellId cell in selectedCells)
                    targets.Add(new PasteTarget(cell, values[0][0]));

                return true;
            }

            // From a single cell, the values fill the next rows and columns
            if (selectedCells.Count == 1)
            {
                int firstRow = rowIndices[selectedCells[0].Asset];
                int firstColumn = columnIndices[selectedCells[0].ColumnId];
                if (firstRow + height > rowAssets.Count || firstColumn + width > columnIds.Count)
                {
                    error = NotFittingMessage;
                    return false;
                }

                for (int row = 0; row < height; row++)
                {
                    for (int column = 0; column < width; column++)
                    {
                        CellId cell = new CellId(rowAssets[firstRow + row], columnIds[firstColumn + column]);
                        targets.Add(new PasteTarget(cell, GetValue(values, row, column)));
                    }
                }

                return true;
            }

            // On a grid selection, the values go to the cells in the same place
            if (!TryGetGrid(selectedCells, rowAssets, columnIds, out List<Object> gridRows, out List<string> gridColumns)
                || gridRows.Count != height || gridColumns.Count != width)
            {
                error = $"The copied values ({height} rows x {width} columns) can't be pasted: select a single cell, or a grid of the " +
                    "same size.";
                return false;
            }

            for (int row = 0; row < height; row++)
            {
                for (int column = 0; column < width; column++)
                    targets.Add(new PasteTarget(new CellId(gridRows[row], gridColumns[column]), GetValue(values, row, column)));
            }

            return true;
        }

        #endregion


        #region Private API

        /// <summary>
        /// Gets the index of each row by its asset.
        /// </summary>
        /// <param name="rowAssets">The assets of the rows, in display order.</param>
        /// <returns>Returns the indices of the rows.</returns>
        private static Dictionary<Object, int> GetRowIndices(IReadOnlyList<Object> rowAssets)
        {
            Dictionary<Object, int> indices = new Dictionary<Object, int>(rowAssets.Count);
            for (int i = 0; i < rowAssets.Count; i++)
                indices[rowAssets[i]] = i;

            return indices;
        }

        /// <summary>
        /// Gets the index of each column by its identifier.
        /// </summary>
        /// <param name="columnIds">The identifiers of the columns, in display order.</param>
        /// <returns>Returns the indices of the columns.</returns>
        private static Dictionary<string, int> GetColumnIndices(IReadOnlyList<string> columnIds)
        {
            Dictionary<string, int> indices = new Dictionary<string, int>(columnIds.Count);
            for (int i = 0; i < columnIds.Count; i++)
                indices[columnIds[i]] = i;

            return indices;
        }

        /// <summary>
        /// Gets a pasted value, or an empty value if its row is shorter than the others.
        /// </summary>
        /// <param name="values">The pasted values, row by row.</param>
        /// <param name="row">The index of the row of the value.</param>
        /// <param name="column">The index of the column of the value.</param>
        /// <returns>Returns the value.</returns>
        private static string GetValue(IReadOnlyList<IReadOnlyList<string>> values, int row, int column)
        {
            return column < values[row].Count ? values[row][column] : string.Empty;
        }

        #endregion

    }

}
