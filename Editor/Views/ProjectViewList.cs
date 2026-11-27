using System;
using System.Collections.Generic;

using UnityEngine;

namespace SideXP.Database.EditorOnly
{

    /// <summary>
    /// The project views of the tables (see <see cref="ProjectView"/>), shared with the team. Stored by <see cref="ProjectViews"/>.
    /// </summary>
    /// <remarks>
    /// This class only stores the views. The rules are applied by <see cref="ViewCatalog"/>, and the changes of the users are saved
    /// as personal overrides (see <see cref="PersonalViews"/>), so this list only changes when a view is shared, renamed or deleted.
    /// </remarks>
    [Serializable]
    internal class ProjectViewList
    {

        #region Fields

        /// <summary>
        /// The project views of all the tables, in the order they are listed. Each table's views keep their relative order.
        /// </summary>
        [SerializeField]
        private List<ProjectView> _views = new List<ProjectView>();

        #endregion


        #region Public API

        /// <summary>
        /// Gets the project views of a table.
        /// </summary>
        /// <param name="tableId">The stable identifier of the table (see <see cref="TableDefinition.Id"/>).</param>
        /// <returns>Returns the views of the table, in the order they are listed, or an empty list if it has none.</returns>
        public List<ProjectView> GetViews(string tableId)
        {
            return _views.FindAll(view => view != null && view.IsValid && view.TableId == tableId);
        }

        /// <summary>
        /// Checks if a view is one of these views.
        /// </summary>
        /// <param name="view">The view to check.</param>
        /// <returns>Returns true if the view is one of these views and can be used.</returns>
        public bool Contains(ProjectView view)
        {
            return view != null && view.IsValid && _views.Contains(view);
        }

        /// <summary>
        /// Adds a view at the end of the list.
        /// </summary>
        /// <param name="view">The view to add.</param>
        /// <returns>Returns the added view.</returns>
        /// <exception cref="ArgumentException">Thrown if the view can't be used, or is already one of these views.</exception>
        public ProjectView Add(ProjectView view)
        {
            if (view == null || !view.IsValid || _views.Contains(view))
                throw new ArgumentException("The view can't be used, or is already one of the project views.", nameof(view));

            _views.Add(view);
            return view;
        }

        /// <summary>
        /// Removes a view.
        /// </summary>
        /// <param name="view">The view to remove.</param>
        /// <returns>Returns true if the view has been removed.</returns>
        public bool Remove(ProjectView view)
        {
            return _views.Remove(view);
        }

        #endregion

    }

}
