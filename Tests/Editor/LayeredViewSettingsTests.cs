using System;
using System.Collections.Generic;

using NUnit.Framework;

using SideXP.Database.EditorOnly;

namespace SideXP.Database.Tests
{

    /// <summary>
    /// Tests for the resolution of the layers of the settings of a view (<see cref="LayeredViewSettings"/>): each setting comes from
    /// the first layer that sets it, or from the defaults of the table, and the changes go to the top layer, which only keeps what
    /// differs from the layers below.
    /// </summary>
    /// <remarks>
    /// The tests with two layers stand for a personal override above a project view (step 23b). The table view only has one layer
    /// for now.
    /// </remarks>
    public class LayeredViewSettingsTests
    {

        #region Fields

        private const string TableId = "Tests.TableId";
        private const float LowerWidth = 100;
        private const float ResizedWidth = 150;

        #endregion


        #region Lifecycle

        /// <summary>
        /// The settings need at least one layer, and no null layer.
        /// </summary>
        [Test]
        public void Constructor_InvalidLayers_Throws()
        {
            Assert.Throws<ArgumentException>(() => new LayeredViewSettings(new ViewSettings[0]));
            Assert.Throws<ArgumentException>(() => new LayeredViewSettings(null));
            Assert.Throws<ArgumentException>(() => new LayeredViewSettings(new[] { new ViewSettings(TableId), null }));
        }

        #endregion


        #region Column order

        /// <summary>
        /// Without settings, the columns are in their default order.
        /// </summary>
        [Test]
        public void GetColumnOrder_NoSettings_DefaultOrder()
        {
            LayeredViewSettings settings = Create(new ViewSettings(TableId));
            CollectionAssert.AreEqual(new[] { "A", "B", "C" }, settings.GetColumnOrder(new[] { "A", "B", "C" }));
        }

        /// <summary>
        /// A column that the order doesn't contain comes right after the column that precedes it in the default order.
        /// </summary>
        [Test]
        public void GetColumnOrder_NewColumn_AfterItsPredecessor()
        {
            ViewSettings top = new ViewSettings(TableId);
            top.SetColumnOrder(new[] { "C", "A", "B" });

            CollectionAssert.AreEqual(new[] { "C", "A", "X", "B" }, Create(top).GetColumnOrder(new[] { "A", "X", "B", "C" }));
        }

        /// <summary>
        /// A new column that is first in the default order comes first.
        /// </summary>
        [Test]
        public void GetColumnOrder_NewFirstColumn_First()
        {
            ViewSettings top = new ViewSettings(TableId);
            top.SetColumnOrder(new[] { "B", "A" });

            CollectionAssert.AreEqual(new[] { "X", "B", "A" }, Create(top).GetColumnOrder(new[] { "X", "A", "B" }));
        }

        /// <summary>
        /// A column that doesn't exist anymore is ignored, and comes back at its place.
        /// </summary>
        [Test]
        public void GetColumnOrder_MissingColumn_IgnoredAndKept()
        {
            ViewSettings top = new ViewSettings(TableId);
            top.SetColumnOrder(new[] { "B", "C", "A" });
            LayeredViewSettings settings = Create(top);

            CollectionAssert.AreEqual(new[] { "C", "A" }, settings.GetColumnOrder(new[] { "A", "C" }));
            CollectionAssert.AreEqual(new[] { "B", "C", "A" }, settings.GetColumnOrder(new[] { "A", "B", "C" }));
        }

        /// <summary>
        /// The order comes from the first layer that sets it.
        /// </summary>
        [Test]
        public void GetColumnOrder_Layers_FirstSetOrder()
        {
            ViewSettings top = new ViewSettings(TableId);
            ViewSettings lower = new ViewSettings(TableId);
            lower.SetColumnOrder(new[] { "C", "B", "A" });
            LayeredViewSettings settings = Create(top, lower);

            CollectionAssert.AreEqual(new[] { "C", "B", "A" }, settings.GetColumnOrder(new[] { "A", "B", "C" }));

            top.SetColumnOrder(new[] { "B", "A", "C" });
            CollectionAssert.AreEqual(new[] { "B", "A", "C" }, settings.GetColumnOrder(new[] { "A", "B", "C" }));
        }

