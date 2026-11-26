using System;
using System.Collections.Generic;

using UnityEngine;

namespace SideXP.Database.EditorOnly
{

    /// <summary>
    /// The personal views of the tables (see <see cref="PersonalView"/>), and the view open for each table. Stored by
    /// <see cref="UserViews"/>.
    /// </summary>
    /// <remarks>
    /// A table that has been opened has at least one view: <see cref="GetOpenView"/> creates the <see cref="DefaultViewName"/> view of
    /// a table that has none, and the last view of a table can't be deleted. The names of the views are unique per table, ignoring
    /// case: a taken name gets a number.
    /// <para>
    /// The changes are made in memory: the owner saves after each change (see <see cref="UserViews.ScheduleSave"/>).
    /// </para>
    /// </remarks>
    [Serializable]
    internal class PersonalViews
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
        /// The views of all the tables, in the order they are listed. Each table's views keep their relative order.
        /// </summary>
        /// <remarks>
        /// A list, since Unity can't serialize dictionaries. The number of views of a project is small enough for a linear search.
        /// </remarks>
        [SerializeField]
        private List<PersonalView> _views = new List<PersonalView>();

        /// <summary>
        /// The identifiers of the open views, one per table at most.
        /// </summary>
        [SerializeField]
        private List<string> _openViewIds = new List<string>();

        #endregion


        #region Public API

        /// <summary>
        /// Gets the views of a table.
        /// </summary>
        /// <param name="tableId">The stable identifier of the table (see <see cref="TableDefinition.Id"/>).</param>
        /// <returns>Returns the views of the table, in the order they are listed, or an empty list if it has none.</returns>
        public List<PersonalView> GetViews(string tableId)
        {
            return _views.FindAll(view => IsValid(view) && view.TableId == tableId);
        }

        /// <summary>
        /// Gets the view open for a table: the view the user used last. Creates the <see cref="DefaultViewName"/> view if the table has
        /// none, and opens the first view of the table if none is open.
        /// </summary>
        /// <param name="tableId">The stable identifier of the table (see <see cref="TableDefinition.Id"/>).</param>
        /// <returns>Returns the open view of the table.</returns>
        /// <exception cref="ArgumentException">Thrown if the identifier is null or empty.</exception>
        public PersonalView GetOpenView(string tableId)
        {
            if (string.IsNullOrEmpty(tableId))
                throw new ArgumentException("The identifier of the table can't be null or empty.", nameof(tableId));

            List<PersonalView> views = GetViews(tableId);
            PersonalView openView = views.Find(view => _openViewIds.Contains(view.Id));
            if (openView != null)
                return openView;

            openView = views.Count > 0 ? views[0] : AddView(new PersonalView(DefaultViewName, new ViewSettings(tableId)), -1);
            SetOpenView(openView);
            return openView;
        }

        /// <summary>
        /// Checks if a view is the open view of its table.
        /// </summary>
        /// <param name="view">The view to check.</param>
        /// <returns>Returns true if the view is open.</returns>
        public bool IsOpen(PersonalView view)
        {
            return view != null && _openViewIds.Contains(view.Id);
        }

        /// <summary>
        /// Opens a view for its table, instead of the view that was open.
        /// </summary>
        /// <param name="view">The view to open.</param>
        /// <returns>Returns true if the open view has changed.</returns>
        /// <exception cref="ArgumentException">Thrown if the view is not one of these views.</exception>
        public bool SetOpenView(PersonalView view)
        {
            CheckContains(view);
            if (IsOpen(view))
                return false;

            foreach (PersonalView tableView in GetViews(view.TableId))
                _openViewIds.Remove(tableView.Id);

            _openViewIds.Add(view.Id);
            return true;
        }

