using System.Collections.Generic;

using NUnit.Framework;

using UnityEngine;

using SideXP.Database.EditorOnly;

using Object = UnityEngine.Object;

namespace SideXP.Database.Tests
{

    /// <summary>
    /// Tests for <see cref="CellClipboard"/>: the selections that can be copied, and the cells that receive pasted values.
    /// </summary>
    public class CellClipboardTests
    {

        #region Fields

        private const string FirstColumn = "$AssetName";
        private const string SecondColumn = "_number";
        private const string ThirdColumn = "_label";

        /// <summary>
        /// The number of test rows: enough to paste a grid that fits, and one that doesn't.
        /// </summary>
        private const int RowCount = 4;

        /// <summary>
        /// The in-memory assets of the rows, created for each test.
        /// </summary>
        private List<Object> _rows = new List<Object>();

        /// <summary>
        /// The identifiers of the columns, in their display order.
        /// </summary>
        private List<string> _columns = new List<string> { FirstColumn, SecondColumn, ThirdColumn };

        #endregion


        #region Lifecycle

        /// <summary>
        /// Creates the assets of the rows.
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            _rows.Clear();
            for (int i = 0; i < RowCount; i++)
                _rows.Add(ScriptableObject.CreateInstance<PipelineTestSO>());
        }

        /// <summary>
        /// Destroys the assets of the rows.
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            foreach (Object row in _rows)
            {
                if (row != null)
                    Object.DestroyImmediate(row);
            }

