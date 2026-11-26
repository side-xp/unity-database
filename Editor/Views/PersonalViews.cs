using System;
using System.Collections.Generic;

using UnityEngine;

namespace SideXP.Database.EditorOnly
{

    /// <summary>
    /// The data of the views that only the user sees: the personal views of the tables (see <see cref="PersonalView"/>), the open view
    /// of each table, and the personal overrides of the project views (see <see cref="ViewOverride"/>). Stored by
    /// <see cref="UserViews"/>.
    /// </summary>
    /// <remarks>
    /// This class only stores the data. The rules (a view per table at least, unique names, the open view after a deletion) are
    /// applied by <see cref="ViewCatalog"/>, since they also concern the project views.
    /// <para>
    /// The changes are made in memory: the owner saves after each change (see <see cref="UserViews.ScheduleSave"/>).
    /// </para>
    /// </remarks>
    [Serializable]
    internal class PersonalViews
    {

        #region Fields

        /// <summary>
        /// The personal views of all the tables, in the order they are listed. Each table's views keep their relative order.
        /// </summary>
        /// <remarks>
        /// A list, since Unity can't serialize dictionaries. The number of views of a project is small enough for a linear search.
        /// </remarks>
        [SerializeField]
        private List<PersonalView> _views = new List<PersonalView>();

        /// <summary>
        /// The identifiers of the open views, personal or project views, one per table at most.
        /// </summary>
        [SerializeField]
        private List<string> _openViewIds = new List<string>();

        /// <summary>
        /// The personal overrides of the project views, one per project view at most.
        /// </summary>
        [SerializeField]
        private List<ViewOverride> _overrides = new List<ViewOverride>();

        #endregion


        #region Views

        /// <summary>
        /// Gets the personal views of a table.
        /// </summary>
        /// <param name="tableId">The stable identifier of the table (see <see cref="TableDefinition.Id"/>).</param>
        /// <returns>Returns the views of the table, in the order they are listed, or an empty list if it has none.</returns>
        public List<PersonalView> GetViews(string tableId)
        {
            return _views.FindAll(view => view != null && view.IsValid && view.TableId == tableId);
        }

        /// <summary>
        /// Checks if a view is one of these views.
        /// </summary>
        /// <param name="view">The view to check.</param>
        /// <returns>Returns true if the view is one of these views and can be used.</returns>
        public bool Contains(PersonalView view)
        {
            return view != null && view.IsValid && _views.Contains(view);
        }

        /// <summary>
        /// Adds a view.
        /// </summary>
        /// <param name="view">The view to add.</param>
        /// <param name="nextTo">A view of the same table after which to insert the view, or null to add it at the end.</param>
        /// <returns>Returns the added view.</returns>
        /// <exception cref="ArgumentException">Thrown if the view can't be used, or is already one of these views.</exception>
        public PersonalView Add(PersonalView view, PersonalView nextTo = null)
        {
            if (view == null || !view.IsValid || _views.Contains(view))
                throw new ArgumentException("The view can't be used, or is already one of the personal views.", nameof(view));

            int index = nextTo != null ? _views.IndexOf(nextTo) : -1;
            if (index >= 0)
                _views.Insert(index + 1, view);
            else
                _views.Add(view);

            return view;
        }

        /// <summary>
        /// Removes a view. Its id stays in the open views: the owner opens another view of its table.
        /// </summary>
        /// <param name="view">The view to remove.</param>
        /// <returns>Returns true if the view has been removed.</returns>
        public bool Remove(PersonalView view)
        {
            return _views.Remove(view);
        }

        #endregion


        #region Open views

        /// <summary>
        /// Checks if a view is open.
        /// </summary>
        /// <param name="viewId">The identifier of the view, personal or project view.</param>
        /// <returns>Returns true if the view is open.</returns>
        public bool IsOpen(string viewId)
        {
            return !string.IsNullOrEmpty(viewId) && _openViewIds.Contains(viewId);
        }

        /// <summary>
        /// Opens a view, instead of the other views of its table.
        /// </summary>
        /// <param name="viewId">The identifier of the view to open.</param>
        /// <param name="tableViewIds">The identifiers of all the views of the table, personal and project views, which are closed.</param>
        /// <returns>Returns true if the open view has changed.</returns>
        public bool SetOpen(string viewId, IEnumerable<string> tableViewIds)
        {
            if (IsOpen(viewId))
                return false;

            foreach (string tableViewId in tableViewIds)
                _openViewIds.Remove(tableViewId);

            _openViewIds.Add(viewId);
            return true;
        }

        /// <summary>
        /// Forgets that a view is open, for example after it's deleted.
        /// </summary>
        /// <param name="viewId">The identifier of the view.</param>
        /// <returns>Returns true if the view was open.</returns>
        public bool RemoveOpen(string viewId)
        {
            return _openViewIds.Remove(viewId);
        }

        #endregion


        #region Overrides

        /// <summary>
        /// Gets the overrides of a project view.
        /// </summary>
        /// <param name="viewId">The identifier of the project view.</param>
        /// <returns>Returns the overrides of the view, or null if the user has none.</returns>
        public ViewOverride FindOverride(string viewId)
        {
            return _overrides.Find(item => item != null && item.Settings != null && item.ViewId == viewId);
        }

        /// <summary>
        /// Gets the overrides of a project view, and creates them, without any setting, if the user has none yet.
        /// </summary>
        /// <param name="view">The project view.</param>
        /// <returns>Returns the settings the user changed in the project view.</returns>
        /// <exception cref="ArgumentException">Thrown if the view can't be used.</exception>
        public ViewSettings GetOrCreateOverride(ProjectView view)
        {
            if (view == null || !view.IsValid)
                throw new ArgumentException("The project view can't be used.", nameof(view));

            ViewOverride viewOverride = FindOverride(view.Id);
            if (viewOverride == null)
            {
                viewOverride = new ViewOverride(view.Id, new ViewSettings(view.TableId));
                _overrides.Add(viewOverride);
            }

            return viewOverride.Settings;
        }

        /// <summary>
        /// Removes the overrides of a project view.
        /// </summary>
        /// <param name="viewId">The identifier of the project view.</param>
        /// <returns>Returns true if the user had overrides on the view.</returns>
        public bool RemoveOverride(string viewId)
        {
            return _overrides.RemoveAll(item => item != null && item.ViewId == viewId) > 0;
        }

        #endregion


        #region Settings of the first pass

        /// <summary>
        /// Adds the settings saved before the views, one per table, as the <see cref="ViewCatalog.DefaultViewName"/> view of their
        /// tables, open.
        /// </summary>
        /// <remarks>
        /// The settings of a table that already has personal views, and the settings without table, are skipped. The settings are used
        /// as they are, not copied. No table has project views yet when these settings are read.
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

                PersonalView view = Add(new PersonalView(ViewCatalog.DefaultViewName, settings));
                SetOpen(view.Id, Array.Empty<string>());
                count++;
            }

            return count;
        }

        #endregion

    }

}
