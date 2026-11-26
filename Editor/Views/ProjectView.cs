using System;

namespace SideXP.Database.EditorOnly
{

    /// <summary>
    /// A view of a table shared with the team through version control, stored with the project views (see
    /// <see cref="ProjectViewList"/>).
    /// </summary>
    /// <remarks>
    /// The changes a user makes to the settings of a project view are saved as personal overrides (see
    /// <see cref="PersonalViews.GetOrCreateOverride"/>), never in the settings of the project view. Its name and its deletion are
    /// written to the project file (see <see cref="ViewCatalog"/>).
    /// </remarks>
    [Serializable]
    internal class ProjectView : SavedView
    {

        #region Lifecycle

        /// <inheritdoc cref="SavedView(string, ViewSettings)"/>
        public ProjectView(string name, ViewSettings settings)
            : base(name, settings)
        {
        }

        /// <inheritdoc cref="SavedView(string, string, ViewSettings)"/>
        public ProjectView(string id, string name, ViewSettings settings)
            : base(id, name, settings)
        {
        }

        #endregion


        #region Public API

        /// <inheritdoc/>
        public override bool IsProjectView => true;

        #endregion

    }

}