            _rows.Clear();
        }

        #endregion


        #region Copy

        /// <summary>
        /// A selection whose rows have the same columns is a grid, in display order, without the unselected columns between them.
        /// </summary>
        [Test]
        public void TryGetGrid_SameColumnsPerRow_GridInDisplayOrder()
        {
            List<CellId> cells = new List<CellId>
            {
                Cell(2, ThirdColumn), Cell(0, FirstColumn), Cell(2, FirstColumn), Cell(0, ThirdColumn),
            };

            Assert.IsTrue(CellClipboard.TryGetGrid(cells, _rows, _columns, out List<Object> gridRows, out List<string> gridColumns));
            CollectionAssert.AreEqual(new[] { _rows[0], _rows[2] }, gridRows);
            CollectionAssert.AreEqual(new[] { FirstColumn, ThirdColumn }, gridColumns);
        }

        /// <summary>
        /// A selection whose rows have different columns is not a grid, nor is an empty selection.
        /// </summary>
        [Test]
        public void TryGetGrid_DifferentColumnsPerRow_ReturnsFalse()
        {
            List<CellId> cells = new List<CellId> { Cell(0, FirstColumn), Cell(0, SecondColumn), Cell(1, FirstColumn) };

            Assert.IsFalse(CellClipboard.TryGetGrid(cells, _rows, _columns, out _, out _));
            Assert.IsFalse(CellClipboard.TryGetGrid(new List<CellId>(), _rows, _columns, out _, out _));
        }

        /// <summary>
        /// The cells of the columns that are not given (the Controls column) are ignored, so they don't break the grid.
        /// </summary>
        [Test]
        public void TryGetGrid_CellsOfOtherColumns_Ignored()
        {
            List<CellId> cells = new List<CellId>
            {
                Cell(0, FirstColumn), Cell(0, TableViewColumn.ControlsId), Cell(1, FirstColumn),
            };

            Assert.IsTrue(CellClipboard.TryGetGrid(cells, _rows, _columns, out List<Object> gridRows, out List<string> gridColumns));
            Assert.AreEqual(2, gridRows.Count);
            CollectionAssert.AreEqual(new[] { FirstColumn }, gridColumns);
        }

        #endregion


        #region Paste

        /// <summary>
        /// A single value goes to all the selected cells.
        /// </summary>
        [Test]
        public void TryGetPasteTargets_SingleValue_AllSelectedCells()
        {
            List<CellId> cells = new List<CellId> { Cell(0, SecondColumn), Cell(3, ThirdColumn) };

            Assert.IsTrue(CellClipboard.TryGetPasteTargets(Values(new[] { "7" }), cells, _rows, _columns,
                out List<CellClipboard.PasteTarget> targets, out _));
            Assert.AreEqual(2, targets.Count);
            Assert.IsTrue(targets.TrueForAll(target => target.Text == "7"));
        }

        /// <summary>
        /// A grid pasted from a single selected cell fills the next rows and columns.
        /// </summary>
        [Test]
        public void TryGetPasteTargets_GridFromSingleCell_FillsNextCells()
        {
            List<CellId> cells = new List<CellId> { Cell(1, SecondColumn) };

            Assert.IsTrue(CellClipboard.TryGetPasteTargets(Values(new[] { "a", "b" }, new[] { "c", "d" }), cells, _rows, _columns,
                out List<CellClipboard.PasteTarget> targets, out _));

            Assert.AreEqual(4, targets.Count);
            Assert.AreEqual(Cell(1, SecondColumn), targets[0].Cell);
            Assert.AreEqual("a", targets[0].Text);
            Assert.AreEqual(Cell(2, ThirdColumn), targets[3].Cell);
            Assert.AreEqual("d", targets[3].Text);
        }

        /// <summary>
        /// A grid pasted from a single cell is refused if it goes beyond the last row or column.
        /// </summary>
        [Test]
        public void TryGetPasteTargets_GridNotFitting_Refused()
        {
            List<CellId> cells = new List<CellId> { Cell(3, SecondColumn) };

            Assert.IsFalse(CellClipboard.TryGetPasteTargets(Values(new[] { "a" }, new[] { "b" }), cells, _rows, _columns, out _,
                out string error));
            Assert.AreEqual(CellClipboard.NotFittingMessage, error);
        }

        /// <summary>
        /// A grid pasted on a grid selection of the same size goes to the cells in the same place, even if they are not next to each
        /// other.
        /// </summary>
        [Test]
        public void TryGetPasteTargets_GridOnSameSizeSelection_SamePlaces()
        {
            List<CellId> cells = new List<CellId>
            {
                Cell(0, FirstColumn), Cell(0, ThirdColumn), Cell(3, FirstColumn), Cell(3, ThirdColumn),
            };

            Assert.IsTrue(CellClipboard.TryGetPasteTargets(Values(new[] { "a", "b" }, new[] { "c", "d" }), cells, _rows, _columns,
                out List<CellClipboard.PasteTarget> targets, out _));

            Assert.AreEqual(Cell(0, ThirdColumn), targets[1].Cell);
            Assert.AreEqual("b", targets[1].Text);
            Assert.AreEqual(Cell(3, FirstColumn), targets[2].Cell);
            Assert.AreEqual("c", targets[2].Text);
        }

        /// <summary>
        /// A grid pasted on a selection of another size, or on a selection that is not a grid, is refused.
        /// </summary>
        [Test]
        public void TryGetPasteTargets_GridOnOtherSelection_Refused()
        {
            List<CellId> otherSize = new List<CellId> { Cell(0, FirstColumn), Cell(1, FirstColumn), Cell(2, FirstColumn) };
            Assert.IsFalse(CellClipboard.TryGetPasteTargets(Values(new[] { "a" }, new[] { "b" }), otherSize, _rows, _columns, out _,
                out string error));
            Assert.IsNotNull(error);

            List<CellId> notGrid = new List<CellId> { Cell(0, FirstColumn), Cell(1, SecondColumn) };
            Assert.IsFalse(CellClipboard.TryGetPasteTargets(Values(new[] { "a" }, new[] { "b" }), notGrid, _rows, _columns, out _,
                out _));
        }

        /// <summary>
        /// Nothing is pasted from an empty clipboard, nor without a selected cell that can receive a value.
        /// </summary>
        [Test]
        public void TryGetPasteTargets_NothingToPasteOrNoSelection_Refused()
        {
            List<CellId> cells = new List<CellId> { Cell(0, FirstColumn) };
            Assert.IsFalse(CellClipboard.TryGetPasteTargets(Values(), cells, _rows, _columns, out _, out string emptyError));
            Assert.AreEqual(CellClipboard.NothingToPasteMessage, emptyError);

            List<CellId> controlsOnly = new List<CellId> { Cell(0, TableViewColumn.ControlsId) };
            Assert.IsFalse(CellClipboard.TryGetPasteTargets(Values(new[] { "a" }), controlsOnly, _rows, _columns, out _,
                out string selectionError));
            Assert.AreEqual(CellClipboard.NoSelectionMessage, selectionError);
        }

        /// <summary>
        /// The rows shorter than the others are completed with empty values.
        /// </summary>
        [Test]
        public void TryGetPasteTargets_ShortRow_EmptyValues()
        {
            List<CellId> cells = new List<CellId> { Cell(0, FirstColumn) };

            Assert.IsTrue(CellClipboard.TryGetPasteTargets(Values(new[] { "a", "b" }, new[] { "c" }), cells, _rows, _columns,
                out List<CellClipboard.PasteTarget> targets, out _));
            Assert.AreEqual(string.Empty, targets[3].Text);
        }

        #endregion


        #region Private API

        /// <summary>
        /// Gets the identifier of a cell of the test rows.
        /// </summary>
        /// <param name="rowIndex">The index of the row.</param>
        /// <param name="columnId">The identifier of the column.</param>
        /// <returns>Returns the identifier of the cell.</returns>
        private CellId Cell(int rowIndex, string columnId)
        {
            return new CellId(_rows[rowIndex], columnId);
        }

        /// <summary>
        /// Creates pasted values, row by row.
        /// </summary>
        /// <param name="rows">The values of each row.</param>
        /// <returns>Returns the values.</returns>
        private static List<IReadOnlyList<string>> Values(params string[][] rows)
        {
            return new List<IReadOnlyList<string>>(rows);
        }

        #endregion

    }

}