        /// <summary>
        /// Reordering some columns puts them in the places of the same columns, so the other columns keep theirs. The order is written
        /// to the top layer.
        /// </summary>
        [Test]
        public void SetColumnOrder_SomeColumns_OthersKeepTheirPlaces()
        {
            string[] columnIds = { "A", "B", "C", "D" };
            ViewSettings top = new ViewSettings(TableId);
            LayeredViewSettings settings = Create(top);

            Assert.IsTrue(settings.SetColumnOrder(columnIds, new[] { "D", "B" }));
            CollectionAssert.AreEqual(new[] { "A", "D", "C", "B" }, settings.GetColumnOrder(columnIds));
            CollectionAssert.AreEqual(new[] { "A", "D", "C", "B" }, top.ColumnOrder);
        }

        /// <summary>
        /// Setting the current order doesn't change anything.
        /// </summary>
        [Test]
        public void SetColumnOrder_SameOrder_False()
        {
            ViewSettings top = new ViewSettings(TableId);

            Assert.IsFalse(Create(top).SetColumnOrder(new[] { "A", "B", "C" }, new[] { "B", "C" }));
            Assert.IsFalse(top.IsColumnOrderSet);
        }

        /// <summary>
        /// The columns that are not in the order are ignored when reordering.
        /// </summary>
        [Test]
        public void SetColumnOrder_UnknownColumn_Ignored()
        {
            string[] columnIds = { "A", "B" };
            LayeredViewSettings settings = Create(new ViewSettings(TableId));

            Assert.IsTrue(settings.SetColumnOrder(columnIds, new[] { "B", "X", "A" }));
            CollectionAssert.AreEqual(new[] { "B", "A" }, settings.GetColumnOrder(columnIds));
        }

        /// <summary>
        /// A column that doesn't exist anymore keeps its place when the other columns are reordered.
        /// </summary>
        [Test]
        public void SetColumnOrder_MissingColumn_KeepsItsPlace()
        {
            ViewSettings top = new ViewSettings(TableId);
            top.SetColumnOrder(new[] { "B", "Missing", "A" });

            Assert.IsTrue(Create(top).SetColumnOrder(new[] { "A", "B" }, new[] { "A", "B" }));
            CollectionAssert.AreEqual(new[] { "A", "Missing", "B" }, top.ColumnOrder);
        }

        /// <summary>
        /// An order that becomes the order of the layers below is unset in the top layer.
        /// </summary>
        [Test]
        public void SetColumnOrder_BackToOrderBelow_Unset()
        {
            string[] columnIds = { "A", "B", "C" };
            ViewSettings top = new ViewSettings(TableId);
            LayeredViewSettings settings = Create(top);

            settings.SetColumnOrder(columnIds, new[] { "B", "A" });
            Assert.IsTrue(top.IsColumnOrderSet);

            Assert.IsTrue(settings.SetColumnOrder(columnIds, new[] { "A", "B" }));
            Assert.IsFalse(top.IsColumnOrderSet);
            CollectionAssert.AreEqual(columnIds, settings.GetColumnOrder(columnIds));
        }

        /// <summary>
        /// Reordering columns over the order of a lower layer writes the whole new order to the top layer, and doesn't change the
        /// lower layer.
        /// </summary>
        [Test]
        public void SetColumnOrder_OrderOfLowerLayer_WrittenToTop()
        {
            string[] columnIds = { "A", "B", "C" };
            ViewSettings top = new ViewSettings(TableId);
            ViewSettings lower = new ViewSettings(TableId);
            lower.SetColumnOrder(new[] { "C", "B", "A" });

            Assert.IsTrue(Create(top, lower).SetColumnOrder(columnIds, new[] { "A", "C" }));
            CollectionAssert.AreEqual(new[] { "A", "B", "C" }, top.ColumnOrder);
            CollectionAssert.AreEqual(new[] { "C", "B", "A" }, lower.ColumnOrder);
        }

        #endregion


        #region Widths

