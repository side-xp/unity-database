using System;
using System.Collections.Generic;

using NUnit.Framework;

using SideXP.Database.EditorOnly;

namespace SideXP.Database.Tests
{

    /// <summary>
    /// Tests for the rules of the views (<see cref="ViewCatalog"/>): the open view of each table, the layers of the personal and the
    /// project views, the operations on the views of both kinds, and the data each operation changes.
    /// </summary>
    public class ViewCatalogTests
    {

        #region Fields

        private const string TableId = "Tests.TableId";
        private const string OtherTableId = "Tests.OtherTableId";
        private const float ResizedWidth = 150;
        private const int PageSize = 50;

        /// <summary>
        /// The data of the views of the user, new for each test.
        /// </summary>
        private PersonalViews _personalViews = null;

        /// <summary>
        /// The project views, new for each test.
        /// </summary>
        private ProjectViewList _projectViews = null;

        /// <summary>
        /// The tested catalog, on <see cref="_personalViews"/> and <see cref="_projectViews"/>.
        /// </summary>
        private ViewCatalog _catalog = null;

        /// <summary>
        /// The number of changes of the data of the user notified by the catalog.
        /// </summary>
        private int _personalChangeCount = 0;

        /// <summary>
        /// The number of changes of the project views notified by the catalog.
        /// </summary>
        private int _projectChangeCount = 0;

        #endregion


        #region Lifecycle

        /// <summary>
        /// Creates empty views and the catalog before each test.
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            _personalViews = new PersonalViews();
            _projectViews = new ProjectViewList();
            _personalChangeCount = 0;
            _projectChangeCount = 0;
            _catalog = new ViewCatalog(_personalViews, _projectViews, () => _personalChangeCount++, () => _projectChangeCount++);
        }

        #endregion


        #region Open view

        /// <summary>
        /// A table without view gets the default personal view, open, and keeps it.
        /// </summary>
        [Test]
        public void GetOpenView_NoView_DefaultViewCreated()
        {
            SavedView view = _catalog.GetOpenView(TableId);

            Assert.IsInstanceOf<PersonalView>(view);
            Assert.AreEqual(ViewCatalog.DefaultViewName, view.Name);
            Assert.AreEqual(TableId, view.TableId);
            Assert.IsTrue(_catalog.IsOpen(view));
            Assert.AreSame(view, _catalog.GetOpenView(TableId));
            CollectionAssert.AreEqual(new[] { view }, _catalog.GetViews(TableId));
        }

        /// <summary>
        /// A table that only has project views opens its first project view, without creating a personal view.
        /// </summary>
        [Test]
        public void GetOpenView_ProjectViewsOnly_FirstProjectViewOpened()
        {
            ProjectView projectView = AddProjectView("Shared");
            AddProjectView("Other");

            Assert.AreSame(projectView, _catalog.GetOpenView(TableId));
            Assert.IsEmpty(_catalog.GetPersonalViews(TableId));
        }

        /// <summary>
        /// A null or empty table identifier is refused.
        /// </summary>
        [Test]
        public void GetOpenView_NoTable_Throws()
        {
            Assert.Throws<ArgumentException>(() => _catalog.GetOpenView(null));
            Assert.Throws<ArgumentException>(() => _catalog.GetOpenView(string.Empty));
        }

        /// <summary>
        /// The views are listed by kind, the personal views first.
        /// </summary>
        [Test]
        public void GetViews_BothKinds_PersonalViewsFirst()
        {
            ProjectView projectView = AddProjectView("Shared");
            Assert.AreSame(projectView, _catalog.GetOpenView(TableId));
            CollectionAssert.AreEqual(new SavedView[] { projectView }, _catalog.GetViews(TableId));

            PersonalView personalView = _catalog.CreateView(TableId);
            CollectionAssert.AreEqual(new SavedView[] { personalView, projectView }, _catalog.GetViews(TableId));
        }

