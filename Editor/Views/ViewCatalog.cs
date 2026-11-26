using System;
using System.Collections.Generic;

using UnityEngine;

namespace SideXP.Database.EditorOnly
{

    /// <summary>
    /// The views of the tables, personal (see <see cref="PersonalViews"/>) and project views (see <see cref="ProjectViewList"/>), with
    /// the rules that concern both: the open view of each table, the layers of a view, and the operations on the views.
    /// </summary>
    /// <remarks>
    /// Rules:
    /// <list type="bullet">
    /// <item>A table that has been opened has at least one view, of either kind: <see cref="GetOpenView"/> creates the
    /// <see cref="DefaultViewName"/> personal view of a table that has none, and the last view of a table can't be deleted.</item>
    /// <item>The names of the views are unique per table, ignoring case, among the views of both kinds: a taken name gets a number.
    /// The project views of other users can still bring a name twice, after a merge.</item>
    /// <item>A personal view has a single layer: its settings. A project view has two: the overrides of the user (where the changes
    /// go), above the settings of the project view (see <see cref="GetLayers"/>).</item>
    /// <item>The views are listed by kind: the personal views first, then the project views.</item>
    /// </list>
    /// <para>
    /// The catalog doesn't hold any data: create one when needed (see <see cref="Current"/>). Each operation notifies the data it
    /// changed, so its owner saves the files that changed only: the project file is never written by the changes of the settings of a
    /// view, which are always written to the files of the user.
    /// </para>
    /// </remarks>
    internal class ViewCatalog
    {

        #region Fields

        /// <summary>
        /// The name of the view created for a table that has none, and of the views of the settings saved before the views.
        /// </summary>
        public const string DefaultViewName = "Default";

        /// <summary>
        /// The name of a new view, before the user renames it.
        /// </summary>
        public const string NewViewName = "New View";

        /// <summary>
        /// Added to the name of a view to name its copy.
        /// </summary>
        private const string CopySuffix = " Copy";

        /// <summary>
        /// The data of the views of the user.
        /// </summary>
        private readonly PersonalViews _personalViews = null;

        /// <summary>
        /// The views shared with the team.
        /// </summary>
        private readonly ProjectViewList _projectViews = null;

        /// <summary>
        /// Called after an operation changed the data of the user, so it can be saved.
        /// </summary>
        private readonly Action _personalViewsChanged = null;

        /// <summary>
        /// Called after an operation changed the project views, so they can be saved.
        /// </summary>
        private readonly Action _projectViewsChanged = null;

        #endregion


        #region Lifecycle

        /// <inheritdoc cref="ViewCatalog"/>
        /// <param name="personalViews"><inheritdoc cref="_personalViews" path="/summary"/></param>
        /// <param name="projectViews"><inheritdoc cref="_projectViews" path="/summary"/></param>
        /// <param name="personalViewsChanged"><inheritdoc cref="_personalViewsChanged" path="/summary"/></param>
        /// <param name="projectViewsChanged"><inheritdoc cref="_projectViewsChanged" path="/summary"/></param>
        /// <exception cref="ArgumentNullException">Thrown if the personal or the project views are null.</exception>
        public ViewCatalog(PersonalViews personalViews, ProjectViewList projectViews, Action personalViewsChanged = null,
            Action projectViewsChanged = null)
        {
            _personalViews = personalViews ?? throw new ArgumentNullException(nameof(personalViews));
            _projectViews = projectViews ?? throw new ArgumentNullException(nameof(projectViews));
            _personalViewsChanged = personalViewsChanged;
            _projectViewsChanged = projectViewsChanged;
        }

        /// <summary>
        /// Gets the catalog of the views of the files of the project (see <see cref="UserViews"/> and <see cref="ProjectViews"/>),
        /// which schedules the save of each file its operations change.
        /// </summary>
        public static ViewCatalog Current => new ViewCatalog(UserViews.instance.PersonalViews, ProjectViews.instance.Views,
            UserViews.instance.ScheduleSave, ProjectViews.instance.ScheduleSave);

        #endregion


        #region Views

