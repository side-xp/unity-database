using System.Collections.Generic;

using NUnit.Framework;

using UnityEngine;

using SideXP.Database.EditorOnly;

namespace SideXP.Database.Tests
{

    /// <summary>
    /// Tests for the remembered settings of the tables (<see cref="ViewSettings"/>): column order, widths, visibility and sort on one
    /// or several columns.
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


        #region Visibility

        /// <summary>
        /// The columns are visible by default, unless the default visibility given for a new column hides it.
        /// </summary>
        [Test]
        public void GetColumnOrder_HiddenByDefault_Hidden()
        {
            ViewSettings settings = new ViewSettings(TableId);
            List<string> order = settings.GetColumnOrder(new[] { "A", "B" }, columnId => columnId == "A");

            CollectionAssert.AreEqual(new[] { "A", "B" }, order);
            Assert.IsTrue(settings.IsHidden("A"));
            Assert.IsFalse(settings.IsHidden("B"));
            Assert.IsFalse(settings.IsHidden("Unknown"));
        }

        /// <summary>
        /// The default visibility only applies to the columns that have no settings yet.
        /// </summary>
        [Test]
        public void GetColumnOrder_HiddenByDefaultShownByUser_Visible()
        {
            ViewSettings settings = new ViewSettings(TableId);
            settings.GetColumnOrder(new[] { "A" }, columnId => true);
            settings.SetHidden("A", false);

            settings.GetColumnOrder(new[] { "A" }, columnId => true);
            Assert.IsFalse(settings.IsHidden("A"));
        }

        /// <summary>
        /// Hiding a column is remembered, and hiding it again changes nothing. A hidden column keeps its place in the order.
        /// </summary>
        [Test]
        public void SetHidden_Column_RememberedAtItsPlace()
        {
            ViewSettings settings = new ViewSettings(TableId);
            settings.GetColumnOrder(new[] { "A", "B", "C" });

            Assert.IsTrue(settings.SetHidden("B", true));
            Assert.IsFalse(settings.SetHidden("B", true));
            Assert.IsTrue(settings.IsHidden("B"));

            // The visible columns are reordered without the hidden one, which keeps its place
            settings.SetColumnOrder(new[] { "C", "A" });
            CollectionAssert.AreEqual(new[] { "C", "B", "A" }, settings.GetColumnOrder(new[] { "A", "B", "C" }));
        }

        #endregion


        #region Reset

        /// <summary>
        /// Resetting removes the order, the widths, the visibility and the sort.
        /// </summary>
        [Test]
        public void Reset_Customized_DefaultSettings()
        {
            ViewSettings settings = new ViewSettings(TableId);
            settings.GetColumnOrder(new[] { "A", "B" });
            settings.SetColumnOrder(new[] { "B", "A" });
            settings.SetWidth("A", ResizedWidth);
            settings.SetHidden("B", true);
            settings.SetSort("A", true);

            Assert.IsTrue(settings.Reset());
            CollectionAssert.AreEqual(new[] { "A", "B" }, settings.GetColumnOrder(new[] { "A", "B" }));
            Assert.AreEqual(0, settings.GetWidth("A"));
            Assert.IsFalse(settings.IsHidden("B"));
            Assert.IsFalse(settings.HasSort);
        }

        /// <summary>
        /// Resetting settings that have nothing changes nothing.
        /// </summary>
        [Test]
        public void Reset_Empty_False()
        {
            Assert.IsFalse(new ViewSettings(TableId).Reset());
        }

        /// <summary>
        /// Resetting keeps the filters and the folder scope, which are cleared on their own.
        /// </summary>
        [Test]
        public void Reset_FiltersAndFolders_Kept()
        {
            ViewSettings settings = new ViewSettings(TableId);
            BoolCondition filter = new BoolCondition("A", true);
            settings.Filter = filter;
            settings.SetFolders(new[] { "Assets/Items" });

            Assert.IsFalse(settings.Reset());
            Assert.AreSame(filter, settings.Filter);
            CollectionAssert.AreEqual(new[] { "Assets/Items" }, settings.Folders);
        }

