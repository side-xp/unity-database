using System;
using System.Collections.Generic;

using NUnit.Framework;

using UnityEngine;

using SideXP.Database.EditorOnly;

using Object = UnityEngine.Object;

namespace SideXP.Database.Tests
{

    /// <summary>
    /// Tests for the personal views of the tables (<see cref="PersonalViews"/>): the open view of each table, creating, duplicating,
    /// renaming and deleting views, the settings saved before the views, and the serialization.
    /// </summary>
    public class PersonalViewsTests
    {

        #region Fields

        private const string TableId = "Tests.TableId";
        private const string OtherTableId = "Tests.OtherTableId";
        private const float ResizedWidth = 150;

        #endregion


        #region Open view

        /// <summary>
        /// A table without view gets the default view, open, and keeps it.
        /// </summary>
        [Test]
        public void GetOpenView_NoView_DefaultViewCreated()
        {
            PersonalViews views = new PersonalViews();
            PersonalView view = views.GetOpenView(TableId);

            Assert.AreEqual(PersonalViews.DefaultViewName, view.Name);
            Assert.AreEqual(TableId, view.TableId);
            Assert.IsFalse(string.IsNullOrEmpty(view.Id));
            Assert.IsTrue(views.IsOpen(view));
            Assert.AreSame(view, views.GetOpenView(TableId));
            CollectionAssert.AreEqual(new[] { view }, views.GetViews(TableId));
        }

        /// <summary>
        /// A null or empty table identifier is refused.
        /// </summary>
        [Test]
        public void GetOpenView_NoTable_Throws()
        {
            Assert.Throws<ArgumentException>(() => new PersonalViews().GetOpenView(null));
            Assert.Throws<ArgumentException>(() => new PersonalViews().GetOpenView(string.Empty));
        }

        /// <summary>
        /// Each table has its own views and its own open view.
        /// </summary>
        [Test]
        public void GetOpenView_SeveralTables_OneOpenViewEach()
        {
            PersonalViews views = new PersonalViews();
            PersonalView view = views.GetOpenView(TableId);
            PersonalView otherView = views.GetOpenView(OtherTableId);

            Assert.AreNotSame(view, otherView);
            Assert.IsTrue(views.IsOpen(view));
            Assert.IsTrue(views.IsOpen(otherView));
            CollectionAssert.AreEqual(new[] { otherView }, views.GetViews(OtherTableId));
        }

        /// <summary>
        /// Opening another view of a table closes the view that was open, and opening it again changes nothing.
        /// </summary>
        [Test]
        public void SetOpenView_OtherView_Opened()
        {
            PersonalViews views = new PersonalViews();
            PersonalView defaultView = views.GetOpenView(TableId);
            PersonalView otherTableView = views.GetOpenView(OtherTableId);
            PersonalView view = views.CreateView(TableId);

            Assert.IsTrue(views.SetOpenView(view));
            Assert.IsFalse(views.SetOpenView(view));
            Assert.IsFalse(views.IsOpen(defaultView));
            Assert.AreSame(view, views.GetOpenView(TableId));
            Assert.IsTrue(views.IsOpen(otherTableView));
        }

        #endregion


        #region Create, duplicate, rename

        /// <summary>
        /// A new view has the defaults of the table, comes after the other views, isn't opened, and gets a name no other view of its
        /// table has, ignoring case.
        /// </summary>
        [Test]
        public void CreateView_Names_Unique()
        {
            PersonalViews views = new PersonalViews();
            PersonalView defaultView = views.GetOpenView(TableId);

            PersonalView view = views.CreateView(TableId);
            Assert.AreEqual(PersonalViews.NewViewName, view.Name);
            Assert.AreEqual($"{PersonalViews.NewViewName} 2", views.CreateView(TableId).Name);
            Assert.AreEqual("Balancing", views.CreateView(TableId, " Balancing ").Name);
            Assert.AreEqual("balancing 2", views.CreateView(TableId, "balancing").Name);
            Assert.AreEqual(PersonalViews.NewViewName, views.CreateView(OtherTableId, "  ").Name);

            Assert.IsFalse(view.Settings.IsColumnOrderSet);
            Assert.AreEqual(5, views.GetViews(TableId).Count);
            Assert.AreSame(defaultView, views.GetViews(TableId)[0]);
            Assert.AreSame(defaultView, views.GetOpenView(TableId));
        }

        /// <summary>
        /// A copy of a view comes right after it, with a copy of its settings, and isn't opened.
        /// </summary>
        [Test]
        public void DuplicateView_View_CopyAfterIt()
        {
            PersonalViews views = new PersonalViews();
            PersonalView view = views.GetOpenView(TableId);
            PersonalView secondView = views.CreateView(TableId, "Second");
            view.Settings.SetWidth("A", ResizedWidth);

            PersonalView copy = views.DuplicateView(view);
            Assert.AreEqual($"{PersonalViews.DefaultViewName} Copy", copy.Name);
            Assert.AreNotEqual(view.Id, copy.Id);
            CollectionAssert.AreEqual(new[] { view, copy, secondView }, views.GetViews(TableId));
            Assert.IsFalse(views.IsOpen(copy));

            Assert.AreEqual(ResizedWidth, copy.Settings.GetWidth("A"));
            copy.Settings.SetWidth("A", 0);
            Assert.AreEqual(ResizedWidth, view.Settings.GetWidth("A"));

            Assert.AreEqual($"{PersonalViews.DefaultViewName} Copy 2", views.DuplicateView(view).Name);
        }

