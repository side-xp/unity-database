using System;
using System.Collections.Generic;

using NUnit.Framework;

using UnityEngine;

using SideXP.Database.EditorOnly;

using Object = UnityEngine.Object;

namespace SideXP.Database.Tests
{

    /// <summary>
    /// Tests for the data of the views of the user (<see cref="PersonalViews"/>): the personal views, the open views, the overrides of
    /// the project views, the settings saved before the views, and the serialization. The rules of the views are tested by
    /// <see cref="ViewCatalogTests"/>.
    /// </summary>
    public class PersonalViewsTests
    {

        #region Fields

        private const string TableId = "Tests.TableId";
        private const string OtherTableId = "Tests.OtherTableId";
        private const float ResizedWidth = 150;

        #endregion


        #region Views

        /// <summary>
        /// Views are added at the end, or right after another view, and listed per table.
        /// </summary>
        [Test]
        public void Add_Views_ListedPerTable()
        {
            PersonalViews views = new PersonalViews();
            PersonalView first = views.Add(new PersonalView("First", new ViewSettings(TableId)));
            PersonalView other = views.Add(new PersonalView("Other", new ViewSettings(OtherTableId)));
            PersonalView last = views.Add(new PersonalView("Last", new ViewSettings(TableId)));
            PersonalView second = views.Add(new PersonalView("Second", new ViewSettings(TableId)), first);

            CollectionAssert.AreEqual(new[] { first, second, last }, views.GetViews(TableId));
            CollectionAssert.AreEqual(new[] { other }, views.GetViews(OtherTableId));
            Assert.IsTrue(views.Contains(second));
        }

        /// <summary>
        /// A view without table, or already added, is refused.
        /// </summary>
        [Test]
        public void Add_InvalidView_Throws()
        {
            PersonalViews views = new PersonalViews();
            PersonalView view = views.Add(new PersonalView("View", new ViewSettings(TableId)));

            Assert.Throws<ArgumentException>(() => views.Add(view));
            Assert.Throws<ArgumentException>(() => views.Add(new PersonalView("View", new ViewSettings(string.Empty))));
            Assert.Throws<ArgumentException>(() => views.Add(null));
        }

        /// <summary>
        /// A removed view is no longer listed.
        /// </summary>
        [Test]
        public void Remove_View_NotListed()
        {
            PersonalViews views = new PersonalViews();
            PersonalView view = views.Add(new PersonalView("View", new ViewSettings(TableId)));

            Assert.IsTrue(views.Remove(view));
            Assert.IsFalse(views.Remove(view));
            Assert.IsEmpty(views.GetViews(TableId));
            Assert.IsFalse(views.Contains(view));
        }

        #endregion


        #region Open views

        /// <summary>
        /// Opening a view closes the other given views of its table, and opening it again changes nothing.
        /// </summary>
        [Test]
        public void SetOpen_View_OthersClosed()
        {
            PersonalViews views = new PersonalViews();
            views.SetOpen("A", Array.Empty<string>());
            views.SetOpen("Other", Array.Empty<string>());

            Assert.IsTrue(views.SetOpen("B", new[] { "A", "B" }));
            Assert.IsFalse(views.SetOpen("B", new[] { "A", "B" }));
            Assert.IsFalse(views.IsOpen("A"));
            Assert.IsTrue(views.IsOpen("B"));
            Assert.IsTrue(views.IsOpen("Other"));

            Assert.IsTrue(views.RemoveOpen("B"));
            Assert.IsFalse(views.IsOpen("B"));
            Assert.IsFalse(views.IsOpen(null));
        }

        #endregion


        #region Overrides

        /// <summary>
        /// The overrides of a project view are created without any setting, for the table of the view, and found again.
        /// </summary>
        [Test]
        public void GetOrCreateOverride_ProjectView_CreatedOnce()
        {
            PersonalViews views = new PersonalViews();
            ProjectView projectView = new ProjectView("Shared", new ViewSettings(TableId));
            Assert.IsNull(views.FindOverride(projectView.Id));

            ViewSettings viewOverride = views.GetOrCreateOverride(projectView);
            Assert.AreEqual(TableId, viewOverride.TableId);
            Assert.IsFalse(viewOverride.IsColumnOrderSet);
            Assert.IsFalse(viewOverride.IsSortSet);
            Assert.AreSame(viewOverride, views.GetOrCreateOverride(projectView));
            Assert.AreSame(viewOverride, views.FindOverride(projectView.Id).Settings);

            Assert.IsTrue(views.RemoveOverride(projectView.Id));
            Assert.IsFalse(views.RemoveOverride(projectView.Id));
            Assert.IsNull(views.FindOverride(projectView.Id));
        }

        /// <summary>
        /// A project view without table is refused.
        /// </summary>
        [Test]
        public void GetOrCreateOverride_InvalidView_Throws()
        {
            PersonalViews views = new PersonalViews();

            Assert.Throws<ArgumentException>(() => views.GetOrCreateOverride(null));
            Assert.Throws<ArgumentException>(() => views.GetOrCreateOverride(new ProjectView("View", new ViewSettings(null))));
        }