        #endregion


        #region Filters and folders

        /// <summary>
        /// The folders are remembered in their order, and setting the same folders again changes nothing.
        /// </summary>
        [Test]
        public void SetFolders_Folders_RememberedInOrder()
        {
            ViewSettings settings = new ViewSettings(TableId);
            Assert.IsEmpty(settings.Folders);

            Assert.IsTrue(settings.SetFolders(new[] { "Assets/Items/Weapons", "Assets/Items/Shields" }));
            Assert.IsFalse(settings.SetFolders(new[] { "Assets/Items/Weapons", "Assets/Items/Shields" }));
            CollectionAssert.AreEqual(new[] { "Assets/Items/Weapons", "Assets/Items/Shields" }, settings.Folders);

            Assert.IsTrue(settings.SetFolders(new[] { "Assets/Items/Shields", "Assets/Items/Weapons" }));
            Assert.IsTrue(settings.SetFolders(null));
            Assert.IsEmpty(settings.Folders);
        }

        /// <summary>
        /// The filter tree and the folders survive Unity's serialization, which <see cref="UserViews"/> uses to store them.
        /// </summary>
        [Test]
        public void Serialization_FiltersAndFolders_Kept()
        {
            ViewSettingsTestSO holder = ScriptableObject.CreateInstance<ViewSettingsTestSO>();
            ViewSettingsTestSO copy = null;
            try
            {
                holder.settings = new ViewSettings(TableId);
                holder.settings.Filter = new FilterGroup(FilterOperator.Or,
                    new BoolCondition("A", false),
                    new TextCondition("B", "sword"));
                holder.settings.SetFolders(new[] { "Assets/Items" });

                // Instantiating copies the object through its serialized data
                copy = Object.Instantiate(holder);

                FilterGroup root = copy.settings.Filter as FilterGroup;
                Assert.IsNotNull(root);
                Assert.AreNotSame(holder.settings.Filter, root);
                Assert.AreEqual(FilterOperator.Or, root.Operator);
                Assert.IsInstanceOf<BoolCondition>(root.Nodes[0]);
                Assert.AreEqual("sword", ((TextCondition)root.Nodes[1]).Text);
                CollectionAssert.AreEqual(new[] { "Assets/Items" }, copy.settings.Folders);
                Assert.AreEqual(TableId, copy.settings.TableId);
            }
            finally
            {
                Object.DestroyImmediate(holder);
                if (copy != null)
                    Object.DestroyImmediate(copy);
            }
        }

        #endregion


        #region Sort

        /// <summary>
        /// A sort on a single column is remembered with its direction, and setting the same sort again changes nothing.
        /// </summary>
        [Test]
        public void SetSort_Column_Remembered()
        {
            ViewSettings settings = new ViewSettings(TableId);

            Assert.IsTrue(settings.SetSort("A", true));
            Assert.IsFalse(settings.SetSort("A", true));
            Assert.IsTrue(settings.HasSort);
            CollectionAssert.AreEqual(new[] { new ColumnSort("A", true) }, settings.GetSort());
        }

        /// <summary>
        /// A sort on several columns is remembered in its order, each column with its direction, and setting the same sort again
        /// changes nothing.
        /// </summary>
        [Test]
        public void SetSort_SeveralColumns_RememberedInOrder()
        {
            ViewSettings settings = new ViewSettings(TableId);
            ColumnSort[] sort = { new ColumnSort("B", false), new ColumnSort("A", true) };

            Assert.IsTrue(settings.SetSort(sort));
            Assert.IsFalse(settings.SetSort(new[] { new ColumnSort("B", false), new ColumnSort("A", true) }));
            CollectionAssert.AreEqual(sort, settings.GetSort());

            Assert.IsTrue(settings.SetSort(new[] { new ColumnSort("B", false), new ColumnSort("A", false) }));
            Assert.IsTrue(settings.SetSort(new[] { new ColumnSort("A", false), new ColumnSort("B", false) }));
        }