        /// <summary>
        /// A view is renamed with a trimmed name, a name taken by another view of the table gets a number, and a blank or same name
        /// changes nothing.
        /// </summary>
        [Test]
        public void RenameView_Name_Renamed()
        {
            PersonalViews views = new PersonalViews();
            PersonalView view = views.GetOpenView(TableId);
            PersonalView otherView = views.CreateView(TableId, "Balancing");

            Assert.IsTrue(views.RenameView(view, " Prices "));
            Assert.AreEqual("Prices", view.Name);
            Assert.IsFalse(views.RenameView(view, "Prices"));
            Assert.IsFalse(views.RenameView(view, "  "));
            Assert.AreEqual("Prices", view.Name);

            Assert.IsTrue(views.RenameView(otherView, "PRICES"));
            Assert.AreEqual("PRICES 2", otherView.Name);
        }

        #endregion


        #region Delete

        /// <summary>
        /// The last view of a table can't be deleted.
        /// </summary>
        [Test]
        public void DeleteView_LastView_Refused()
        {
            PersonalViews views = new PersonalViews();
            PersonalView view = views.GetOpenView(TableId);
            views.GetOpenView(OtherTableId);

            Assert.IsFalse(views.CanDeleteView(view));
            Assert.IsFalse(views.DeleteView(view));
            CollectionAssert.AreEqual(new[] { view }, views.GetViews(TableId));
        }

        /// <summary>
        /// Deleting the open view opens the next view of the table, or the previous one if it was the last.
        /// </summary>
        [Test]
        public void DeleteView_OpenView_NextViewOpened()
        {
            PersonalViews views = new PersonalViews();
            PersonalView firstView = views.GetOpenView(TableId);
            PersonalView secondView = views.CreateView(TableId);
            PersonalView thirdView = views.CreateView(TableId);
            views.SetOpenView(secondView);

            Assert.IsTrue(views.CanDeleteView(secondView));
            Assert.IsTrue(views.DeleteView(secondView));
            Assert.AreSame(thirdView, views.GetOpenView(TableId));

            Assert.IsTrue(views.DeleteView(thirdView));
            Assert.AreSame(firstView, views.GetOpenView(TableId));
            CollectionAssert.AreEqual(new[] { firstView }, views.GetViews(TableId));
        }

        /// <summary>
        /// Deleting a view that is not open keeps the open view.
        /// </summary>
        [Test]
        public void DeleteView_OtherView_OpenViewKept()
        {
            PersonalViews views = new PersonalViews();
            PersonalView openView = views.GetOpenView(TableId);
            PersonalView view = views.CreateView(TableId);

            Assert.IsTrue(views.DeleteView(view));
            Assert.IsTrue(views.IsOpen(openView));
        }

        /// <summary>
        /// The operations on a view refuse a view that is not one of the views.
        /// </summary>
        [Test]
        public void Operations_UnknownView_Throw()
        {
            PersonalViews views = new PersonalViews();
            views.GetOpenView(TableId);
            PersonalView unknownView = new PersonalView("Unknown", new ViewSettings(TableId));

            Assert.Throws<ArgumentException>(() => views.SetOpenView(unknownView));
            Assert.Throws<ArgumentException>(() => views.DuplicateView(unknownView));
            Assert.Throws<ArgumentException>(() => views.RenameView(unknownView, "Name"));
            Assert.Throws<ArgumentException>(() => views.DeleteView(unknownView));
            Assert.Throws<ArgumentException>(() => views.SetOpenView(null));
            Assert.IsFalse(views.CanDeleteView(unknownView));
            Assert.IsFalse(views.IsOpen(unknownView));
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
            PersonalView existingView = views.GetOpenView(OtherTableId);
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

            PersonalView view = views.GetOpenView(TableId);
            Assert.AreEqual(PersonalViews.DefaultViewName, view.Name);
            Assert.AreSame(settings, view.Settings);
            Assert.AreEqual(1, views.GetViews(TableId).Count);
            CollectionAssert.AreEqual(new[] { existingView }, views.GetViews(OtherTableId));
        }

        #endregion


        #region Serialization

        /// <summary>
        /// The views, their names, ids and settings, and the open views, survive Unity's serialization, which <see cref="UserViews"/>
        /// uses to store them.
        /// </summary>
        [Test]
        public void Serialization_Views_Kept()
        {
            ViewSettingsTestSO holder = ScriptableObject.CreateInstance<ViewSettingsTestSO>();
            ViewSettingsTestSO copy = null;
            try
            {
                holder.personalViews = new PersonalViews();
                holder.personalViews.GetOpenView(TableId);
                PersonalView view = holder.personalViews.CreateView(TableId, "Balancing");
                view.Settings.SetWidth("A", ResizedWidth);
                view.Settings.SetFilter(new TextCondition("B", "sword"));
                holder.personalViews.SetOpenView(view);

                // Instantiating copies the object through its serialized data
                copy = Object.Instantiate(holder);

                List<PersonalView> copiedViews = copy.personalViews.GetViews(TableId);
                Assert.AreEqual(2, copiedViews.Count);
                PersonalView copiedView = copy.personalViews.GetOpenView(TableId);
                Assert.AreSame(copiedViews[1], copiedView);
                Assert.AreEqual(view.Id, copiedView.Id);
                Assert.AreEqual("Balancing", copiedView.Name);
                Assert.AreEqual(ResizedWidth, copiedView.Settings.GetWidth("A"));
                Assert.AreEqual("sword", ((TextCondition)copiedView.Settings.Filter).Text);
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
