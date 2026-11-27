namespace SideXP.Database.EditorOnly
{

    /// <summary>
    /// The kind of a view, and whether the user changed it (see <see cref="ViewCatalog.GetStatus"/>).
    /// </summary>
    internal enum ViewStatus
    {

        /// <summary>
        /// A personal view, only visible to its user.
        /// </summary>
        Personal = 0,

        /// <summary>
        /// A project view, shared with the team, that the user didn't change: it's displayed as saved in the project file.
        /// </summary>
        Project = 1,

        /// <summary>
        /// A project view, shared with the team, with personal overrides of the user: the user sees their changes, the team doesn't.
        /// </summary>
        OverriddenProject = 2,

    }

}