        /// <summary>
        /// Opening another view of a table, of either kind, closes the view that was open, and opening it again changes nothing.
        /// </summary>
        [Test]
        public void SetOpenView_OtherKind_Opened()
        {
            SavedView defaultView = _catalog.GetOpenView(TableId);
            SavedView otherTableView = _catalog.GetOpenView(OtherTableId);
            ProjectView projectView = AddProjectView("Shared");
            _personalChangeCount = 0;

            Assert.IsTrue(_catalog.SetOpenView(projectView));
            Assert.IsFalse(_catalog.SetOpenView(projectView));
            Assert.IsFalse(_catalog.IsOpen(defaultView));
            Assert.AreSame(projectView, _catalog.GetOpenView(TableId));
            Assert.IsTrue(_catalog.IsOpen(otherTableView));
            Assert.AreEqual(1, _personalChangeCount);
            Assert.AreEqual(0, _projectChangeCount);
        }

        #endregion


        #region Layers

        /// <summary>
        /// A personal view has a single layer, its settings.
        /// </summary>
        [Test]
        public void GetLayers_PersonalView_Settings()
        {
            SavedView view = _catalog.GetOpenView(TableId);
            CollectionAssert.AreEqual(new[] { view.Settings }, _catalog.GetLayers(view));
        }

        /// <summary>
        /// A project view has the overrides of the user above its settings, and the changes made through its layers go to the
        /// overrides only.
        /// </summary>
        [Test]
        public void GetLayers_ProjectView_OverridesAboveSettings()
        {
            ProjectView view = AddProjectView("Shared");
            view.Settings.SetSort(new[] { new ColumnSort("A", false) });

            List<ViewSettings> layers = _catalog.GetLayers(view);
            Assert.AreEqual(2, layers.Count);
            Assert.AreSame(_personalViews.FindOverride(view.Id).Settings, layers[0]);
            Assert.AreSame(view.Settings, layers[1]);

            LayeredViewSettings settings = new LayeredViewSettings(layers);
            settings.SetWidth("B", ResizedWidth);
            settings.SetSort(new[] { new ColumnSort("B", true) });
            Assert.AreEqual(0, view.Settings.GetWidth("B"));
            CollectionAssert.AreEqual(new[] { new ColumnSort("A", false) }, view.Settings.GetSort());
        }

        /// <summary>
        /// The overrides are stored per setting: a change of the project view on a setting the user didn't change still applies, and
        /// the settings the user changed keep their value.
        /// </summary>
        [Test]
        public void GetLayers_ProjectViewChangedUnderOverrides_OtherSettingsApply()
        {
            ProjectView view = AddProjectView("Shared");
            view.Settings.SetWidth("A", 100);
            new LayeredViewSettings(_catalog.GetLayers(view)).SetWidth("A", ResizedWidth);

            // A change of the project file, for example after pulling from version control
            view.Settings.SetWidth("A", 120);
            view.Settings.SetVisibility("B", ColumnVisibility.Hidden);
            view.Settings.SetSort(new[] { new ColumnSort("B", true) });

            LayeredViewSettings settings = new LayeredViewSettings(_catalog.GetLayers(view));
            Assert.AreEqual(ResizedWidth, settings.GetWidth("A"));
            Assert.IsTrue(settings.IsHidden("B"));
            CollectionAssert.AreEqual(new[] { new ColumnSort("B", true) }, settings.GetSort());
        }

        /// <summary>
        /// A personal view has no overrides. A project view is not overridden until a setting of the view is set in the overrides, and
        /// is again when the user's changes give back the settings of the project view.
        /// </summary>
        [Test]
        public void GetStatus_BothKinds_OverridesFollowTheChanges()
        {
            SavedView personalView = _catalog.GetOpenView(TableId);
            ProjectView projectView = AddProjectView("Shared");
            projectView.Settings.SetWidth("A", 100);
            Assert.AreEqual(ViewStatus.Personal, _catalog.GetStatus(personalView));
            Assert.IsFalse(_catalog.HasOverrides(personalView));
            Assert.AreEqual(ViewStatus.Project, _catalog.GetStatus(projectView));

            // Opened by the table view: the overrides exist, without any setting
            LayeredViewSettings settings = new LayeredViewSettings(_catalog.GetLayers(projectView));
            Assert.AreEqual(ViewStatus.Project, _catalog.GetStatus(projectView));

            // The page size is not a setting of the views
            settings.Top.SetPageSize(PageSize);
            Assert.AreEqual(ViewStatus.Project, _catalog.GetStatus(projectView));

            settings.SetWidth("A", ResizedWidth);
            Assert.IsTrue(_catalog.HasOverrides(projectView));
            Assert.AreEqual(ViewStatus.OverriddenProject, _catalog.GetStatus(projectView));

            settings.SetWidth("A", 100);
            Assert.AreEqual(ViewStatus.Project, _catalog.GetStatus(projectView));
        }

