using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using NUnit.Framework;

using UnityEngine;

using SideXP.Database.EditorOnly;

using Object = UnityEngine.Object;

namespace SideXP.Database.Tests
{

    /// <summary>
    /// Tests for the <see cref="TablePipeline"/>: value cache, invalidation per stage, filter, stable sort on one or several columns,
    /// and pages.
    /// </summary>
    /// <remarks>
    /// The pipeline reads in-memory assets from a list, through accessors that count their reads, so the tests can check which stages
    /// rerun.
    /// </remarks>
    public class TablePipelineTests
    {

        #region Subclasses

        /// <summary>
        /// Provides the assets of a list, and counts how many times they are loaded.
        /// </summary>
        private class ListAssetSource : IAssetSource
        {

            /// <summary>
            /// The assets to provide.
            /// </summary>
            public List<Object> Assets = new List<Object>();

            /// <summary>
            /// The number of times the assets have been loaded.
            /// </summary>
            public int LoadCount = 0;

            /// <inheritdoc/>
            public IReadOnlyList<Object> LoadAssets()
            {
                LoadCount++;
                return new List<Object>(Assets);
            }

        }

        /// <summary>
        /// Wraps an accessor, and counts how many values it reads.
        /// </summary>
        private class CountingAccessor : IValueAccessor
        {

            /// <summary>
            /// The wrapped accessor.
            /// </summary>
            private IValueAccessor _accessor = null;

            /// <summary>
            /// The number of values read.
            /// </summary>
            public int ReadCount = 0;

            /// <inheritdoc cref="CountingAccessor"/>
            /// <param name="accessor"><inheritdoc cref="_accessor" path="/summary"/></param>
            public CountingAccessor(IValueAccessor accessor)
            {
                _accessor = accessor;
            }

            /// <inheritdoc/>
            public Type ValueType => _accessor.ValueType;

            /// <inheritdoc/>
            public object Read(ValueReadContext context)
            {
                ReadCount++;
                return _accessor.Read(context);
            }

        }

        #endregion


        #region Fields

        /// <summary>
        /// The index of the column that reads <see cref="PipelineTestSO.number"/>.
        /// </summary>
        private const int NumberColumn = 0;

        /// <summary>
        /// The index of the column that reads the asset names.
        /// </summary>
        private const int NameColumn = 1;

        /// <summary>
        /// The assets of the tests, named A to D, with the numbers 3, 1, 3 and 2.
        /// </summary>
        private List<PipelineTestSO> _assets = null;

        /// <summary>
        /// The source of the tested pipeline.
        /// </summary>
        private ListAssetSource _source = null;

        /// <summary>
        /// The accessor of the <see cref="NumberColumn"/>.
        /// </summary>
        private CountingAccessor _numberAccessor = null;

        /// <summary>
        /// The accessor of the <see cref="NameColumn"/>.
        /// </summary>
        private CountingAccessor _nameAccessor = null;

        /// <summary>
        /// The tested pipeline.
        /// </summary>
        private TablePipeline _pipeline = null;

        #endregion


        #region Lifecycle

        /// <summary>
        /// Creates the assets and the pipeline.
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            _assets = new List<PipelineTestSO>
            {
                CreateAsset("A", 3),
                CreateAsset("B", 1),
                CreateAsset("C", 3),
                CreateAsset("D", 2),
            };

            _source = new ListAssetSource();
            _source.Assets.AddRange(_assets);

            FieldInfo numberField = typeof(PipelineTestSO).GetField(nameof(PipelineTestSO.number));
            _numberAccessor = new CountingAccessor(new ReflectionAccessor(new[] { numberField }));
            _nameAccessor = new CountingAccessor(AssetNameAccessor.Instance);
            _pipeline = new TablePipeline(_source, new IValueAccessor[] { _numberAccessor, _nameAccessor });
        }

