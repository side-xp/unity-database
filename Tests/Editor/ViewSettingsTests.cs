using System;
using System.Collections.Generic;

using NUnit.Framework;

using UnityEngine;

using SideXP.Database.EditorOnly;

using Object = UnityEngine.Object;

namespace SideXP.Database.Tests
{

    /// <summary>
    /// Tests for a layer of the settings of a view (<see cref="ViewSettings"/>): each setting set and unset, the copy, the serialization,
    /// and the settings saved before the views. The resolution of the layers is tested by <see cref="LayeredViewSettingsTests"/>.
    /// </summary>
    public class ViewSettingsTests
    {

        #region Fields

        private const string TableId = "Tests.TableId";
        private const float ResizedWidth = 150;

        #endregion


        #region Columns

        /// <summary>
        /// The order of the columns is not set by default, is stored as given (columns that don't exist anymore included), and is unset
        /// with an empty order.
        /// </summary>
        [Test]
        public void SetColumnOrder_Order_StoredAsGiven()
        {
            ViewSettings settings = new ViewSettings(TableId);
            Assert.IsFalse(settings.IsColumnOrderSet);

            Assert.IsTrue(settings.SetColumnOrder(new[] { "C", "Missing", "A" }));
            Assert.IsFalse(settings.SetColumnOrder(new[] { "C", "Missing", "A" }));
            Assert.IsTrue(settings.IsColumnOrderSet);
            CollectionAssert.AreEqual(new[] { "C", "Missing", "A" }, settings.ColumnOrder);

            Assert.IsTrue(settings.SetColumnOrder(null));
            Assert.IsFalse(settings.IsColumnOrderSet);
        }

        /// <summary>
        /// A width is not set (0) until the user resizes the column, is remembered, and setting the same width again changes nothing.
        /// </summary>
        [Test]
        public void SetWidth_Resized_Remembered()
        {
            ViewSettings settings = new ViewSettings(TableId);
            Assert.AreEqual(0, settings.GetWidth("A"));

            Assert.IsTrue(settings.SetWidth("A", ResizedWidth));
            Assert.IsFalse(settings.SetWidth("A", ResizedWidth));
            Assert.AreEqual(ResizedWidth, settings.GetWidth("A"));
        }

        /// <summary>
        /// A width of 0 or less unsets the width.
        /// </summary>
        [Test]
        public void SetWidth_Negative_Unset()
        {
            ViewSettings settings = new ViewSettings(TableId);
            settings.SetWidth("A", ResizedWidth);

            Assert.IsTrue(settings.SetWidth("A", -1));
            Assert.AreEqual(0, settings.GetWidth("A"));
            Assert.IsFalse(settings.SetWidth("A", 0));
        }

        /// <summary>
        /// The visibility of a column is not set by default, is remembered, and can be unset.
        /// </summary>
        [Test]
        public void SetVisibility_Visibility_Remembered()
        {
            ViewSettings settings = new ViewSettings(TableId);
            Assert.AreEqual(ColumnVisibility.Unset, settings.GetVisibility("A"));

            Assert.IsTrue(settings.SetVisibility("A", ColumnVisibility.Hidden));
            Assert.IsFalse(settings.SetVisibility("A", ColumnVisibility.Hidden));
            Assert.AreEqual(ColumnVisibility.Hidden, settings.GetVisibility("A"));

            Assert.IsTrue(settings.SetVisibility("A", ColumnVisibility.Unset));
            Assert.AreEqual(ColumnVisibility.Unset, settings.GetVisibility("A"));
        }

        /// <summary>
        /// The width and the visibility of a column are independent: unsetting one keeps the other.
        /// </summary>
        [Test]
        public void SetVisibility_Unset_WidthKept()
        {
            ViewSettings settings = new ViewSettings(TableId);
            settings.SetWidth("A", ResizedWidth);
            settings.SetVisibility("A", ColumnVisibility.Hidden);

            settings.SetVisibility("A", ColumnVisibility.Unset);
            Assert.AreEqual(ResizedWidth, settings.GetWidth("A"));

            settings.SetVisibility("A", ColumnVisibility.Visible);
            settings.SetWidth("A", 0);
            Assert.AreEqual(ColumnVisibility.Visible, settings.GetVisibility("A"));
        }