        /// <summary>
        /// Creates a view with the defaults of a table, after its other views. The view is not opened.
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
            return AddView(new PersonalView(GetUniqueName(tableId, name), new ViewSettings(tableId)), -1);
        }

        /// <summary>
        /// Creates a copy of a view, with a copy of its settings, right after it. The copy is not opened.
        /// </summary>
        /// <param name="view">The view to copy.</param>
        /// <returns>Returns the created copy.</returns>
        /// <exception cref="ArgumentException">Thrown if the view is not one of these views.</exception>
        public PersonalView DuplicateView(PersonalView view)
        {
            CheckContains(view);
            string name = GetUniqueName(view.TableId, $"{view.Name}{CopySuffix}");
            return AddView(new PersonalView(name, view.Settings.Clone()), _views.IndexOf(view) + 1);
        }

        /// <summary>
        /// Renames a view.
        /// </summary>
        /// <param name="view">The view to rename.</param>
        /// <param name="name">The new name of the view, trimmed. A name taken by another view of the table gets a number.</param>
        /// <returns>Returns true if the name has changed, or false if it's the same or blank.</returns>
        /// <exception cref="ArgumentException">Thrown if the view is not one of these views.</exception>
        public bool RenameView(PersonalView view, string name)
        {
            CheckContains(view);
            if (string.IsNullOrWhiteSpace(name))
                return false;

            name = GetUniqueName(view.TableId, name.Trim(), view);
            if (name == view.Name)
                return false;

            view.Name = name;
            return true;
        }

        /// <summary>
        /// Checks if a view can be deleted: it's not the last view of its table.
        /// </summary>
        /// <param name="view">The view to check.</param>
        /// <returns>Returns true if the view can be deleted.</returns>
        public bool CanDeleteView(PersonalView view)
        {
            return view != null && _views.Contains(view) && GetViews(view.TableId).Count > 1;
        }

        /// <summary>
        /// Deletes a view. If it's the open view of its table, the next view of the table is opened instead (or the previous one if it
        /// was the last).
        /// </summary>
        /// <param name="view">The view to delete.</param>
        /// <returns>Returns true if the view has been deleted, or false if it's the last view of its table.</returns>
        /// <exception cref="ArgumentException">Thrown if the view is not one of these views.</exception>
        public bool DeleteView(PersonalView view)
        {
            CheckContains(view);
            if (!CanDeleteView(view))
                return false;

            List<PersonalView> tableViews = GetViews(view.TableId);
            int index = tableViews.IndexOf(view);
            bool wasOpen = IsOpen(view);
            _views.Remove(view);
            _openViewIds.Remove(view.Id);

            if (wasOpen)
            {
                tableViews.RemoveAt(index);
                SetOpenView(tableViews[Mathf.Min(index, tableViews.Count - 1)]);
            }

            return true;
        }

        /// <summary>
        /// Gets a name that no other view of a table has, ignoring case.
        /// </summary>
        /// <param name="tableId">The stable identifier of the table.</param>
        /// <param name="name">The wanted name.</param>
        /// <param name="ignoredView">A view whose name doesn't count, for example the renamed view, or null.</param>
        /// <returns>Returns the wanted name if it's free, or the name followed by the first free number from 2 otherwise.</returns>
        /// <example>
        /// With the views <c>Default</c> and <c>New View</c>, <c>GetUniqueName(tableId, "new view")</c> returns <c>new view 2</c>.
        /// </example>
        public string GetUniqueName(string tableId, string name, PersonalView ignoredView = null)
        {
            List<PersonalView> views = GetViews(tableId);
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

        /// <summary>
        /// Adds the settings saved before the views, one per table, as the <see cref="DefaultViewName"/> view of their tables, open.
        /// </summary>
        /// <remarks>
        /// The settings of a table that already has views, and the settings without table, are skipped. The settings are used as
        /// they are, not copied.
        /// </remarks>
        /// <param name="defaultViews">The settings of each table saved before the views.</param>
        /// <returns>Returns the number of views added.</returns>
        public int ImportDefaultViews(IEnumerable<ViewSettings> defaultViews)
        {
            int count = 0;
            foreach (ViewSettings settings in defaultViews)
            {
                if (settings == null || string.IsNullOrEmpty(settings.TableId) || GetViews(settings.TableId).Count > 0)
                    continue;

                SetOpenView(AddView(new PersonalView(DefaultViewName, settings), -1));
                count++;
            }

            return count;
        }

        #endregion


        #region Private API

        /// <summary>
        /// Adds a view to the list.
        /// </summary>
        /// <param name="view">The view to add.</param>
        /// <param name="index">The index at which to insert the view, or a negative index to add it at the end.</param>
        /// <returns>Returns the added view.</returns>
        private PersonalView AddView(PersonalView view, int index)
        {
            if (index >= 0 && index < _views.Count)
                _views.Insert(index, view);
            else
                _views.Add(view);

            return view;
        }

        /// <summary>
        /// Checks if a view of a list has a name, ignoring case.
        /// </summary>
        /// <param name="views">The views to check.</param>
        /// <param name="name">The name to find.</param>
        /// <returns>Returns true if a view has the name.</returns>
        private static bool IsNameTaken(List<PersonalView> views, string name)
        {
            return views.Exists(view => string.Equals(view.Name, name, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Checks if a view can be used: it has an id and settings with a table. A hand-edited file can contain views that can't.
        /// </summary>
        /// <param name="view">The view to check.</param>
        /// <returns>Returns true if the view can be used.</returns>
        private static bool IsValid(PersonalView view)
        {
            return view != null && !string.IsNullOrEmpty(view.Id) && !string.IsNullOrEmpty(view.TableId);
        }

        /// <summary>
        /// Throws if a view is not one of these views.
        /// </summary>
        /// <param name="view">The view to check.</param>
        /// <exception cref="ArgumentException">Thrown if the view is null or not one of these views.</exception>
        private void CheckContains(PersonalView view)
        {
            if (!IsValid(view) || !_views.Contains(view))
                throw new ArgumentException("The view is not one of the personal views.", nameof(view));
        }

        #endregion

    }

}