        /// <summary>
        /// Gets the views of a table, of both kinds.
        /// </summary>
        /// <param name="tableId">The stable identifier of the table (see <see cref="TableDefinition.Id"/>).</param>
        /// <returns>Returns the personal views of the table, then its project views, or an empty list if it has none.</returns>
        public List<SavedView> GetViews(string tableId)
        {
            List<SavedView> views = new List<SavedView>(GetPersonalViews(tableId));
            views.AddRange(GetProjectViews(tableId));
            return views;
        }

        /// <inheritdoc cref="PersonalViews.GetViews"/>
        public List<PersonalView> GetPersonalViews(string tableId)
        {
            return _personalViews.GetViews(tableId);
        }

        /// <inheritdoc cref="ProjectViewList.GetViews"/>
        public List<ProjectView> GetProjectViews(string tableId)
        {
            return _projectViews.GetViews(tableId);
        }

        /// <summary>
        /// Gets the layers of the settings of a view, from the top: the settings of a personal view, or the overrides of the user then
        /// the settings of a project view (the overrides are created, without any setting, if the user has none yet).
        /// </summary>
        /// <param name="view">The view of which to get the layers.</param>
        /// <returns>Returns the layers of the view, the first one receiving the changes.</returns>
        /// <exception cref="ArgumentException">Thrown if the view is not one of the views of the catalog.</exception>
        public List<ViewSettings> GetLayers(SavedView view)
        {
            CheckContains(view);
            if (view is ProjectView projectView)
                return new List<ViewSettings> { _personalViews.GetOrCreateOverride(projectView), projectView.Settings };

            return new List<ViewSettings> { view.Settings };
        }

        /// <summary>
        /// Checks if the user changed a project view: their overrides of it set at least one setting of the view.
        /// </summary>
        /// <param name="view">The view to check.</param>
        /// <returns>Returns true if the view is a project view with overrides, or false for a personal view.</returns>
        public bool HasOverrides(SavedView view)
        {
            if (view is not ProjectView)
                return false;

            ViewOverride viewOverride = _personalViews.FindOverride(view.Id);
            return viewOverride != null && !viewOverride.Settings.IsEmpty;
        }

        /// <summary>
        /// Gets the overrides of the user on a project view: the settings they changed in it.
        /// </summary>
        /// <param name="view">The view of which to get the overrides.</param>
        /// <returns>Returns the layer of the overrides, or null for a personal view or a project view the user hasn't opened yet. Read
        /// it only: change the overrides through the operations of the catalog, or through the layers of the view.</returns>
        public ViewSettings GetOverrides(SavedView view)
        {
            return view is ProjectView ? _personalViews.FindOverride(view.Id)?.Settings : null;
        }

        /// <summary>
        /// Gets the kind of a view, and whether the user changed it.
        /// </summary>
        /// <param name="view">The view of which to get the status.</param>
        /// <returns>Returns the status of the view.</returns>
        public ViewStatus GetStatus(SavedView view)
        {
            if (view == null || !view.IsProjectView)
                return ViewStatus.Personal;

            return HasOverrides(view) ? ViewStatus.OverriddenProject : ViewStatus.Project;
        }

        #endregion


        #region Open view

        /// <summary>
        /// Gets the view open for a table: the view the user used last. Creates the <see cref="DefaultViewName"/> personal view if the
        /// table has no view of either kind, and opens the first view of the table if none is open.
        /// </summary>
        /// <param name="tableId">The stable identifier of the table (see <see cref="TableDefinition.Id"/>).</param>
        /// <returns>Returns the open view of the table.</returns>
        /// <exception cref="ArgumentException">Thrown if the identifier is null or empty.</exception>
        public SavedView GetOpenView(string tableId)
        {
            if (string.IsNullOrEmpty(tableId))
                throw new ArgumentException("The identifier of the table can't be null or empty.", nameof(tableId));

            List<SavedView> views = GetViews(tableId);
            SavedView openView = views.Find(view => _personalViews.IsOpen(view.Id));
            if (openView != null)
                return openView;

            openView = views.Count > 0 ? views[0] : _personalViews.Add(new PersonalView(DefaultViewName, new ViewSettings(tableId)));
            SetOpenView(openView);
            return openView;
        }

