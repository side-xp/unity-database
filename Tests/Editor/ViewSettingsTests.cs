using NUnit.Framework;

using UnityEngine;

using SideXP.Database.EditorOnly;

namespace SideXP.Database.Tests
{

    /// <summary>
    /// Tests for the remembered settings of the tables (<see cref="ViewSettings"/>): column order, widths and sort.
    /// </summary>
    public class ViewSettingsTests
    {

        #region Fields

        private const string TableId = "Tests.TableId";
        private const float ResizedWidth = 150;

        #endregion


        #region Column order

        /// <summary>
        /// Without settings, the columns are in their default order.
        /// </summary>
        [Test]
        public void GetColumnOrder_NoSettings_DefaultOrder()
        {
            ViewSettings settings = new ViewSettings(TableId);
            CollectionAssert.AreEqual(new[] { "A", "B", "C" }, settings.GetColumnOrder(new[] { "A", "B", "C" }));
        }

        /// <summary>
        /// A column that has no settings yet comes right after the column that precedes it in the default order.
        /// </summary>
        [Test]
        public void GetColumnOrder_NewColumn_AfterItsPredecessor()
        {
            ViewSettings settings = new ViewSettings(TableId);
            settings.GetColumnOrder(new[] { "A", "B", "C" });
            settings.SetColumnOrder(new[] { "C", "A", "B" });

            CollectionAssert.AreEqual(new[] { "C", "A", "X", "B" }, settings.GetColumnOrder(new[] { "A", "X", "B", "C" }));
        }

        /// <summary>
        /// A new column that is first in the default order comes first.
        /// </summary>
        [Test]
        public void GetColumnOrder_NewFirstColumn_First()
        {
            ViewSettings settings = new ViewSettings(TableId);
            settings.GetColumnOrder(new[] { "A", "B" });
            settings.SetColumnOrder(new[] { "B", "A" });

            CollectionAssert.AreEqual(new[] { "X", "B", "A" }, settings.GetColumnOrder(new[] { "X", "A", "B" }));
        }

        /// <summary>
        /// The settings of a column that doesn't exist anymore are ignored, and apply again if the column comes back.
        /// </summary>
        [Test]
        public void GetColumnOrder_MissingColumn_IgnoredAndKept()
        {
            ViewSettings settings = new ViewSettings(TableId);
            settings.GetColumnOrder(new[] { "A", "B", "C" });
            settings.SetColumnOrder(new[] { "B", "C", "A" });
            settings.SetWidth("B", ResizedWidth);

            CollectionAssert.AreEqual(new[] { "C", "A" }, settings.GetColumnOrder(new[] { "A", "C" }));
            CollectionAssert.AreEqual(new[] { "B", "C", "A" }, settings.GetColumnOrder(new[] { "A", "B", "C" }));
            Assert.AreEqual(ResizedWidth, settings.GetWidth("B"));
        }

        /// <summary>
        /// Reordering some columns puts them in the places of the same columns, so the other columns keep theirs.
        /// </summary>
        [Test]
        public void SetColumnOrder_SomeColumns_OthersKeepTheirPlaces()
        {
            ViewSettings settings = new ViewSettings(TableId);
            settings.GetColumnOrder(new[] { "A", "B", "C", "D" });

            Assert.IsTrue(settings.SetColumnOrder(new[] { "D", "B" }));
            CollectionAssert.AreEqual(new[] { "A", "D", "C", "B" }, settings.GetColumnOrder(new[] { "A", "B", "C", "D" }));
        }

        /// <summary>
        /// Setting the current order doesn't change anything.
        /// </summary>
        [Test]
        public void SetColumnOrder_SameOrder_False()
        {
            ViewSettings settings = new ViewSettings(TableId);
            settings.GetColumnOrder(new[] { "A", "B", "C" });

            Assert.IsFalse(settings.SetColumnOrder(new[] { "B", "C" }));
        }