        #endregion


        #region Settings of the first pass

        /// <summary>
        /// The settings saved before the views become the default view of their table, open, as they are. The settings without table,
        /// and the settings of a table that already has views, are skipped.
        /// </summary>
        [Test]
        public void ImportDefaultViews_SettingsOfFirstPass_DefaultViewsOpen()
        {
            PersonalViews views = new PersonalViews();
            PersonalView existingView = views.Add(new PersonalView("Existing", new ViewSettings(OtherTableId)));
            ViewSettings settings = new ViewSettings(TableId);
            List<ViewSettings> defaultViews = new List<ViewSettings>
            {
                settings,
                null,
                new ViewSettings(string.Empty),
                new ViewSettings(TableId),
                new ViewSettings(OtherTableId),
            };

            Assert.AreEqual(1, views.ImportDefaultViews(defaultViews));

            List<PersonalView> tableViews = views.GetViews(TableId);
            Assert.AreEqual(1, tableViews.Count);
            Assert.AreEqual(ViewCatalog.DefaultViewName, tableViews[0].Name);
            Assert.AreSame(settings, tableViews[0].Settings);
            Assert.IsTrue(views.IsOpen(tableViews[0].Id));
            CollectionAssert.AreEqual(new[] { existingView }, views.GetViews(OtherTableId));
        }

        #endregion


        #region Serialization

        /// <summary>
        /// The views, their names, ids and settings, the open views and the overrides survive Unity's serialization, which
        /// <see cref="UserViews"/> uses to store them.
        /// </summary>
        [Test]
        public void Serialization_ViewsAndOverrides_Kept()
        {
            ViewSettingsTestSO holder = ScriptableObject.CreateInstance<ViewSettingsTestSO>();
            ViewSettingsTestSO copy = null;
            try
            {
                holder.personalViews = new PersonalViews();
                holder.personalViews.Add(new PersonalView("Default", new ViewSettings(TableId)));
                PersonalView view = holder.personalViews.Add(new PersonalView("Balancing", new ViewSettings(TableId)));
                view.Settings.SetWidth("A", ResizedWidth);
                view.Settings.SetFilter(new TextCondition("B", "sword"));
                holder.personalViews.SetOpen(view.Id, Array.Empty<string>());
                ProjectView projectView = new ProjectView("Shared", new ViewSettings(TableId));
                holder.personalViews.GetOrCreateOverride(projectView).SetSort(new[] { new ColumnSort("A", true) });

                // Instantiating copies the object through its serialized data
                copy = Object.Instantiate(holder);

                List<PersonalView> copiedViews = copy.personalViews.GetViews(TableId);
                Assert.AreEqual(2, copiedViews.Count);
                PersonalView copiedView = copiedViews[1];
                Assert.AreEqual(view.Id, copiedView.Id);
                Assert.AreEqual("Balancing", copiedView.Name);
                Assert.IsTrue(copy.personalViews.IsOpen(copiedView.Id));
                Assert.AreEqual(ResizedWidth, copiedView.Settings.GetWidth("A"));
                Assert.AreEqual("sword", ((TextCondition)copiedView.Settings.Filter).Text);

                ViewOverride copiedOverride = copy.personalViews.FindOverride(projectView.Id);
                Assert.IsNotNull(copiedOverride);
                CollectionAssert.AreEqual(new[] { new ColumnSort("A", true) }, copiedOverride.Settings.GetSort());
            }
            finally
            {
                Object.DestroyImmediate(holder);
                if (copy != null)
                    Object.DestroyImmediate(copy);
            }
        }

        /// <summary>
        /// The project views, their names, ids and settings, survive Unity's serialization, which <see cref="ProjectViews"/> uses to
        /// store them.
        /// </summary>
        [Test]
        public void Serialization_ProjectViews_Kept()
        {
            ViewSettingsTestSO holder = ScriptableObject.CreateInstance<ViewSettingsTestSO>();
            ViewSettingsTestSO copy = null;
            try
            {
                holder.projectViews = new ProjectViewList();
                ProjectView view = holder.projectViews.Add(new ProjectView("Shared", new ViewSettings(TableId)));
                view.Settings.SetFolders(new[] { "Assets/Items" });

                copy = Object.Instantiate(holder);

                List<ProjectView> copiedViews = copy.projectViews.GetViews(TableId);
                Assert.AreEqual(1, copiedViews.Count);
                Assert.AreEqual(view.Id, copiedViews[0].Id);
                Assert.AreEqual("Shared", copiedViews[0].Name);
                Assert.IsTrue(copiedViews[0].IsProjectView);
                CollectionAssert.AreEqual(new[] { "Assets/Items" }, copiedViews[0].Settings.Folders);
            }
            finally
            {
                Object.DestroyImmediate(holder);
                if (copy != null)
                    Object.DestroyImmediate(copy);
            }
        }

        #endregion

    }

}
