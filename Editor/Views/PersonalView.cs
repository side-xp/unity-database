using System;

namespace SideXP.Database.EditorOnly
{

    /// <summary>
    /// A view of a table that only its user sees, stored with the personal views (see <see cref="PersonalViews"/>).
    /// </summary>
    [Serializable]
    internal class PersonalView : SavedView
    {

        #region Lifecycle

        /// <inheritdoc cref="SavedView(string, ViewSettings)"/>
        public PersonalView(string name, ViewSettings settings)
            : base(name, settings)
        {
        }

        #endregion


        #region Public API

        /// <inheritdoc/>
        public override bool IsProjectView => false;

        #endregion

    }

}