        /// <summary>
        /// Checks if a view is the open view of its table.
        /// </summary>
        /// <param name="view">The view to check.</param>
        /// <returns>Returns true if the view is open.</returns>
        public bool IsOpen(SavedView view)
        {
            return view != null && _personalViews.IsOpen(view.Id);
        }

        /// <summary>
        /// Opens a view for its table, instead of the view that was open.
        /// </summary>
        /// <param name="view">The view to open.</param>
        /// <returns>Returns true if the open view has changed.</returns>
        /// <exception cref="ArgumentException">Thrown if the view is not one of the views of the catalog.</exception>
        public bool SetOpenView(SavedView view)
        {
            CheckContains(view);
            List<string> tableViewIds = GetViews(view.TableId).ConvertAll(tableView => tableView.Id);
            if (!_personalViews.SetOpen(view.Id, tableViewIds))
                return false;

            _personalViewsChanged?.Invoke();
            return true;
        }

        #endregion


        #region Operations

        /// <summary>
        /// Creates a personal view with the defaults of a table, after its other personal views. The view is not opened.
        /// </summary>
        /// <param name="tableId">The stable identifier of the table (see <see cref="TableDefinition.Id"/>).</param>
        /// <param name="name">The name of the view, or null or blank for <see cref="NewViewName"/>. A taken name gets a number.</param>
        /// <returns>Returns the created view.</returns>
        /// <exception cref="ArgumentException">Thrown if the identifier is null or empty.</exception>
        public PersonalView CreateView(string tableId, string name = null)
        {
            if (string.IsNullOrEmpty(tableId))
                throw new ArgumentException("The identifier of the table can't be null or empty.", nameof(tableId));

            name = string.IsNullOrWhiteSpace(name) ? NewViewName : name.Trim();
            PersonalView view = _personalViews.Add(new PersonalView(GetUniqueName(tableId, name), new ViewSettings(tableId)));
            _personalViewsChanged?.Invoke();
            return view;
        }

        /// <summary>
        /// Creates a personal copy of a view. The copy is not opened.
        /// </summary>
        /// <remarks>
        /// The copy of a personal view comes right after it, with a copy of its settings. The copy of a project view comes after the
        /// personal views of its table, with the settings the user sees: the settings of the project view with the overrides of the
        /// user (see <see cref="LayeredViewSettings.Flatten"/>). The overrides of the project view are kept.
        /// </remarks>
        /// <param name="view">The view to copy.</param>
        /// <returns>Returns the created copy.</returns>
        /// <exception cref="ArgumentException">Thrown if the view is not one of the views of the catalog.</exception>
        public PersonalView DuplicateView(SavedView view)
        {
            CheckContains(view);
            string name = GetUniqueName(view.TableId, $"{view.Name}{CopySuffix}");

            PersonalView copy;
            if (view is PersonalView personalView)
            {
                copy = _personalViews.Add(new PersonalView(name, view.Settings.Clone()), personalView);
            }
            else
            {
                ViewSettings settings = new LayeredViewSettings(GetLayers(view)).Flatten();
                copy = _personalViews.Add(new PersonalView(name, settings));
            }

            _personalViewsChanged?.Invoke();
            return copy;
        }

        /// <summary>
        /// Renames a view. Renaming a project view changes the project views.
        /// </summary>
        /// <param name="view">The view to rename.</param>
        /// <param name="name">The new name of the view, trimmed. A name taken by another view of the table gets a number.</param>
        /// <returns>Returns true if the name has changed, or false if it's the same or blank.</returns>
        /// <exception cref="ArgumentException">Thrown if the view is not one of the views of the catalog.</exception>
        public bool RenameView(SavedView view, string name)
        {
            CheckContains(view);
            if (string.IsNullOrWhiteSpace(name))
                return false;

            name = GetUniqueName(view.TableId, name.Trim(), view);
            if (name == view.Name)
                return false;

            view.Name = name;
            NotifyChanged(view);
            return true;
        }

        /// <summary>
        /// Checks if a view can be deleted: it's not the last view of its table, of either kind.
        /// </summary>
        /// <param name="view">The view to check.</param>
        /// <returns>Returns true if the view can be deleted.</returns>
        public bool CanDeleteView(SavedView view)
        {
            return Contains(view) && GetViews(view.TableId).Count > 1;
        }