        #endregion


        #region Sort

        /// <summary>
        /// The sort is not set by default.
        /// </summary>
        [Test]
        public void GetSort_NotSet_Empty()
        {
            ViewSettings settings = new ViewSettings(TableId);

            Assert.IsFalse(settings.IsSortSet);
            Assert.IsEmpty(settings.GetSort());
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
            Assert.IsTrue(settings.IsSortSet);
            CollectionAssert.AreEqual(sort, settings.GetSort());

            Assert.IsTrue(settings.SetSort(new[] { new ColumnSort("B", false), new ColumnSort("A", false) }));
            Assert.IsTrue(settings.SetSort(new[] { new ColumnSort("A", false), new ColumnSort("B", false) }));
        }

        /// <summary>
        /// An empty sort is a set sort (no sort), not an unset one.
        /// </summary>
        [Test]
        public void SetSort_Empty_SetToNoSort()
        {
            ViewSettings settings = new ViewSettings(TableId);

            Assert.IsTrue(settings.SetSort(new ColumnSort[0]));
            Assert.IsFalse(settings.SetSort(new ColumnSort[0]));
            Assert.IsTrue(settings.IsSortSet);
            Assert.IsEmpty(settings.GetSort());
        }

        /// <summary>
        /// A null sort is refused: the sort is unset with <see cref="ViewSettings.UnsetSort"/>.
        /// </summary>
        [Test]
        public void SetSort_Null_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new ViewSettings(TableId).SetSort(null));
        }

        /// <summary>
        /// Unsetting the sort forgets its columns.
        /// </summary>
        [Test]
        public void UnsetSort_Set_Unset()
        {
            ViewSettings settings = new ViewSettings(TableId);
            settings.SetSort(new[] { new ColumnSort("A", true) });

            Assert.IsTrue(settings.UnsetSort());
            Assert.IsFalse(settings.UnsetSort());
            Assert.IsFalse(settings.IsSortSet);
            Assert.IsEmpty(settings.GetSort());
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

        #endregion


        #region Filters and folders

        /// <summary>
        /// The filters are not set by default, a tree is remembered by reference, and a null filter is a set filter (no filter).
        /// </summary>
        [Test]
        public void SetFilter_TreeAndNull_Set()
        {
            ViewSettings settings = new ViewSettings(TableId);
            Assert.IsFalse(settings.IsFilterSet);

            BoolCondition filter = new BoolCondition("A", true);
            settings.SetFilter(filter);
            Assert.AreSame(filter, settings.Filter);

            settings.SetFilter(null);
            Assert.IsTrue(settings.IsFilterSet);
            Assert.IsNull(settings.Filter);
        }

        /// <summary>
        /// Unsetting the filters forgets the tree.
        /// </summary>
        [Test]
        public void UnsetFilter_Set_Unset()
        {
            ViewSettings settings = new ViewSettings(TableId);
            settings.SetFilter(new BoolCondition("A", true));

            Assert.IsTrue(settings.UnsetFilter());
            Assert.IsFalse(settings.UnsetFilter());
            Assert.IsFalse(settings.IsFilterSet);
            Assert.IsNull(settings.Filter);
        }

        /// <summary>
        /// The folders are remembered in their order, setting the same folders again changes nothing, and no folder is a set scope (the
        /// whole project).
        /// </summary>
        [Test]
        public void SetFolders_Folders_RememberedInOrder()
        {
            ViewSettings settings = new ViewSettings(TableId);
            Assert.IsFalse(settings.AreFoldersSet);
            Assert.IsEmpty(settings.Folders);

            Assert.IsTrue(settings.SetFolders(new[] { "Assets/Items/Weapons", "Assets/Items/Shields" }));
            Assert.IsFalse(settings.SetFolders(new[] { "Assets/Items/Weapons", "Assets/Items/Shields" }));
            CollectionAssert.AreEqual(new[] { "Assets/Items/Weapons", "Assets/Items/Shields" }, settings.Folders);

            Assert.IsTrue(settings.SetFolders(new[] { "Assets/Items/Shields", "Assets/Items/Weapons" }));
            Assert.IsTrue(settings.SetFolders(null));
            Assert.IsTrue(settings.AreFoldersSet);
            Assert.IsEmpty(settings.Folders);
        }

        /// <summary>
        /// Unsetting the folders forgets them.
        /// </summary>
        [Test]
        public void UnsetFolders_Set_Unset()
        {
            ViewSettings settings = new ViewSettings(TableId);
            settings.SetFolders(new[] { "Assets/Items" });

            Assert.IsTrue(settings.UnsetFolders());
            Assert.IsFalse(settings.UnsetFolders());
            Assert.IsFalse(settings.AreFoldersSet);
            Assert.IsEmpty(settings.Folders);
        }

        #endregion


        #region Empty

        /// <summary>
        /// A layer is empty until it sets a setting of the view.
        /// </summary>
        [Test]
        public void IsEmpty_EachSetting_NotEmpty()
        {
            ViewSettings settings = new ViewSettings(TableId);
            Assert.IsTrue(settings.IsEmpty);

            settings.SetColumnOrder(new[] { "A" });
            Assert.IsFalse(settings.IsEmpty);
            settings.SetColumnOrder(null);

            settings.SetVisibility("A", ColumnVisibility.Hidden);
            Assert.IsFalse(settings.IsEmpty);
            settings.SetVisibility("A", ColumnVisibility.Unset);
            Assert.IsTrue(settings.IsEmpty);

            settings.SetSort(new ColumnSort[0]);
            Assert.IsFalse(settings.IsEmpty);
            settings.UnsetSort();

            settings.SetFilter(null);
            Assert.IsFalse(settings.IsEmpty);
            settings.UnsetFilter();

            settings.SetFolders(null);
            Assert.IsFalse(settings.IsEmpty);
        }

        #endregion


        #region Kinds of settings

        /// <summary>
        /// The count of a kind of setting is the number of columns for the widths and the visibility, and 1 or 0 for the others.
        /// </summary>
        [Test]
        public void GetSetCount_EachKind_Counted()
        {
            ViewSettings settings = new ViewSettings(TableId);
            foreach (ViewSettingKind kind in ViewSettings.AllKinds)
                Assert.AreEqual(0, settings.GetSetCount(kind), kind.ToString());

            settings.SetColumnOrder(new[] { "B", "A" });
            settings.SetWidth("A", ResizedWidth);
            settings.SetWidth("B", ResizedWidth);
            settings.SetVisibility("B", ColumnVisibility.Hidden);
            settings.SetSort(new ColumnSort[0]);
            settings.SetFilter(null);
            settings.SetFolders(null);

            Assert.AreEqual(1, settings.GetSetCount(ViewSettingKind.ColumnOrder));
            Assert.AreEqual(2, settings.GetSetCount(ViewSettingKind.ColumnWidths));
            Assert.AreEqual(1, settings.GetSetCount(ViewSettingKind.ColumnVisibility));
            Assert.AreEqual(1, settings.GetSetCount(ViewSettingKind.Sort));
            Assert.AreEqual(1, settings.GetSetCount(ViewSettingKind.Filters));
            Assert.AreEqual(1, settings.GetSetCount(ViewSettingKind.Folders));
            Assert.IsTrue(settings.IsSet(ViewSettingKind.ColumnWidths));
        }

        /// <summary>
        /// Unsetting a kind of setting keeps the other kinds, the other settings of the same columns included.
        /// </summary>
        [Test]
        public void Unset_Kind_OtherKindsKept()
        {
            ViewSettings settings = new ViewSettings(TableId);
            settings.SetColumnOrder(new[] { "B", "A" });
            settings.SetWidth("A", ResizedWidth);
            settings.SetVisibility("A", ColumnVisibility.Hidden);

            Assert.IsTrue(settings.Unset(ViewSettingKind.ColumnWidths));
            Assert.IsFalse(settings.Unset(ViewSettingKind.ColumnWidths));
            Assert.AreEqual(0, settings.GetWidth("A"));
            Assert.AreEqual(ColumnVisibility.Hidden, settings.GetVisibility("A"));
            Assert.IsTrue(settings.IsColumnOrderSet);

            Assert.IsTrue(settings.Unset(ViewSettingKind.ColumnVisibility));
            Assert.AreEqual(ColumnVisibility.Unset, settings.GetVisibility("A"));
            Assert.IsFalse(settings.Unset(ViewSettingKind.Sort));
        }

        /// <summary>
        /// Unsetting all the settings leaves an empty layer.
        /// </summary>
        [Test]
        public void UnsetAll_Settings_Empty()
        {
            ViewSettings settings = new ViewSettings(TableId);
            settings.SetColumnOrder(new[] { "B", "A" });
            settings.SetWidth("A", ResizedWidth);
            settings.SetSort(new ColumnSort[0]);
            settings.SetFilter(new BoolCondition("A", true));
            settings.SetFolders(null);

            Assert.IsTrue(settings.UnsetAll());
            Assert.IsTrue(settings.IsEmpty);
            Assert.IsFalse(settings.UnsetAll());
        }

        /// <summary>
        /// Applying a layer to another one writes the settings it sets, per column for the widths and the visibility, and keeps the
        /// other settings of the other layer. The filter tree is copied.
        /// </summary>
        [Test]
        public void ApplyTo_SetSettings_WrittenOthersKept()
        {
            ViewSettings source = new ViewSettings(TableId);
            source.SetWidth("B", ResizedWidth);
            source.SetVisibility("C", ColumnVisibility.Visible);
            source.SetSort(new[] { new ColumnSort("B", true) });
            BoolCondition filter = new BoolCondition("A", false);
            source.SetFilter(filter);

            ViewSettings target = new ViewSettings(TableId);
            target.SetColumnOrder(new[] { "C", "B", "A" });
            target.SetWidth("A", 100);
            target.SetVisibility("C", ColumnVisibility.Hidden);
            target.SetSort(new[] { new ColumnSort("A", false) });
            target.SetFolders(new[] { "Assets/Items" });

            Assert.IsTrue(source.ApplyTo(target));
            CollectionAssert.AreEqual(new[] { "C", "B", "A" }, target.ColumnOrder);
            Assert.AreEqual(100, target.GetWidth("A"));
            Assert.AreEqual(ResizedWidth, target.GetWidth("B"));
            Assert.AreEqual(ColumnVisibility.Visible, target.GetVisibility("C"));
            CollectionAssert.AreEqual(new[] { new ColumnSort("B", true) }, target.GetSort());
            Assert.IsInstanceOf<BoolCondition>(target.Filter);
            Assert.AreNotSame(filter, target.Filter);
            CollectionAssert.AreEqual(new[] { "Assets/Items" }, target.Folders);

            Assert.IsFalse(new ViewSettings(TableId).ApplyTo(target));
            Assert.Throws<ArgumentNullException>(() => source.ApplyTo(null));
        }

        #endregion


        #region Reset

        /// <summary>
        /// Resetting unsets the order, the widths, the visibility and the sort.
        /// </summary>
        [Test]
        public void Reset_Customized_Unset()
        {
            ViewSettings settings = new ViewSettings(TableId);
            settings.SetColumnOrder(new[] { "B", "A" });
            settings.SetWidth("A", ResizedWidth);
            settings.SetVisibility("B", ColumnVisibility.Hidden);
            settings.SetSort(new ColumnSort[0]);

            Assert.IsTrue(settings.Reset());
            Assert.IsFalse(settings.IsColumnOrderSet);
            Assert.AreEqual(0, settings.GetWidth("A"));
            Assert.AreEqual(ColumnVisibility.Unset, settings.GetVisibility("B"));
            Assert.IsFalse(settings.IsSortSet);
        }

        /// <summary>
        /// Resetting settings that set nothing changes nothing.
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
            settings.SetFilter(filter);
            settings.SetFolders(new[] { "Assets/Items" });

            Assert.IsFalse(settings.Reset());
            Assert.AreSame(filter, settings.Filter);
            CollectionAssert.AreEqual(new[] { "Assets/Items" }, settings.Folders);
        }

        #endregion


        #region Clone

        /// <summary>
        /// A copy has the same settings, and changing it doesn't change the original, the nodes of the filter tree included.
        /// </summary>
        [Test]
        public void Clone_Settings_SameAndIndependent()
        {
            ViewSettings settings = new ViewSettings(TableId);
            settings.SetColumnOrder(new[] { "B", "A" });
            settings.SetWidth("A", ResizedWidth);
            settings.SetVisibility("B", ColumnVisibility.Hidden);
            settings.SetSort(new[] { new ColumnSort("A", true) });
            settings.SetFolders(new[] { "Assets/Items" });
            // Any enum value works: the condition only stores its number
            EnumCondition condition = new EnumCondition("C", FilterOperator.Or);
            settings.SetFilter(new FilterGroup(FilterOperator.And, condition));

            ViewSettings copy = settings.Clone();
            Assert.AreEqual(TableId, copy.TableId);
            CollectionAssert.AreEqual(new[] { "B", "A" }, copy.ColumnOrder);
            Assert.AreEqual(ResizedWidth, copy.GetWidth("A"));
            Assert.AreEqual(ColumnVisibility.Hidden, copy.GetVisibility("B"));
            CollectionAssert.AreEqual(new[] { new ColumnSort("A", true) }, copy.GetSort());
            CollectionAssert.AreEqual(new[] { "Assets/Items" }, copy.Folders);

            FilterGroup copiedRoot = copy.Filter as FilterGroup;
            Assert.IsNotNull(copiedRoot);
            Assert.AreNotSame(settings.Filter, copiedRoot);
            EnumCondition copiedCondition = copiedRoot.Nodes[0] as EnumCondition;
            Assert.IsNotNull(copiedCondition);
            Assert.AreNotSame(condition, copiedCondition);

            copy.SetColumnOrder(new[] { "A", "B" });
            copy.SetWidth("A", 0);
            copy.SetVisibility("B", ColumnVisibility.Visible);
            copy.SetSort(new ColumnSort[0]);
            copy.SetFolders(null);
            copiedRoot.Nodes.Add(new BoolCondition("D", true));
            copiedCondition.Values.Clear();

            CollectionAssert.AreEqual(new[] { "B", "A" }, settings.ColumnOrder);
            Assert.AreEqual(ResizedWidth, settings.GetWidth("A"));
            Assert.AreEqual(ColumnVisibility.Hidden, settings.GetVisibility("B"));
            Assert.AreEqual(1, settings.GetSort().Count);
            Assert.AreEqual(1, settings.Folders.Count);
            Assert.AreEqual(1, ((FilterGroup)settings.Filter).Nodes.Count);
            Assert.AreEqual(1, condition.Values.Count);
        }

        #endregion


        #region Serialization

        /// <summary>
        /// The settings survive Unity's serialization, which is used to store them, and the unset settings stay unset.
        /// </summary>
        [Test]
        public void Serialization_RoundTrip_SettingsKept()
        {
            ViewSettings settings = new ViewSettings(TableId);
            settings.SetColumnOrder(new[] { "A", "C", "B" });
            settings.SetWidth("B", ResizedWidth);
            settings.SetVisibility("A", ColumnVisibility.Hidden);
            settings.SetSort(new[] { new ColumnSort("C", true), new ColumnSort("B", false) });

            ViewSettings copy = new ViewSettings(string.Empty);
            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(settings), copy);

            Assert.AreEqual(TableId, copy.TableId);
            CollectionAssert.AreEqual(new[] { "A", "C", "B" }, copy.ColumnOrder);
            Assert.AreEqual(ResizedWidth, copy.GetWidth("B"));
            Assert.AreEqual(ColumnVisibility.Hidden, copy.GetVisibility("A"));
            // Not read as the settings saved before the views, where every column had a visibility
            Assert.AreEqual(ColumnVisibility.Unset, copy.GetVisibility("B"));
            CollectionAssert.AreEqual(new[] { new ColumnSort("C", true), new ColumnSort("B", false) }, copy.GetSort());
            Assert.IsFalse(copy.IsFilterSet);
            Assert.IsFalse(copy.AreFoldersSet);
        }

        /// <summary>
        /// Settings without anything set stay without anything set.
        /// </summary>
        [Test]
        public void Serialization_Empty_NothingSet()
        {
            ViewSettings copy = new ViewSettings(string.Empty);
            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(new ViewSettings(TableId)), copy);

            Assert.IsFalse(copy.IsColumnOrderSet);
            Assert.IsFalse(copy.IsSortSet);
            Assert.IsFalse(copy.IsFilterSet);
            Assert.IsFalse(copy.AreFoldersSet);
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
                holder.settings.SetFilter(new FilterGroup(FilterOperator.Or,
                    new BoolCondition("A", false),
                    new TextCondition("B", "sword")));
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

        /// <summary>
        /// The settings saved before the views are read with every setting set: the list of the columns gives their order and each
        /// column keeps its visibility. An empty sort, filter or folder scope is read as unset, which gives the same rows.
        /// </summary>
        [Test]
        public void Serialization_SettingsOfFirstPass_Read()
        {
            string json = $"{{\"_tableId\":\"{TableId}\",\"_columns\":[" +
                "{\"_id\":\"B\",\"_width\":150,\"_isHidden\":false}," +
                "{\"_id\":\"A\",\"_width\":0,\"_isHidden\":true}," +
                "{\"_id\":\"C\",\"_width\":0,\"_isHidden\":false}]," +
                "\"_sort\":[{\"_columnId\":\"A\",\"_isDescending\":true}],\"_folders\":[],\"_pageSize\":50}";
            ViewSettings settings = new ViewSettings(string.Empty);
            JsonUtility.FromJsonOverwrite(json, settings);

            CollectionAssert.AreEqual(new[] { "B", "A", "C" }, settings.ColumnOrder);
            Assert.AreEqual(ResizedWidth, settings.GetWidth("B"));
            Assert.AreEqual(ColumnVisibility.Visible, settings.GetVisibility("B"));
            Assert.AreEqual(ColumnVisibility.Hidden, settings.GetVisibility("A"));
            Assert.AreEqual(ColumnVisibility.Visible, settings.GetVisibility("C"));
            CollectionAssert.AreEqual(new[] { new ColumnSort("A", true) }, settings.GetSort());
            Assert.IsTrue(settings.IsSortSet);
            Assert.IsFalse(settings.IsFilterSet);
            Assert.IsFalse(settings.AreFoldersSet);

            // Saved in the new format, so it's not read as the settings of the first pass again
            settings.SetVisibility("C", ColumnVisibility.Unset);
            ViewSettings copy = new ViewSettings(string.Empty);
            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(settings), copy);
            Assert.AreEqual(ColumnVisibility.Unset, copy.GetVisibility("C"));
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

            // The single sort is not saved again, so it doesn't come back once the user unsets the sort
            settings.UnsetSort();
            ViewSettings copy = new ViewSettings(string.Empty);
            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(settings), copy);
            Assert.IsFalse(copy.IsSortSet);
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