        #endregion


        #region Create, duplicate, rename

        /// <summary>
        /// A new view is personal, has the defaults of the table, isn't opened, and gets a name no other view of its table has, of
        /// either kind, ignoring case.
        /// </summary>
        [Test]
        public void CreateView_Names_UniqueAmongBothKinds()
        {
            SavedView defaultView = _catalog.GetOpenView(TableId);
            AddProjectView("Balancing");

            PersonalView view = _catalog.CreateView(TableId);
            Assert.AreEqual(ViewCatalog.NewViewName, view.Name);
            Assert.AreEqual($"{ViewCatalog.NewViewName} 2", _catalog.CreateView(TableId).Name);
            Assert.AreEqual("balancing 2", _catalog.CreateView(TableId, " balancing ").Name);
            Assert.AreEqual(ViewCatalog.NewViewName, _catalog.CreateView(OtherTableId, "  ").Name);

            Assert.IsFalse(view.Settings.IsColumnOrderSet);
            Assert.AreSame(defaultView, _catalog.GetOpenView(TableId));
            Assert.AreEqual(0, _projectChangeCount);
        }

        /// <summary>
        /// A copy of a personal view comes right after it, with a copy of its settings, and isn't opened.
        /// </summary>
        [Test]
        public void DuplicateView_PersonalView_CopyAfterIt()
        {
            SavedView view = _catalog.GetOpenView(TableId);
            PersonalView secondView = _catalog.CreateView(TableId, "Second");
            view.Settings.SetWidth("A", ResizedWidth);

            PersonalView copy = _catalog.DuplicateView(view);
            Assert.AreEqual($"{ViewCatalog.DefaultViewName} Copy", copy.Name);
            Assert.AreNotEqual(view.Id, copy.Id);
            CollectionAssert.AreEqual(new SavedView[] { view, copy, secondView }, _catalog.GetViews(TableId));
            Assert.IsFalse(_catalog.IsOpen(copy));

            copy.Settings.SetWidth("A", 0);
            Assert.AreEqual(ResizedWidth, view.Settings.GetWidth("A"));
            Assert.AreEqual($"{ViewCatalog.DefaultViewName} Copy 2", _catalog.DuplicateView(view).Name);
        }

        /// <summary>
        /// A personal copy of a project view has the settings the user sees (the project view with the overrides), and the overrides
        /// and the project view are kept.
        /// </summary>
        [Test]
        public void DuplicateView_ProjectView_FlattenedPersonalCopy()
        {
            ProjectView view = AddProjectView("Shared");
            view.Settings.SetWidth("A", 100);
            view.Settings.SetSort(new[] { new ColumnSort("A", false) });
            view.Settings.SetFilter(new BoolCondition("C", true));
            new LayeredViewSettings(_catalog.GetLayers(view)).SetWidth("B", ResizedWidth);
            _projectChangeCount = 0;

            PersonalView copy = _catalog.DuplicateView(view);
            Assert.AreEqual("Shared Copy", copy.Name);
            CollectionAssert.AreEqual(new[] { copy }, _catalog.GetPersonalViews(TableId));
            Assert.AreEqual(100, copy.Settings.GetWidth("A"));
            Assert.AreEqual(ResizedWidth, copy.Settings.GetWidth("B"));
            CollectionAssert.AreEqual(new[] { new ColumnSort("A", false) }, copy.Settings.GetSort());
            Assert.IsInstanceOf<BoolCondition>(copy.Settings.Filter);
            Assert.AreNotSame(view.Settings.Filter, copy.Settings.Filter);

            Assert.AreEqual(ResizedWidth, _personalViews.FindOverride(view.Id).Settings.GetWidth("B"));
            CollectionAssert.AreEqual(new[] { view }, _catalog.GetProjectViews(TableId));
            Assert.AreEqual(0, _projectChangeCount);
        }