        /// <summary>
        /// Deletes a view. Deleting a project view changes the project views, and removes the overrides of the user. If the view is the
        /// open view of its table, the next view of the table is opened instead (or the previous one if it was the last).
        /// </summary>
        /// <param name="view">The view to delete.</param>
        /// <returns>Returns true if the view has been deleted, or false if it's the last view of its table.</returns>
        /// <exception cref="ArgumentException">Thrown if the view is not one of the views of the catalog.</exception>
        public bool DeleteView(SavedView view)
        {
            CheckContains(view);
            if (!CanDeleteView(view))
                return false;

            List<SavedView> tableViews = GetViews(view.TableId);
            int index = tableViews.IndexOf(view);
            bool wasOpen = IsOpen(view);

            if (view is ProjectView projectView)
            {
                _projectViews.Remove(projectView);
                _personalViews.RemoveOverride(view.Id);
            }
            else
            {
                _personalViews.Remove((PersonalView)view);
            }

            _personalViews.RemoveOpen(view.Id);
            if (wasOpen)
            {
                tableViews.RemoveAt(index);
                SetOpenView(tableViews[Mathf.Min(index, tableViews.Count - 1)]);
            }

            NotifyChanged(view);
            _personalViewsChanged?.Invoke();
            return true;
        }

        /// <summary>
        /// Shares a personal view as a project view: the view leaves the personal views and becomes a project view with the same id,
        /// name and settings, so it stays open if it was.
        /// </summary>
        /// <param name="view">The personal view to share.</param>
        /// <returns>Returns the created project view.</returns>
        /// <exception cref="ArgumentException">Thrown if the view is not one of the personal views of the catalog.</exception>
        public ProjectView ShareView(PersonalView view)
        {
            CheckContains(view);
            _personalViews.Remove(view);
            ProjectView projectView = _projectViews.Add(new ProjectView(view.Id, view.Name, view.Settings));

            _personalViewsChanged?.Invoke();
            _projectViewsChanged?.Invoke();
            return projectView;
        }

        /// <summary>
        /// Writes the overrides of the user to their project view, so the team gets them (<i>Save for everyone</i>), then removes them.
        /// Changes the project views.
        /// </summary>
        /// <remarks>
        /// Only the settings the user changed are written: the other settings of the project view are kept.
        /// </remarks>
        /// <param name="view">The project view.</param>
        /// <returns>Returns true if the view had overrides to save.</returns>
        /// <exception cref="ArgumentException">Thrown if the view is not one of the project views of the catalog.</exception>
        public bool SaveOverrides(ProjectView view)
        {
            CheckContains(view);
            ViewSettings overrides = GetOverrides(view);
            if (overrides == null || overrides.IsEmpty)
                return false;

            overrides.ApplyTo(view.Settings);
            overrides.UnsetAll();
            _personalViewsChanged?.Invoke();
            _projectViewsChanged?.Invoke();
            return true;
        }

        /// <summary>
        /// Discards the overrides of the user on a project view, so they see it as saved in the project again.
        /// </summary>
        /// <param name="view">The project view.</param>
        /// <returns>Returns true if the view had overrides to discard.</returns>
        /// <exception cref="ArgumentException">Thrown if the view is not one of the project views of the catalog.</exception>
        public bool ResetOverrides(ProjectView view)
        {
            CheckContains(view);
            ViewSettings overrides = GetOverrides(view);
            if (overrides == null || !overrides.UnsetAll())
                return false;

            _personalViewsChanged?.Invoke();
            return true;
        }

        /// <summary>
        /// Discards one kind of override of the user on a project view, for example the widths of all the columns, so it comes from the
        /// project view again.
        /// </summary>
        /// <param name="view">The project view.</param>
        /// <param name="kind">The kind of setting to discard.</param>
        /// <returns>Returns true if the view had overrides of that kind.</returns>
        /// <exception cref="ArgumentException">Thrown if the view is not one of the project views of the catalog.</exception>
        public bool ResetOverride(ProjectView view, ViewSettingKind kind)
        {
            CheckContains(view);
            ViewSettings overrides = GetOverrides(view);
            if (overrides == null || !overrides.Unset(kind))
                return false;

            _personalViewsChanged?.Invoke();
            return true;
        }