        /// <summary>
        /// The columns without settings are ignored when reordering.
        /// </summary>
        [Test]
        public void SetColumnOrder_UnknownColumn_Ignored()
        {
            ViewSettings settings = new ViewSettings(TableId);
            settings.GetColumnOrder(new[] { "A", "B" });

            Assert.IsTrue(settings.SetColumnOrder(new[] { "B", "X", "A" }));
            CollectionAssert.AreEqual(new[] { "B", "A" }, settings.GetColumnOrder(new[] { "A", "B" }));
        }

        #endregion


        #region Widths

        /// <summary>
        /// A column has its default width (0) until the user resizes it.
        /// </summary>
        [Test]
        public void GetWidth_NotResized_Zero()
        {
            ViewSettings settings = new ViewSettings(TableId);
            settings.GetColumnOrder(new[] { "A" });

            Assert.AreEqual(0, settings.GetWidth("A"));
            Assert.AreEqual(0, settings.GetWidth("Unknown"));
        }

        /// <summary>
        /// A width is remembered, and setting the same width again changes nothing.
        /// </summary>
        [Test]
        public void SetWidth_Resized_Remembered()
        {
            ViewSettings settings = new ViewSettings(TableId);
            settings.GetColumnOrder(new[] { "A" });

            Assert.IsTrue(settings.SetWidth("A", ResizedWidth));
            Assert.IsFalse(settings.SetWidth("A", ResizedWidth));
            Assert.AreEqual(ResizedWidth, settings.GetWidth("A"));
        }

        /// <summary>
        /// A width of 0 or less gives the column its default width again.
        /// </summary>
        [Test]
        public void SetWidth_Negative_DefaultWidth()
        {
            ViewSettings settings = new ViewSettings(TableId);
            settings.SetWidth("A", ResizedWidth);

            Assert.IsTrue(settings.SetWidth("A", -1));
            Assert.AreEqual(0, settings.GetWidth("A"));
        }

        #endregion


        #region Sort

        /// <summary>
        /// A sort is remembered with its direction, and setting the same sort again changes nothing.
        /// </summary>
        [Test]
        public void SetSort_Column_Remembered()
        {
            ViewSettings settings = new ViewSettings(TableId);

            Assert.IsTrue(settings.SetSort("A", true));
            Assert.IsFalse(settings.SetSort("A", true));
            Assert.IsTrue(settings.HasSort);
            Assert.AreEqual("A", settings.SortColumnId);
            Assert.IsTrue(settings.IsSortDescending);
        }

        /// <summary>
        /// Clearing the sort, or setting a sort without column, keeps the order of the source.
        /// </summary>
        [Test]
        public void ClearSort_Sorted_NoSort()
        {
            ViewSettings settings = new ViewSettings(TableId);
            settings.SetSort("A", true);
            Assert.IsTrue(settings.ClearSort());
            Assert.IsFalse(settings.ClearSort());

            settings.SetSort("A", false);
            Assert.IsTrue(settings.SetSort(null, false));
            Assert.IsFalse(settings.HasSort);
            Assert.IsNull(settings.SortColumnId);
            Assert.IsFalse(settings.IsSortDescending);
        }

        #endregion


        #region Serialization

        /// <summary>
        /// The settings survive Unity's serialization, which is used to store them.
        /// </summary>
        [Test]
        public void Serialization_RoundTrip_SettingsKept()
        {
            ViewSettings settings = new ViewSettings(TableId);
            settings.GetColumnOrder(new[] { "A", "B", "C" });
            settings.SetColumnOrder(new[] { "C", "B" });
            settings.SetWidth("B", ResizedWidth);
            settings.SetSort("C", true);

            ViewSettings copy = new ViewSettings(string.Empty);
            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(settings), copy);

            Assert.AreEqual(TableId, copy.TableId);
            CollectionAssert.AreEqual(new[] { "A", "C", "B" }, copy.GetColumnOrder(new[] { "A", "B", "C" }));
            Assert.AreEqual(ResizedWidth, copy.GetWidth("B"));
            Assert.AreEqual("C", copy.SortColumnId);
            Assert.IsTrue(copy.IsSortDescending);
        }

        #endregion

    }

}