        /// <summary>
        /// A width comes from the first layer that sets it, or is the default width (0).
        /// </summary>
        [Test]
        public void GetWidth_Layers_FirstSetWidth()
        {
            ViewSettings top = new ViewSettings(TableId);
            ViewSettings lower = new ViewSettings(TableId);
            LayeredViewSettings settings = Create(top, lower);
            Assert.AreEqual(0, settings.GetWidth("A"));

            lower.SetWidth("A", LowerWidth);
            Assert.AreEqual(LowerWidth, settings.GetWidth("A"));

            top.SetWidth("A", ResizedWidth);
            Assert.AreEqual(ResizedWidth, settings.GetWidth("A"));
        }

        /// <summary>
        /// A width is written to the top layer, and setting the same width again changes nothing.
        /// </summary>
        [Test]
        public void SetWidth_Resized_WrittenToTop()
        {
            ViewSettings top = new ViewSettings(TableId);
            ViewSettings lower = new ViewSettings(TableId);
            LayeredViewSettings settings = Create(top, lower);

            Assert.IsTrue(settings.SetWidth("A", ResizedWidth));
            Assert.IsFalse(settings.SetWidth("A", ResizedWidth));
            Assert.AreEqual(ResizedWidth, top.GetWidth("A"));
            Assert.AreEqual(0, lower.GetWidth("A"));
        }

        /// <summary>
        /// A width that becomes the width of the layers below is unset in the top layer.
        /// </summary>
        [Test]
        public void SetWidth_SameAsBelow_Unset()
        {
            ViewSettings top = new ViewSettings(TableId);
            ViewSettings lower = new ViewSettings(TableId);
            lower.SetWidth("A", LowerWidth);
            LayeredViewSettings settings = Create(top, lower);

            settings.SetWidth("A", ResizedWidth);
            Assert.IsTrue(settings.SetWidth("A", LowerWidth));
            Assert.AreEqual(0, top.GetWidth("A"));
            Assert.AreEqual(LowerWidth, settings.GetWidth("A"));
        }

        #endregion


        #region Visibility

        /// <summary>
        /// Without any layer that sets the visibility of a column, it has its default visibility.
        /// </summary>
        [Test]
        public void IsHidden_NotSet_DefaultVisibility()
        {
            LayeredViewSettings settings = Create(columnId => columnId == "A", new ViewSettings(TableId));

            Assert.IsTrue(settings.IsHidden("A"));
            Assert.IsFalse(settings.IsHidden("B"));
            Assert.IsFalse(Create(new ViewSettings(TableId)).IsHidden("A"));
        }

        /// <summary>
        /// The visibility comes from the first layer that sets it.
        /// </summary>
        [Test]
        public void IsHidden_Layers_FirstSetVisibility()
        {
            ViewSettings top = new ViewSettings(TableId);
            ViewSettings lower = new ViewSettings(TableId);
            lower.SetVisibility("A", ColumnVisibility.Hidden);
            LayeredViewSettings settings = Create(top, lower);
            Assert.IsTrue(settings.IsHidden("A"));

            top.SetVisibility("A", ColumnVisibility.Visible);
            Assert.IsFalse(settings.IsHidden("A"));
        }

        /// <summary>
        /// Hiding a column is written to the top layer, and hiding it again changes nothing.
        /// </summary>
        [Test]
        public void SetHidden_Column_WrittenToTop()
        {
            ViewSettings top = new ViewSettings(TableId);
            LayeredViewSettings settings = Create(top);

            Assert.IsTrue(settings.SetHidden("B", true));
            Assert.IsFalse(settings.SetHidden("B", true));
            Assert.IsTrue(settings.IsHidden("B"));
            Assert.AreEqual(ColumnVisibility.Hidden, top.GetVisibility("B"));
        }