        /// <summary>
        /// A view is renamed with a trimmed name, a name taken by another view of the table gets a number, and a blank or same name
        /// changes nothing. Renaming a project view changes the project views only.
        /// </summary>
        [Test]
        public void RenameView_BothKinds_RenamedInTheirData()
        {
            SavedView view = _catalog.GetOpenView(TableId);
            ProjectView projectView = AddProjectView("Shared");
            _personalChangeCount = 0;

            Assert.IsTrue(_catalog.RenameView(view, " Prices "));
            Assert.AreEqual("Prices", view.Name);
            Assert.IsFalse(_catalog.RenameView(view, "Prices"));
            Assert.IsFalse(_catalog.RenameView(view, "  "));
            Assert.AreEqual(1, _personalChangeCount);
            Assert.AreEqual(0, _projectChangeCount);

            Assert.IsTrue(_catalog.RenameView(projectView, "PRICES"));
            Assert.AreEqual("PRICES 2", projectView.Name);
            Assert.AreEqual(1, _personalChangeCount);
            Assert.AreEqual(1, _projectChangeCount);
        }

        #endregion


        #region Delete

        /// <summary>
        /// The last view of a table can't be deleted, whatever its kind.
        /// </summary>
        [Test]
        public void DeleteView_LastView_Refused()
        {
            SavedView view = _catalog.GetOpenView(TableId);
            ProjectView projectView = AddProjectView("Shared", OtherTableId);

            Assert.IsFalse(_catalog.CanDeleteView(view));
            Assert.IsFalse(_catalog.DeleteView(view));
            Assert.IsFalse(_catalog.CanDeleteView(projectView));
            Assert.IsFalse(_catalog.DeleteView(projectView));
        }

        /// <summary>
        /// The last personal view of a table can be deleted if the table has project views.
        /// </summary>
        [Test]
        public void DeleteView_LastPersonalViewWithProjectViews_ProjectViewOpened()
        {
            SavedView view = _catalog.GetOpenView(TableId);
            ProjectView projectView = AddProjectView("Shared");

            Assert.IsTrue(_catalog.DeleteView(view));
            Assert.AreSame(projectView, _catalog.GetOpenView(TableId));
            Assert.IsEmpty(_catalog.GetPersonalViews(TableId));
        }

        /// <summary>
        /// Deleting the open view opens the next view of the table, of either kind, or the previous one if it was the last.
        /// </summary>
        [Test]
        public void DeleteView_OpenView_NextViewOpened()
        {
            SavedView firstView = _catalog.GetOpenView(TableId);
            PersonalView secondView = _catalog.CreateView(TableId);
            ProjectView projectView = AddProjectView("Shared");
            _catalog.SetOpenView(secondView);

            Assert.IsTrue(_catalog.DeleteView(secondView));
            Assert.AreSame(projectView, _catalog.GetOpenView(TableId));

            Assert.IsTrue(_catalog.DeleteView(projectView));
            Assert.AreSame(firstView, _catalog.GetOpenView(TableId));
            CollectionAssert.AreEqual(new[] { firstView }, _catalog.GetViews(TableId));
        }

        /// <summary>
        /// Deleting a project view changes the project views, and removes the overrides of the user.
        /// </summary>
        [Test]
        public void DeleteView_ProjectView_OverridesRemoved()
        {
            SavedView openView = _catalog.GetOpenView(TableId);
            ProjectView projectView = AddProjectView("Shared");
            _catalog.GetLayers(projectView);
            _projectChangeCount = 0;

            Assert.IsTrue(_catalog.DeleteView(projectView));
            Assert.IsNull(_personalViews.FindOverride(projectView.Id));
            Assert.IsEmpty(_catalog.GetProjectViews(TableId));
            Assert.IsTrue(_catalog.IsOpen(openView));
            Assert.AreEqual(1, _projectChangeCount);
        }

