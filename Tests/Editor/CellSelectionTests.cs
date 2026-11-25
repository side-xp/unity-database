using System.Collections.Generic;

using NUnit.Framework;

using UnityEngine;

using SideXP.Database.EditorOnly;

using Object = UnityEngine.Object;

namespace SideXP.Database.Tests
{

    /// <summary>
    /// Tests for <see cref="CellSelection"/>: single cells, toggled cells, ranges and their base, removed cells.
    /// </summary>
    public class CellSelectionTests
    {

        #region Fields

        private const string FirstColumn = "$AssetName";
        private const string SecondColumn = "_number";
        private const string ThirdColumn = "_label";

        /// <summary>
        /// The number of test rows: enough for a range that doesn't start nor end at the edges.
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


        #region Tests

        /// <summary>
        /// Selecting a cell deselects the others.
        /// </summary>
        [Test]
        public void Select_Cell_ReplacesSelection()
        {
            CellSelection selection = new CellSelection();
            selection.Select(Cell(0, FirstColumn));
            selection.Select(Cell(1, SecondColumn));

            Assert.AreEqual(1, selection.Count);
            Assert.IsTrue(selection.Contains(Cell(1, SecondColumn)));
            Assert.IsFalse(selection.Contains(Cell(0, FirstColumn)));
        }

        /// <summary>
        /// Toggling a cell selects it, and toggling it again deselects it, without changing the other cells.
        /// </summary>
        [Test]
        public void Toggle_Cell_AddsThenRemoves()
        {
            CellSelection selection = new CellSelection();
            selection.Select(Cell(0, FirstColumn));

            Assert.IsTrue(selection.Toggle(Cell(2, ThirdColumn)));
            Assert.AreEqual(2, selection.Count);

            Assert.IsFalse(selection.Toggle(Cell(2, ThirdColumn)));
            Assert.AreEqual(1, selection.Count);
            Assert.IsTrue(selection.Contains(Cell(0, FirstColumn)));
        }

        /// <summary>
        /// A range contains the rectangle of the rows and the columns between its two cells, row by row.
        /// </summary>
        [Test]
        public void GetRange_TwoCells_RectangleRowByRow()
        {
            List<CellId> range = CellSelection.GetRange(_rows, _columns, Cell(1, SecondColumn), Cell(2, ThirdColumn));

            CollectionAssert.AreEqual(new List<CellId>
            {
                Cell(1, SecondColumn), Cell(1, ThirdColumn),
                Cell(2, SecondColumn), Cell(2, ThirdColumn),
            }, range);
        }

        /// <summary>
        /// A range from the bottom right to the top left contains the same cells as the other way around.
        /// </summary>
        [Test]
        public void GetRange_ReversedCells_SameCells()
        {
            List<CellId> range = CellSelection.GetRange(_rows, _columns, Cell(1, SecondColumn), Cell(3, ThirdColumn));
            List<CellId> reversedRange = CellSelection.GetRange(_rows, _columns, Cell(3, ThirdColumn), Cell(1, SecondColumn));

            CollectionAssert.AreEquivalent(range, reversedRange);
            Assert.AreEqual(6, range.Count);
        }

        /// <summary>
        /// A range follows the order of the given columns, not the order in which the columns are declared: a range between the first
        /// and the last column includes the column between them, even if it's hidden.
        /// </summary>
        [Test]
        public void GetRange_ColumnOrder_FollowsGivenOrder()
        {
            List<string> reordered = new List<string> { ThirdColumn, FirstColumn, SecondColumn };
            List<CellId> range = CellSelection.GetRange(_rows, reordered, Cell(0, ThirdColumn), Cell(0, FirstColumn));

            CollectionAssert.AreEqual(new List<CellId> { Cell(0, ThirdColumn), Cell(0, FirstColumn) }, range);
        }

        /// <summary>
        /// A range from a cell that is not in the rows (no active cell, or a hidden row) contains the end cell only.
        /// </summary>
        [Test]
        public void GetRange_StartNotFound_EndCellOnly()
        {
            List<CellId> range = CellSelection.GetRange(_rows, _columns, CellId.None, Cell(2, SecondColumn));

            CollectionAssert.AreEqual(new List<CellId> { Cell(2, SecondColumn) }, range);
        }

        /// <summary>
        /// A range to a cell that is not in the rows is empty.
        /// </summary>
        [Test]
        public void GetRange_EndNotFound_Empty()
        {
            List<CellId> range = CellSelection.GetRange(_rows, _columns, Cell(0, FirstColumn), new CellId(_rows[1], "_missing"));

            Assert.AreEqual(0, range.Count);
        }