        /// <summary>
        /// A visibility that becomes the visibility of the layers below, or the default visibility, is unset in the top layer.
        /// </summary>
        [Test]
        public void SetHidden_BackToVisibilityBelow_Unset()
        {
            ViewSettings top = new ViewSettings(TableId);
            ViewSettings lower = new ViewSettings(TableId);
            lower.SetVisibility("B", ColumnVisibility.Hidden);
            LayeredViewSettings settings = Create(columnId => columnId == "A", top, lower);

            // Hidden by default
            settings.SetHidden("A", false);
            Assert.AreEqual(ColumnVisibility.Visible, top.GetVisibility("A"));
            Assert.IsTrue(settings.SetHidden("A", true));
            Assert.AreEqual(ColumnVisibility.Unset, top.GetVisibility("A"));

            // Hidden by the lower layer
            settings.SetHidden("B", false);
            Assert.AreEqual(ColumnVisibility.Visible, top.GetVisibility("B"));
            Assert.IsTrue(settings.SetHidden("B", true));
            Assert.AreEqual(ColumnVisibility.Unset, top.GetVisibility("B"));
        }

        #endregion


        #region Sort

        /// <summary>
        /// The sort comes from the first layer that sets it, and a layer that sets no sort removes the sort of the layers below.
        /// </summary>
        [Test]
        public void GetSort_Layers_FirstSetSort()
        {
            ViewSettings top = new ViewSettings(TableId);
            ViewSettings lower = new ViewSettings(TableId);
            lower.SetSort(new[] { new ColumnSort("A", true) });
            LayeredViewSettings settings = Create(top, lower);

            CollectionAssert.AreEqual(new[] { new ColumnSort("A", true) }, settings.GetSort());

            top.SetSort(new ColumnSort[0]);
            Assert.IsEmpty(settings.GetSort());
        }

        /// <summary>
        /// The columns that can't be sorted are skipped, like the sort of a single layer.
        /// </summary>
        [Test]
        public void GetSort_ColumnsThatCantBeSorted_Skipped()
        {
            ViewSettings top = new ViewSettings(TableId);
            top.SetSort(new[] { new ColumnSort("Missing", false), new ColumnSort("A", true) });
            HashSet<string> columnIds = new HashSet<string> { "A" };

            CollectionAssert.AreEqual(new[] { new ColumnSort("A", true) }, Create(top).GetSort(columnIds.Contains));
        }

        /// <summary>
        /// A sort is written to the top layer, and setting the same sort again changes nothing.
        /// </summary>
        [Test]
        public void SetSort_Sort_WrittenToTop()
        {
            ViewSettings top = new ViewSettings(TableId);
            ViewSettings lower = new ViewSettings(TableId);
            LayeredViewSettings settings = Create(top, lower);

            Assert.IsTrue(settings.SetSort(new[] { new ColumnSort("B", false) }));
            Assert.IsFalse(settings.SetSort(new[] { new ColumnSort("B", false) }));
            CollectionAssert.AreEqual(new[] { new ColumnSort("B", false) }, top.GetSort());
            Assert.IsFalse(lower.IsSortSet);
        }

        /// <summary>
        /// Removing the sort of a lower layer sets no sort in the top layer.
        /// </summary>
        [Test]
        public void SetSort_NoSortOverSortBelow_SetToNoSort()
        {
            ViewSettings top = new ViewSettings(TableId);
            ViewSettings lower = new ViewSettings(TableId);
            lower.SetSort(new[] { new ColumnSort("A", true) });

            Assert.IsTrue(Create(top, lower).SetSort(new ColumnSort[0]));
            Assert.IsTrue(top.IsSortSet);
            Assert.IsEmpty(top.GetSort());
        }

        /// <summary>
        /// A sort that becomes the sort of the layers below is unset in the top layer.
        /// </summary>
        [Test]
        public void SetSort_BackToSortBelow_Unset()
        {
            ViewSettings top = new ViewSettings(TableId);
            ViewSettings lower = new ViewSettings(TableId);
            lower.SetSort(new[] { new ColumnSort("A", true) });
            LayeredViewSettings settings = Create(top, lower);

            settings.SetSort(new[] { new ColumnSort("B", false) });
            Assert.IsTrue(settings.SetSort(new[] { new ColumnSort("A", true) }));
            Assert.IsFalse(top.IsSortSet);
        }

        #endregion


        #region Filters