        /// <summary>
        /// Destroys the assets.
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            foreach (PipelineTestSO asset in _assets)
            {
                if (asset != null)
                    Object.DestroyImmediate(asset);
            }
        }

        #endregion


        #region Source and value cache

        /// <summary>
        /// The first update loads all the rows in the order of the source, with their values.
        /// </summary>
        [Test]
        public void Update_FirstCall_LoadsRowsInSourceOrder()
        {
            Assert.IsTrue(_pipeline.Update());

            CollectionAssert.AreEqual(new[] { "A", "B", "C", "D" }, GetVisibleNames());
            Assert.AreEqual(4, _pipeline.Rows.Count);
            Assert.AreEqual(3, _pipeline.Rows[0].GetValue(NumberColumn));
            Assert.AreEqual(4, _numberAccessor.ReadCount);
        }

        /// <summary>
        /// An update without any invalidation reruns nothing.
        /// </summary>
        [Test]
        public void Update_NothingInvalidated_RerunsNothing()
        {
            _pipeline.Update();
            ResetCounts();

            Assert.IsFalse(_pipeline.Update());
            Assert.AreEqual(0, _source.LoadCount);
            Assert.AreEqual(0, _numberAccessor.ReadCount);
        }

        /// <summary>
        /// Invalidating a row reads the values of that row only.
        /// </summary>
        [Test]
        public void InvalidateRow_ChangedValue_RereadsOnlyThatRow()
        {
            _pipeline.Update();
            ResetCounts();

            _assets[1].number = 10;
            Assert.IsTrue(_pipeline.InvalidateRow(_assets[1]));
            _pipeline.Update();

            Assert.AreEqual(1, _numberAccessor.ReadCount);
            Assert.AreEqual(1, _nameAccessor.ReadCount);
            Assert.AreEqual(0, _source.LoadCount);
            Assert.IsTrue(_pipeline.TryGetRow(_assets[1], out TableRow row));
            Assert.AreEqual(10, row.GetValue(NumberColumn));
        }

        /// <summary>
        /// A value changed without invalidating its row is not read: the cache keeps the previous value.
        /// </summary>
        [Test]
        public void Update_ChangedValueNotInvalidated_KeepsCachedValue()
        {
            _pipeline.Update();

            _assets[1].number = 10;
            _pipeline.Update();

            _pipeline.TryGetRow(_assets[1], out TableRow row);
            Assert.AreEqual(1, row.GetValue(NumberColumn));
        }

        /// <summary>
        /// Invalidating the row of an asset that is not in the table does nothing.
        /// </summary>
        [Test]
        public void InvalidateRow_UnknownAsset_ReturnsFalse()
        {
            _pipeline.Update();
            PipelineTestSO otherAsset = CreateAsset("E", 0);
            _assets.Add(otherAsset);

            Assert.IsFalse(_pipeline.InvalidateRow(otherAsset));
            Assert.IsFalse(_pipeline.InvalidateRow(null));
            Assert.IsFalse(_pipeline.Update());
        }

        /// <summary>
        /// Invalidating the source loads the assets again, keeps the rows of the assets still in the table, and reads all the values
        /// again.
        /// </summary>
        [Test]
        public void InvalidateSource_ChangedAssets_KeepsRemainingRows()
        {
            _pipeline.Update();
            _pipeline.TryGetRow(_assets[0], out TableRow rowA);
            ResetCounts();

            PipelineTestSO assetE = CreateAsset("E", 5);
            _assets.Add(assetE);
            _source.Assets.Remove(_assets[1]);
            _source.Assets.Add(assetE);
            _pipeline.InvalidateSource();
            _pipeline.Update();

            CollectionAssert.AreEqual(new[] { "A", "C", "D", "E" }, GetVisibleNames());
            Assert.IsTrue(_pipeline.TryGetRow(_assets[0], out TableRow newRowA));
            Assert.AreSame(rowA, newRowA);
            Assert.IsFalse(_pipeline.TryGetRow(_assets[1], out _));
            Assert.AreEqual(1, _source.LoadCount);
            Assert.AreEqual(4, _numberAccessor.ReadCount);
        }

        /// <summary>
        /// The source version changes each time the source is loaded, and only then.
        /// </summary>
        [Test]
        public void SourceVersion_SourceLoaded_Changes()
        {
            _pipeline.Update();
            int firstVersion = _pipeline.SourceVersion;

            _pipeline.InvalidateRow(_assets[0]);
            _pipeline.Update();
            Assert.AreEqual(firstVersion, _pipeline.SourceVersion);

            _pipeline.InvalidateSource();
            _pipeline.Update();
            Assert.AreNotEqual(firstVersion, _pipeline.SourceVersion);
        }

        #endregion


        #region Filter

        /// <summary>
        /// The filter hides the rows that don't match, without reading the values again.
        /// </summary>
        [Test]
        public void Filter_Set_HidesRowsThatDontMatch()
        {
            _pipeline.Update();
            ResetCounts();

            _pipeline.Filter = row => (int)row.GetValue(NumberColumn) == 3;
            Assert.IsTrue(_pipeline.Update());

            CollectionAssert.AreEqual(new[] { "A", "C" }, GetVisibleNames());
            Assert.AreEqual(4, _pipeline.Rows.Count);
            Assert.AreEqual(0, _numberAccessor.ReadCount);
        }

        /// <summary>
        /// Removing the filter shows all the rows.
        /// </summary>
        [Test]
        public void Filter_Removed_ShowsAllRows()
        {
            _pipeline.Filter = row => (int)row.GetValue(NumberColumn) == 3;
            _pipeline.Update();

            _pipeline.Filter = null;
            _pipeline.Update();

            CollectionAssert.AreEqual(new[] { "A", "B", "C", "D" }, GetVisibleNames());
        }

        /// <summary>
        /// A row whose value changes is filtered again when it's invalidated.
        /// </summary>
        [Test]
        public void InvalidateRow_ChangedValue_FiltersAgain()
        {
            _pipeline.Filter = row => (int)row.GetValue(NumberColumn) == 3;
            _pipeline.Update();

            _assets[1].number = 3;
            _pipeline.InvalidateRow(_assets[1]);
            _pipeline.Update();

            CollectionAssert.AreEqual(new[] { "A", "B", "C" }, GetVisibleNames());
        }

        /// <summary>
        /// The text of a row built for the search is cleared when its values are read again, and kept otherwise.
        /// </summary>
        [Test]
        public void SearchText_RowRead_Cleared()
        {
            _pipeline.Update();
            _pipeline.TryGetRow(_assets[0], out TableRow rowA);
            _pipeline.TryGetRow(_assets[1], out TableRow rowB);
            rowA.SearchText = "A";
            rowB.SearchText = "B";

            _pipeline.InvalidateRow(_assets[0]);
            _pipeline.Update();

            Assert.IsNull(rowA.SearchText);
            Assert.AreEqual("B", rowB.SearchText);
        }

        #endregion


        #region Sort

        /// <summary>
        /// Sorting keeps the order of the source for equal values, in ascending order.
        /// </summary>
        [Test]
        public void SetSort_Ascending_EqualValuesKeepSourceOrder()
        {
            _pipeline.SetSort(NumberColumn, false, Compare);
            _pipeline.Update();

            CollectionAssert.AreEqual(new[] { "B", "D", "A", "C" }, GetVisibleNames());
        }

        /// <summary>
        /// Sorting keeps the order of the source for equal values, in descending order too.
        /// </summary>
        [Test]
        public void SetSort_Descending_EqualValuesKeepSourceOrder()
        {
            _pipeline.SetSort(NumberColumn, true, Compare);
            _pipeline.Update();

            CollectionAssert.AreEqual(new[] { "A", "C", "D", "B" }, GetVisibleNames());
        }

        /// <summary>
        /// Changing the sort doesn't read the values again.
        /// </summary>
        [Test]
        public void SetSort_Changed_DoesNotReadValues()
        {
            _pipeline.Update();
            ResetCounts();

            _pipeline.SetSort(NameColumn, true, Compare);
            Assert.IsTrue(_pipeline.Update());

            CollectionAssert.AreEqual(new[] { "D", "C", "B", "A" }, GetVisibleNames());
            Assert.AreEqual(0, _numberAccessor.ReadCount);
            Assert.AreEqual(0, _nameAccessor.ReadCount);
        }

        /// <summary>
        /// Removing the sort restores the order of the source.
        /// </summary>
        [Test]
        public void ClearSort_Sorted_RestoresSourceOrder()
        {
            _pipeline.SetSort(NumberColumn, false, Compare);
            _pipeline.Update();

            _pipeline.ClearSort();
            _pipeline.Update();

            CollectionAssert.AreEqual(new[] { "A", "B", "C", "D" }, GetVisibleNames());
        }

        /// <summary>
        /// A row whose value changes is sorted again when it's invalidated.
        /// </summary>
        [Test]
        public void InvalidateRow_ChangedValue_SortsAgain()
        {
            _pipeline.SetSort(NumberColumn, false, Compare);
            _pipeline.Update();

            _assets[1].number = 5;
            _pipeline.InvalidateRow(_assets[1]);
            _pipeline.Update();

            CollectionAssert.AreEqual(new[] { "D", "A", "C", "B" }, GetVisibleNames());
        }

        /// <summary>
        /// Sorting by a column that doesn't exist is refused.
        /// </summary>
        [Test]
        public void SetSort_InvalidArguments_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => _pipeline.SetSort(-1, false, Compare));
            Assert.Throws<ArgumentOutOfRangeException>(() => _pipeline.SetSort(_pipeline.ColumnCount, false, Compare));
            Assert.Throws<ArgumentNullException>(() => _pipeline.SetSort(NumberColumn, false, null));
        }

        #endregion


        #region Sort on several columns

        /// <summary>
        /// The rows of equal values in the first column are sorted by the second column.
        /// </summary>
        [Test]
        public void SetSort_SeveralColumns_EqualValuesSortedByNextColumn()
        {
            _pipeline.SetSort(new[] { new SortKey(NumberColumn, false, Compare), new SortKey(NameColumn, true, Compare) });
            _pipeline.Update();

            CollectionAssert.AreEqual(new[] { "B", "D", "C", "A" }, GetVisibleNames());
        }

        /// <summary>
        /// Each column has its own direction.
        /// </summary>
        [Test]
        public void SetSort_SeveralColumns_OwnDirections()
        {
            _pipeline.SetSort(new[] { new SortKey(NumberColumn, true, Compare), new SortKey(NameColumn, true, Compare) });
            _pipeline.Update();

            CollectionAssert.AreEqual(new[] { "C", "A", "D", "B" }, GetVisibleNames());
        }

        /// <summary>
        /// The rows equal in all the sorted columns keep the order of the source.
        /// </summary>
        [Test]
        public void SetSort_SeveralColumnsAllEqual_KeepSourceOrder()
        {
            _pipeline.SetSort(new[] { new SortKey(NumberColumn, false, Compare), new SortKey(NameColumn, true, (a, b) => 0) });
            _pipeline.Update();

            CollectionAssert.AreEqual(new[] { "B", "D", "A", "C" }, GetVisibleNames());
        }

        /// <summary>
        /// The sort keys of the pipeline are the given ones, in their order.
        /// </summary>
        [Test]
        public void SortKeys_SeveralColumns_InOrder()
        {
            _pipeline.SetSort(new[] { new SortKey(NameColumn, true, Compare), new SortKey(NumberColumn, false, Compare) });

            Assert.AreEqual(2, _pipeline.SortKeys.Count);
            Assert.AreEqual(NameColumn, _pipeline.SortKeys[0].ColumnIndex);
            Assert.IsTrue(_pipeline.SortKeys[0].IsDescending);
            Assert.AreEqual(NumberColumn, _pipeline.SortKeys[1].ColumnIndex);
            Assert.IsFalse(_pipeline.SortKeys[1].IsDescending);
        }

        /// <summary>
        /// Sorting by an empty list of columns restores the order of the source.
        /// </summary>
        [Test]
        public void SetSort_NoColumn_RestoresSourceOrder()
        {
            _pipeline.SetSort(NumberColumn, false, Compare);
            _pipeline.Update();

            _pipeline.SetSort(new SortKey[0]);
            _pipeline.Update();

            CollectionAssert.AreEqual(new[] { "A", "B", "C", "D" }, GetVisibleNames());
            Assert.AreEqual(0, _pipeline.SortKeys.Count);
        }

        /// <summary>
        /// A list of keys with an invalid key, or with the same column twice, is refused, and the current sort is kept.
        /// </summary>
        [Test]
        public void SetSort_InvalidKeys_Throws()
        {
            _pipeline.SetSort(NumberColumn, false, Compare);

            Assert.Throws<ArgumentNullException>(() => _pipeline.SetSort(null));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                _pipeline.SetSort(new[] { new SortKey(NumberColumn, false, Compare), new SortKey(-1, false, Compare) }));
            Assert.Throws<ArgumentNullException>(() =>
                _pipeline.SetSort(new[] { new SortKey(NumberColumn, false, Compare), new SortKey(NameColumn, false, null) }));
            Assert.Throws<ArgumentException>(() =>
                _pipeline.SetSort(new[] { new SortKey(NumberColumn, false, Compare), new SortKey(NumberColumn, true, Compare) }));

            Assert.AreEqual(1, _pipeline.SortKeys.Count);
            Assert.AreEqual(NumberColumn, _pipeline.SortKeys[0].ColumnIndex);
        }

        #endregion


        #region Pages

        /// <summary>
        /// Without page size, the page holds all the visible rows.
        /// </summary>
        [Test]
        public void PageRows_NotPaginated_AllVisibleRows()
        {
            _pipeline.Update();

            Assert.IsFalse(_pipeline.IsPaginated);
            CollectionAssert.AreEqual(new[] { "A", "B", "C", "D" }, GetPageNames());
            Assert.AreEqual(1, _pipeline.PageCount);
            Assert.AreEqual(0, _pipeline.PageStartIndex);
        }

        /// <summary>
        /// With a page size, each page holds the visible rows of its range, and the last page holds the remaining rows.
        /// </summary>
        [Test]
        public void PageRows_Paginated_RowsOfThePage()
        {
            _pipeline.PageSize = 3;
            _pipeline.Update();

            Assert.IsTrue(_pipeline.IsPaginated);
            Assert.AreEqual(2, _pipeline.PageCount);
            CollectionAssert.AreEqual(new[] { "A", "B", "C" }, GetPageNames());

            _pipeline.PageIndex = 1;
            _pipeline.Update();

            CollectionAssert.AreEqual(new[] { "D" }, GetPageNames());
            Assert.AreEqual(3, _pipeline.PageStartIndex);
            // The visible rows still hold the rows of all the pages
            CollectionAssert.AreEqual(new[] { "A", "B", "C", "D" }, GetVisibleNames());
        }

        /// <summary>
        /// The pages split the visible rows in the order of the sort.
        /// </summary>
        [Test]
        public void PageRows_Sorted_PagesFollowSort()
        {
            _pipeline.SetSort(NumberColumn, false, Compare);
            _pipeline.PageSize = 2;
            _pipeline.PageIndex = 1;
            _pipeline.Update();

            CollectionAssert.AreEqual(new[] { "A", "C" }, GetPageNames());
        }

        /// <summary>
        /// A page out of the range of the pages shows the last page, or the first page for a negative index.
        /// </summary>
        [Test]
        public void PageIndex_OutOfRange_KeptInRange()
        {
            _pipeline.PageSize = 3;
            _pipeline.PageIndex = 10;
            _pipeline.Update();

            Assert.AreEqual(1, _pipeline.PageIndex);
            CollectionAssert.AreEqual(new[] { "D" }, GetPageNames());

            _pipeline.PageIndex = -1;
            _pipeline.Update();

            Assert.AreEqual(0, _pipeline.PageIndex);
            CollectionAssert.AreEqual(new[] { "A", "B", "C" }, GetPageNames());
        }

        /// <summary>
        /// A filter that removes the rows of the displayed page shows the last page left.
        /// </summary>
        [Test]
        public void PageIndex_FilterHidesRows_KeptInRange()
        {
            _pipeline.Filter = row => (int)row.GetValue(NumberColumn) > 1;
            _pipeline.PageSize = 2;
            _pipeline.PageIndex = 1;
            _pipeline.Update();
            CollectionAssert.AreEqual(new[] { "D" }, GetPageNames());

            _pipeline.Filter = row => (int)row.GetValue(NumberColumn) == 3;
            _pipeline.Update();

            Assert.AreEqual(0, _pipeline.PageIndex);
            Assert.AreEqual(1, _pipeline.PageCount);
            CollectionAssert.AreEqual(new[] { "A", "C" }, GetPageNames());
        }

        /// <summary>
        /// Without any visible row, there is a single empty page.
        /// </summary>
        [Test]
        public void PageRows_NoVisibleRow_SingleEmptyPage()
        {
            _pipeline.Filter = row => false;
            _pipeline.PageSize = 2;
            _pipeline.PageIndex = 1;
            _pipeline.Update();

            Assert.AreEqual(0, _pipeline.PageIndex);
            Assert.AreEqual(1, _pipeline.PageCount);
            Assert.IsEmpty(_pipeline.PageRows);
        }

        /// <summary>
        /// Changing the page reruns the paginate stage only: the values are not read again, and an update without change reruns
        /// nothing.
        /// </summary>
        [Test]
        public void PageIndex_Changed_RerunsPageOnly()
        {
            _pipeline.PageSize = 2;
            _pipeline.Update();
            ResetCounts();

            _pipeline.PageIndex = 1;
            Assert.IsTrue(_pipeline.Update());
            Assert.AreEqual(0, _source.LoadCount);
            Assert.AreEqual(0, _numberAccessor.ReadCount);

            _pipeline.PageIndex = 1;
            Assert.IsFalse(_pipeline.Update());
        }

        /// <summary>
        /// A row whose value changes is sorted again, and moves to the page of its new position.
        /// </summary>
        [Test]
        public void InvalidateRow_ChangedValue_MovesToItsPage()
        {
            _pipeline.SetSort(NumberColumn, false, Compare);
            _pipeline.PageSize = 2;
            _pipeline.Update();
            CollectionAssert.AreEqual(new[] { "B", "D" }, GetPageNames());

            _assets[1].number = 5;
            _pipeline.InvalidateRow(_assets[1]);
            _pipeline.Update();

            Assert.AreEqual(0, _pipeline.PageIndex);
            CollectionAssert.AreEqual(new[] { "D", "A" }, GetPageNames());
            _pipeline.TryGetRow(_assets[1], out TableRow rowB);
            Assert.AreEqual(1, _pipeline.GetPageOf(rowB));
        }

        /// <summary>
        /// The page of a row comes from its position in the visible rows, and a row that isn't visible has no page.
        /// </summary>
        [Test]
        public void GetPageOf_Rows_PageOfTheirPosition()
        {
            _pipeline.Filter = row => (int)row.GetValue(NumberColumn) > 1;
            _pipeline.PageSize = 2;
            _pipeline.Update();

            _pipeline.TryGetRow(_assets[0], out TableRow rowA);
            _pipeline.TryGetRow(_assets[1], out TableRow rowB);
            _pipeline.TryGetRow(_assets[3], out TableRow rowD);
            Assert.AreEqual(0, _pipeline.GetPageOf(rowA));
            Assert.AreEqual(1, _pipeline.GetPageOf(rowD));
            Assert.AreEqual(-1, _pipeline.GetPageOf(rowB));
            Assert.AreEqual(-1, _pipeline.GetPageOf(null));

            _pipeline.PageSize = 0;
            Assert.AreEqual(0, _pipeline.GetPageOf(rowD));
        }

        /// <summary>
        /// A negative page size is read as no pagination.
        /// </summary>
        [Test]
        public void PageSize_Negative_NotPaginated()
        {
            _pipeline.PageSize = -5;
            _pipeline.Update();

            Assert.AreEqual(0, _pipeline.PageSize);
            Assert.IsFalse(_pipeline.IsPaginated);
            CollectionAssert.AreEqual(new[] { "A", "B", "C", "D" }, GetPageNames());
        }

        #endregion


        #region Private API

        /// <summary>
        /// Creates an in-memory asset.
        /// </summary>
        /// <param name="name">The name of the asset.</param>
        /// <param name="number">The value of <see cref="PipelineTestSO.number"/>.</param>
        /// <returns>Returns the created asset.</returns>
        private static PipelineTestSO CreateAsset(string name, int number)
        {
            PipelineTestSO asset = ScriptableObject.CreateInstance<PipelineTestSO>();
            asset.name = name;
            asset.number = number;
            return asset;
        }

        /// <summary>
        /// Compares two values with the default comparer.
        /// </summary>
        /// <param name="a">The first value.</param>
        /// <param name="b">The second value.</param>
        /// <returns>Returns a negative number if a is before b, a positive number if a is after b, or 0 if they are equal.</returns>
        private static int Compare(object a, object b)
        {
            return Comparer<object>.Default.Compare(a, b);
        }

        /// <summary>
        /// Resets the counters of the source and the accessors.
        /// </summary>
        private void ResetCounts()
        {
            _source.LoadCount = 0;
            _numberAccessor.ReadCount = 0;
            _nameAccessor.ReadCount = 0;
        }

        /// <summary>
        /// Gets the names of the assets of the visible rows, in order.
        /// </summary>
        /// <returns>Returns the names of the assets.</returns>
        private string[] GetVisibleNames()
        {
            return _pipeline.VisibleRows.Select(i => i.Asset.name).ToArray();
        }

        /// <summary>
        /// Gets the names of the assets of the rows of the displayed page, in order.
        /// </summary>
        /// <returns>Returns the names of the assets.</returns>
        private string[] GetPageNames()
        {
            return _pipeline.PageRows.Select(i => i.Asset.name).ToArray();
        }

        #endregion

    }

}