        #endregion


        #region Share

        /// <summary>
        /// Sharing a personal view moves it to the project views, with the same id, name and settings, and keeps it open. The page
        /// size stays a setting of the user, in the overrides.
        /// </summary>
        [Test]
        public void ShareView_PersonalView_MovedToProjectViews()
        {
            _catalog.GetOpenView(TableId);
            PersonalView view = _catalog.CreateView(TableId, "Balancing");
            view.Settings.SetSort(new[] { new ColumnSort("A", true) });
            view.Settings.SetPageSize(PageSize);
            _catalog.SetOpenView(view);
            _personalChangeCount = 0;

            ProjectView projectView = _catalog.ShareView(view);
            Assert.AreEqual(view.Id, projectView.Id);
            Assert.AreEqual("Balancing", projectView.Name);
            Assert.AreSame(view.Settings, projectView.Settings);
            CollectionAssert.AreEqual(new[] { projectView }, _catalog.GetProjectViews(TableId));
            Assert.AreEqual(1, _catalog.GetPersonalViews(TableId).Count);
            Assert.AreSame(projectView, _catalog.GetOpenView(TableId));

            Assert.AreEqual(0, projectView.Settings.PageSize);
            Assert.AreEqual(PageSize, _personalViews.FindOverride(projectView.Id).Settings.PageSize);
            CollectionAssert.AreEqual(new[] { new ColumnSort("A", true) },
                new LayeredViewSettings(_catalog.GetLayers(projectView)).GetSort());
            Assert.AreEqual(1, _personalChangeCount);
            Assert.AreEqual(1, _projectChangeCount);
        }

        /// <summary>
        /// The last personal view of a table can be shared: the table then only has a project view.
        /// </summary>
        [Test]
        public void ShareView_LastPersonalView_TableKeepsAView()
        {
            PersonalView view = (PersonalView)_catalog.GetOpenView(TableId);

            ProjectView projectView = _catalog.ShareView(view);
            Assert.IsEmpty(_catalog.GetPersonalViews(TableId));
            Assert.AreSame(projectView, _catalog.GetOpenView(TableId));
        }

        #endregion


        #region Checks

        /// <summary>
        /// The operations on a view refuse a view that is not one of the views of the catalog.
        /// </summary>
        [Test]
        public void Operations_UnknownView_Throw()
        {
            _catalog.GetOpenView(TableId);
            PersonalView unknownView = new PersonalView("Unknown", new ViewSettings(TableId));
            ProjectView unknownProjectView = new ProjectView("Unknown", new ViewSettings(TableId));

            Assert.Throws<ArgumentException>(() => _catalog.SetOpenView(unknownView));
            Assert.Throws<ArgumentException>(() => _catalog.GetLayers(unknownProjectView));
            Assert.Throws<ArgumentException>(() => _catalog.DuplicateView(unknownView));
            Assert.Throws<ArgumentException>(() => _catalog.RenameView(unknownProjectView, "Name"));
            Assert.Throws<ArgumentException>(() => _catalog.DeleteView(unknownView));
            Assert.Throws<ArgumentException>(() => _catalog.ShareView(unknownView));
            Assert.Throws<ArgumentException>(() => _catalog.SetOpenView(null));
            Assert.IsFalse(_catalog.CanDeleteView(unknownView));
            Assert.IsFalse(_catalog.IsOpen(unknownView));
        }

        #endregion


        #region Private API

        /// <summary>
        /// Adds a project view directly to the project views, like a view shared by another user.
        /// </summary>
        /// <param name="name">The name of the view.</param>
        /// <param name="tableId">The stable identifier of the table of the view, or null for <see cref="TableId"/>.</param>
        /// <returns>Returns the added view.</returns>
        private ProjectView AddProjectView(string name, string tableId = null)
        {
            return _projectViews.Add(new ProjectView(name, new ViewSettings(tableId ?? TableId)));
        }

        #endregion

    }

}
