using System.Collections.Generic;

using NUnit.Framework;

using SideXP.Database.EditorOnly;

namespace SideXP.Database.Tests
{

    /// <summary>
    /// Tests for the rules of the sort on several columns (<see cref="MultiColumnSort"/>): clicks and Alt-clicks on the headers, and
    /// the items of their menu.
    /// </summary>
    public class MultiColumnSortTests
    {

        #region Fields

        private static readonly ColumnSort TierAscending = new ColumnSort("Tier", false);
        private static readonly ColumnSort TierDescending = new ColumnSort("Tier", true);
        private static readonly ColumnSort PriceAscending = new ColumnSort("Price", false);
        private static readonly ColumnSort PriceDescending = new ColumnSort("Price", true);

        #endregion


        #region Click

        /// <summary>
        /// A click on a column that is not sorted sorts by that column only, in ascending order.
        /// </summary>
        [Test]
        public void SortBy_ColumnNotSorted_OnlyThatColumnAscending()
        {
            List<ColumnSort> sort = MultiColumnSort.SortBy(new[] { TierDescending }, "Price");
            CollectionAssert.AreEqual(new[] { PriceAscending }, sort);
        }

        /// <summary>
        /// A click on the sorted column reverses its direction.
        /// </summary>
        [Test]
        public void SortBy_OnlySortedColumn_Reversed()
        {
            CollectionAssert.AreEqual(new[] { TierDescending }, MultiColumnSort.SortBy(new[] { TierAscending }, "Tier"));
            CollectionAssert.AreEqual(new[] { TierAscending }, MultiColumnSort.SortBy(new[] { TierDescending }, "Tier"));
        }

        /// <summary>
        /// A click on a column of a sort on several columns sorts by that column only, in the other direction.
        /// </summary>
        [Test]
        public void SortBy_ColumnOfSeveral_OnlyThatColumnReversed()
        {
            List<ColumnSort> sort = MultiColumnSort.SortBy(new[] { TierAscending, PriceAscending }, "Price");
            CollectionAssert.AreEqual(new[] { PriceDescending }, sort);
        }

        /// <summary>
        /// A click on a column without sort sorts by that column in ascending order.
        /// </summary>
        [Test]
        public void SortBy_NoSort_ColumnAscending()
        {
            CollectionAssert.AreEqual(new[] { TierAscending }, MultiColumnSort.SortBy(new ColumnSort[0], "Tier"));
        }

        #endregion


        #region Alt-click

        /// <summary>
        /// An Alt-click on a column that is not sorted adds it last, in ascending order.
        /// </summary>
        [Test]
        public void Cycle_ColumnNotSorted_AddedLastAscending()
        {
            List<ColumnSort> sort = MultiColumnSort.Cycle(new[] { TierDescending }, "Price");
            CollectionAssert.AreEqual(new[] { TierDescending, PriceAscending }, sort);
        }

        /// <summary>
        /// An Alt-click on an ascending column makes it descending, at the same priority.
        /// </summary>
        [Test]
        public void Cycle_AscendingColumn_DescendingAtSamePlace()
        {
            List<ColumnSort> sort = MultiColumnSort.Cycle(new[] { TierAscending, PriceAscending }, "Tier");
            CollectionAssert.AreEqual(new[] { TierDescending, PriceAscending }, sort);
        }

        /// <summary>
        /// An Alt-click on a descending column removes it from the sort, and the next columns take its priority.
        /// </summary>
        [Test]
        public void Cycle_DescendingColumn_Removed()
        {
            List<ColumnSort> sort = MultiColumnSort.Cycle(new[] { TierDescending, PriceAscending }, "Tier");
            CollectionAssert.AreEqual(new[] { PriceAscending }, sort);
        }

        /// <summary>
        /// Three Alt-clicks on a column go through ascending, descending, and back to the sort without that column.
        /// </summary>
        [Test]
        public void Cycle_ThreeTimes_BackToPreviousSort()
        {
            ColumnSort[] initialSort = { TierAscending };
            List<ColumnSort> sort = MultiColumnSort.Cycle(initialSort, "Price");
            CollectionAssert.AreEqual(new[] { TierAscending, PriceAscending }, sort);

            sort = MultiColumnSort.Cycle(sort, "Price");
            CollectionAssert.AreEqual(new[] { TierAscending, PriceDescending }, sort);

            sort = MultiColumnSort.Cycle(sort, "Price");
            CollectionAssert.AreEqual(initialSort, sort);
        }

        /// <summary>
        /// The given sort is never changed.
        /// </summary>
        [Test]
        public void Cycle_GivenSort_Unchanged()
        {
            List<ColumnSort> initialSort = new List<ColumnSort> { TierAscending };
            MultiColumnSort.Cycle(initialSort, "Price");
            MultiColumnSort.Cycle(initialSort, "Tier");

            CollectionAssert.AreEqual(new[] { TierAscending }, initialSort);
        }

        #endregion


        #region Menu

        /// <summary>
        /// Adding a column puts it last in ascending order, and adding a sorted column changes nothing.
        /// </summary>
        [Test]
        public void Add_Column_LastOrUnchanged()
        {
            CollectionAssert.AreEqual(new[] { TierDescending, PriceAscending }, MultiColumnSort.Add(new[] { TierDescending }, "Price"));
            CollectionAssert.AreEqual(new[] { TierDescending, PriceDescending },
                MultiColumnSort.Add(new[] { TierDescending, PriceDescending }, "Price"));
        }

        /// <summary>
        /// Removing a column keeps the other columns in their order, and removing a column that is not sorted changes nothing.
        /// </summary>
        [Test]
        public void Remove_Column_OthersKept()
        {
            CollectionAssert.AreEqual(new[] { PriceDescending }, MultiColumnSort.Remove(new[] { TierAscending, PriceDescending }, "Tier"));
            CollectionAssert.AreEqual(new[] { TierAscending }, MultiColumnSort.Remove(new[] { TierAscending }, "Price"));
        }

        /// <summary>
        /// The priority of a column is its index in the sort.
        /// </summary>
        [Test]
        public void IndexOf_Columns_Priority()
        {
            ColumnSort[] sort = { TierAscending, PriceDescending };

            Assert.AreEqual(0, MultiColumnSort.IndexOf(sort, "Tier"));
            Assert.AreEqual(1, MultiColumnSort.IndexOf(sort, "Price"));
            Assert.AreEqual(-1, MultiColumnSort.IndexOf(sort, "Weight"));
        }

        #endregion

    }

}