        /// <summary>
        /// Removes the overrides of the user on the project views of a table that don't exist anymore, for example after a teammate
        /// deleted one and the project file was pulled. Call it when the table opens.
        /// </summary>
        /// <remarks>
        /// The overrides are removed silently: a deleted project view is not kept as a personal view (chosen by the user), since its
        /// name and its settings are gone with the project file, and the team knows why it was deleted.
        /// </remarks>
        /// <param name="tableId">The stable identifier of the table (see <see cref="TableDefinition.Id"/>).</param>
        /// <returns>Returns the number of removed overrides.</returns>
        public int RemoveDeletedOverrides(string tableId)
        {
            HashSet<string> viewIds = new HashSet<string>(GetProjectViews(tableId).ConvertAll(view => view.Id));
            int count = 0;
            foreach (ViewOverride viewOverride in _personalViews.GetOverrides(tableId))
            {
                if (viewIds.Contains(viewOverride.ViewId))
                    continue;

                _personalViews.RemoveOverride(viewOverride.ViewId);
                _personalViews.RemoveOpen(viewOverride.ViewId);
                count++;
            }

            if (count > 0)
                _personalViewsChanged?.Invoke();

            return count;
        }

        /// <summary>
        /// Gets a name that no other view of a table has, of either kind, ignoring case.
        /// </summary>
        /// <param name="tableId">The stable identifier of the table.</param>
        /// <param name="name">The wanted name.</param>
        /// <param name="ignoredView">A view whose name doesn't count, for example the renamed view, or null.</param>
        /// <returns>Returns the wanted name if it's free, or the name followed by the first free number from 2 otherwise.</returns>
        /// <example>
        /// With the views <c>Default</c> and <c>New View</c>, <c>GetUniqueName(tableId, "new view")</c> returns <c>new view 2</c>.
        /// </example>
        public string GetUniqueName(string tableId, string name, SavedView ignoredView = null)
        {
            List<SavedView> views = GetViews(tableId);
            views.Remove(ignoredView);
            if (!IsNameTaken(views, name))
                return name;

            for (int number = 2; ; number++)
            {
                string candidate = $"{name} {number}";
                if (!IsNameTaken(views, candidate))
                    return candidate;
            }
        }

        #endregion


        #region Private API

        /// <summary>
        /// Checks if a view is one of the views of the catalog.
        /// </summary>
        /// <param name="view">The view to check.</param>
        /// <returns>Returns true if the view is a personal or a project view of the catalog, and can be used.</returns>
        private bool Contains(SavedView view)
        {
            return view switch
            {
                PersonalView personalView => _personalViews.Contains(personalView),
                ProjectView projectView => _projectViews.Contains(projectView),
                _ => false,
            };
        }

        /// <summary>
        /// Throws if a view is not one of the views of the catalog.
        /// </summary>
        /// <param name="view">The view to check.</param>
        /// <exception cref="ArgumentException">Thrown if the view is null or not one of the views of the catalog.</exception>
        private void CheckContains(SavedView view)
        {
            if (!Contains(view))
                throw new ArgumentException("The view is not one of the views of the catalog.", nameof(view));
        }

        /// <summary>
        /// Notifies the change of the data that holds a view: the data of the user for a personal view, the project views for a
        /// project view.
        /// </summary>
        /// <param name="view">The changed view.</param>
        private void NotifyChanged(SavedView view)
        {
            if (view.IsProjectView)
                _projectViewsChanged?.Invoke();
            else
                _personalViewsChanged?.Invoke();
        }

        /// <summary>
        /// Checks if a view of a list has a name, ignoring case.
        /// </summary>
        /// <param name="views">The views to check.</param>
        /// <param name="name">The name to find.</param>
        /// <returns>Returns true if a view has the name.</returns>
        private static bool IsNameTaken(List<SavedView> views, string name)
        {
            return views.Exists(view => string.Equals(view.Name, name, StringComparison.OrdinalIgnoreCase));
        }

        #endregion

    }

}