        /// <summary>
        /// The columns that can't be sorted (for example missing or hidden columns) are skipped, and the next ones take their
        /// priority. They are still remembered.
        /// </summary>
        [Test]
        public void GetSort_MissingColumns_Skipped()
        {
            ViewSettings settings = new ViewSettings(TableId);
            settings.SetSort(new[] { new ColumnSort("Missing", false), new ColumnSort("A", true), new ColumnSort("B", false) });
            HashSet<string> columnIds = new HashSet<string> { "A", "B" };

            CollectionAssert.AreEqual(new[] { new ColumnSort("A", true), new ColumnSort("B", false) },
                settings.GetSort(columnIds.Contains));
            Assert.AreEqual(3, settings.GetSort().Count);
        }

        /// <summary>
        /// The entries without column and the duplicates of a column, which a file edited by hand can contain, are skipped.
        /// </summary>
        [Test]
        public void GetSort_InvalidEntries_Skipped()
        {
            ViewSettings settings = new ViewSettings(TableId);
            settings.SetSort(new[] { new ColumnSort("A", true), new ColumnSort(null, false), new ColumnSort("A", false) });

            CollectionAssert.AreEqual(new[] { new ColumnSort("A", true) }, settings.GetSort());
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
            Assert.IsEmpty(settings.GetSort());

            settings.SetSort(new[] { new ColumnSort("A", false), new ColumnSort("B", true) });
            Assert.IsTrue(settings.SetSort(new ColumnSort[0]));
            Assert.IsFalse(settings.HasSort);
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
            settings.SetHidden("A", true);
            settings.SetSort(new[] { new ColumnSort("C", true), new ColumnSort("B", false) });

            ViewSettings copy = new ViewSettings(string.Empty);
            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(settings), copy);

            Assert.AreEqual(TableId, copy.TableId);
            CollectionAssert.AreEqual(new[] { "A", "C", "B" }, copy.GetColumnOrder(new[] { "A", "B", "C" }));
            Assert.AreEqual(ResizedWidth, copy.GetWidth("B"));
            Assert.IsTrue(copy.IsHidden("A"));
            CollectionAssert.AreEqual(new[] { new ColumnSort("C", true), new ColumnSort("B", false) }, copy.GetSort());
        }

        /// <summary>
        /// The single sort remembered before the sort on several columns is read as a sort on one column.
        /// </summary>
        [Test]
        public void Serialization_SingleSortOfFirstPass_ReadAsList()
        {
            string json = $"{{\"_tableId\":\"{TableId}\",\"_sortColumnId\":\"A\",\"_isSortDescending\":true}}";
            ViewSettings settings = new ViewSettings(string.Empty);
            JsonUtility.FromJsonOverwrite(json, settings);

            CollectionAssert.AreEqual(new[] { new ColumnSort("A", true) }, settings.GetSort());

            // The single sort is not saved again, so it doesn't come back once the user clears the sort
            settings.ClearSort();
            ViewSettings copy = new ViewSettings(string.Empty);
            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(settings), copy);
            Assert.IsFalse(copy.HasSort);
        }

        /// <summary>
        /// The single sort of the first pass doesn't replace a sort on several columns found in the same settings.
        /// </summary>
        [Test]
        public void Serialization_SingleSortAndList_ListKept()
        {
            string json = $"{{\"_tableId\":\"{TableId}\",\"_sort\":[{{\"_columnId\":\"B\",\"_isDescending\":false}}]," +
                "\"_sortColumnId\":\"A\",\"_isSortDescending\":true}";
            ViewSettings settings = new ViewSettings(string.Empty);
            JsonUtility.FromJsonOverwrite(json, settings);

            CollectionAssert.AreEqual(new[] { new ColumnSort("B", false) }, settings.GetSort());
        }

        #endregion

    }

}