        /// <summary>
        /// The filters come from the first layer that sets them, and a layer that sets no filter removes the filters of the layers
        /// below.
        /// </summary>
        [Test]
        public void Filter_Layers_FirstSetFilter()
        {
            ViewSettings top = new ViewSettings(TableId);
            ViewSettings lower = new ViewSettings(TableId);
            BoolCondition filter = new BoolCondition("A", true);
            lower.SetFilter(filter);
            LayeredViewSettings settings = Create(top, lower);

            Assert.AreSame(filter, settings.Filter);

            settings.SetFilter(null);
            Assert.IsNull(settings.Filter);
            Assert.IsTrue(top.IsFilterSet);
            Assert.AreSame(filter, lower.Filter);
        }

        #endregion


        #region Folders

        /// <summary>
        /// The folders come from the first layer that sets them, and are written to the top layer.
        /// </summary>
        [Test]
        public void SetFolders_Layers_WrittenToTop()
        {
            ViewSettings top = new ViewSettings(TableId);
            ViewSettings lower = new ViewSettings(TableId);
            lower.SetFolders(new[] { "Assets/Items" });
            LayeredViewSettings settings = Create(top, lower);
            CollectionAssert.AreEqual(new[] { "Assets/Items" }, settings.Folders);

            Assert.IsTrue(settings.SetFolders(new[] { "Assets/Weapons" }));
            Assert.IsFalse(settings.SetFolders(new[] { "Assets/Weapons" }));
            CollectionAssert.AreEqual(new[] { "Assets/Weapons" }, top.Folders);
            CollectionAssert.AreEqual(new[] { "Assets/Items" }, lower.Folders);
        }

        /// <summary>
        /// The whole project over the folders of a lower layer is set in the top layer, and the folders of the layers below are unset in
        /// the top layer.
        /// </summary>
        [Test]
        public void SetFolders_WholeProjectAndFoldersBelow_SetAndUnset()
        {
            ViewSettings top = new ViewSettings(TableId);
            ViewSettings lower = new ViewSettings(TableId);
            lower.SetFolders(new[] { "Assets/Items" });
            LayeredViewSettings settings = Create(top, lower);

            Assert.IsTrue(settings.SetFolders(null));
            Assert.IsTrue(top.AreFoldersSet);
            Assert.IsEmpty(settings.Folders);

            Assert.IsTrue(settings.SetFolders(new[] { "Assets/Items" }));
            Assert.IsFalse(top.AreFoldersSet);
        }

        #endregion


        #region Reset

        /// <summary>
        /// Resetting unsets the settings of the columns and the sort in the top layer, so the settings of the layers below apply.
        /// </summary>
        [Test]
        public void Reset_Top_SettingsBelowApply()
        {
            ViewSettings top = new ViewSettings(TableId);
            ViewSettings lower = new ViewSettings(TableId);
            lower.SetWidth("A", LowerWidth);
            lower.SetSort(new[] { new ColumnSort("A", true) });
            LayeredViewSettings settings = Create(top, lower);
            settings.SetWidth("A", ResizedWidth);
            settings.SetSort(new ColumnSort[0]);

            Assert.IsTrue(settings.Reset());
            Assert.AreEqual(LowerWidth, settings.GetWidth("A"));
            CollectionAssert.AreEqual(new[] { new ColumnSort("A", true) }, settings.GetSort());
        }

        #endregion


        #region Private API

        /// <summary>
        /// Creates settings resolved from layers, with all the columns visible by default.
        /// </summary>
        /// <param name="layers">The layers, from the top.</param>
        /// <returns>Returns the created settings.</returns>
        private static LayeredViewSettings Create(params ViewSettings[] layers)
        {
            return new LayeredViewSettings(layers);
        }

        /// <summary>
        /// Creates settings resolved from layers.
        /// </summary>
        /// <param name="isHiddenByDefault">Checks if a column is hidden when no layer sets its visibility.</param>
        /// <param name="layers">The layers, from the top.</param>
        /// <returns>Returns the created settings.</returns>
        private static LayeredViewSettings Create(Predicate<string> isHiddenByDefault, params ViewSettings[] layers)
        {
            return new LayeredViewSettings(layers, isHiddenByDefault);
        }

        #endregion

    }

}