        /// <summary>
        /// A range that is not added replaces all the selected cells, including the previous range.
        /// </summary>
        [Test]
        public void SelectRange_NotAdded_ReplacesSelection()
        {
            CellSelection selection = new CellSelection();
            selection.Select(Cell(0, FirstColumn));
            selection.Toggle(Cell(3, ThirdColumn));

            selection.SelectRange(CellSelection.GetRange(_rows, _columns, Cell(0, FirstColumn), Cell(2, FirstColumn)), false);
            selection.SelectRange(CellSelection.GetRange(_rows, _columns, Cell(0, FirstColumn), Cell(1, FirstColumn)), false);

            Assert.AreEqual(2, selection.Count);
            Assert.IsTrue(selection.Contains(Cell(0, FirstColumn)));
            Assert.IsTrue(selection.Contains(Cell(1, FirstColumn)));
            Assert.IsFalse(selection.Contains(Cell(2, FirstColumn)));
            Assert.IsFalse(selection.Contains(Cell(3, ThirdColumn)));
        }

        /// <summary>
        /// An added range keeps the cells selected before the last range, and replaces the previous range.
        /// </summary>
        [Test]
        public void SelectRange_Added_KeepsBaseAndReplacesPreviousRange()
        {
            CellSelection selection = new CellSelection();
            selection.Select(Cell(3, ThirdColumn));
            selection.Toggle(Cell(0, FirstColumn));

            selection.SelectRange(CellSelection.GetRange(_rows, _columns, Cell(0, FirstColumn), Cell(2, FirstColumn)), true);
            selection.SelectRange(CellSelection.GetRange(_rows, _columns, Cell(0, FirstColumn), Cell(1, FirstColumn)), true);

            Assert.AreEqual(3, selection.Count);
            Assert.IsTrue(selection.Contains(Cell(3, ThirdColumn)));
            Assert.IsTrue(selection.Contains(Cell(0, FirstColumn)));
            Assert.IsTrue(selection.Contains(Cell(1, FirstColumn)));
            Assert.IsFalse(selection.Contains(Cell(2, FirstColumn)));
        }

        /// <summary>
        /// Removed cells leave the selection and its base, so a later added range doesn't bring them back.
        /// </summary>
        [Test]
        public void RemoveAll_HiddenRow_RemovedFromBase()
        {
            CellSelection selection = new CellSelection();
            selection.Select(Cell(0, FirstColumn));
            selection.Toggle(Cell(1, FirstColumn));

            Object hiddenAsset = _rows[1];
            Assert.AreEqual(1, selection.RemoveAll(cell => cell.Asset == hiddenAsset));
            selection.SelectRange(new List<CellId> { Cell(3, FirstColumn) }, true);

            Assert.AreEqual(2, selection.Count);
            Assert.IsFalse(selection.Contains(Cell(1, FirstColumn)));
        }

        /// <summary>
        /// The asset count counts each asset once, whatever the number of its selected cells.
        /// </summary>
        [Test]
        public void AssetCount_SeveralCellsPerAsset_CountsAssets()
        {
            CellSelection selection = new CellSelection();
            selection.SelectRange(CellSelection.GetRange(_rows, _columns, Cell(0, FirstColumn), Cell(1, ThirdColumn)), false);

            Assert.AreEqual(6, selection.Count);
            Assert.AreEqual(2, selection.AssetCount);

            selection.RemoveAll(cell => cell.Asset == _rows[1]);
            Assert.AreEqual(1, selection.AssetCount);
        }

        /// <summary>
        /// A copy of the selection doesn't change when the selection changes, and restores it.
        /// </summary>
        [Test]
        public void Clone_SelectionChanged_CopyRestoresIt()
        {
            CellSelection selection = new CellSelection();
            selection.Select(Cell(0, FirstColumn));
            selection.Toggle(Cell(2, SecondColumn));
            CellSelection copy = selection.Clone();

            selection.Select(Cell(3, ThirdColumn));
            Assert.AreEqual(2, copy.Count);

            selection.CopyFrom(copy);
            Assert.AreEqual(2, selection.Count);
            Assert.IsTrue(selection.Contains(Cell(0, FirstColumn)));
            Assert.IsTrue(selection.Contains(Cell(2, SecondColumn)));
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

        #endregion

    }

}
