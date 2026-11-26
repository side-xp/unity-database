namespace SideXP.Database.EditorOnly
{

    /// <summary>
    /// Declares the names of the Tabler icons used by the package, displayed with the <c>EditorIcons</c> feature of the Core package
    /// (see the <c>Icon</c> element). Browse them from <i>Tools &gt; Sideways Experiments &gt; Viewers &gt; Editor Icons</i>.
    /// </summary>
    /// <remarks>
    /// Unity's own built-in icons are declared in <see cref="BuiltInIcons"/>.
    /// </remarks>
    internal static class DatabaseIcons
    {

        #region Fields

        /// <summary>
        /// A left chevron, for the button that goes back to the table list.
        /// </summary>
        public const string Back = "chevron-left";

        /// <summary>
        /// Two circular arrows, for the button that loads the assets of the table again.
        /// </summary>
        public const string Refresh = "refresh";

        /// <summary>
        /// A plus sign, for the buttons that create an asset when the table has a single type to create.
        /// </summary>
        public const string Create = "plus";

        /// <summary>
        /// A list with a plus sign, for the buttons that create an asset when the table has several types to choose from in a menu.
        /// </summary>
        public const string CreateWithChoice = "playlist-add";

        /// <summary>
        /// A magnifying glass, for the button that selects an asset and shows it in the Project window.
        /// </summary>
        public const string Select = "search";

        /// <summary>
        /// Two overlapping squares, for the button that duplicates an asset.
        /// </summary>
        public const string Duplicate = "duplicate";

        /// <summary>
        /// A trash can, for the button that deletes an asset.
        /// </summary>
        public const string Delete = "trash";

        /// <summary>
        /// A chain link, for the button of the selection sync while it's enabled.
        /// </summary>
        public const string SelectionSynced = "link";

        /// <summary>
        /// A broken chain link, for the button of the selection sync while it's disabled.
        /// </summary>
        public const string SelectionNotSynced = "link-off";

        /// <summary>
        /// A window with a panel on its right, for the button that shows and hides the view panel.
        /// </summary>
        public const string ViewPanel = "layout-sidebar-right";

        /// <summary>
        /// An eye, for the button of the views and the status of the view panel while the open view is a personal view (see
        /// <see cref="ViewStatus.Personal"/>).
        /// </summary>
        public const string PersonalView = "eye";

        /// <summary>
        /// An eye with a check mark, for the button of the views and the status of the view panel while the open view is a project
        /// view without overrides (see <see cref="ViewStatus.Project"/>).
        /// </summary>
        public const string ProjectView = "eye-check";

        /// <summary>
        /// An eye with a bolt, for the button of the views and the status of the view panel while the open view is a project view
        /// with overrides of the user (see <see cref="ViewStatus.OverriddenProject"/>).
        /// </summary>
        public const string OverriddenProjectView = "eye-bolt";

        /// <summary>
        /// A cloud with an arrow pointing up, for the buttons that write the overrides of the user to the project view, for the whole
        /// team (<i>Save for Everyone</i>).
        /// </summary>
        public const string SaveOverrides = "cloud-upload";

        /// <summary>
        /// An arrow turning back, for the buttons that discard the overrides of the user on a project view, all of them or one kind.
        /// </summary>
        public const string ResetOverrides = "arrow-back-up";

        /// <summary>
        /// A chevron pointing down, after the name of the open view, to show that the button opens a menu.
        /// </summary>
        public const string MenuArrow = "chevron-down";

        /// <summary>
        /// A funnel, shown in the header of a column that the filters use.
        /// </summary>
        public const string Filter = "filter";

        /// <summary>
        /// A funnel with a bar across it, for the button that clears the filters.
        /// </summary>
        public const string ClearFilters = "filter-off";

        /// <summary>
        /// An arrow that splits in two, for the button that adds a condition to a group of the filters.
        /// </summary>
        public const string AddCondition = "arrows-split";

        /// <summary>
        /// Several circles, for the button that adds a group of conditions to a group of the filters.
        /// </summary>
        public const string AddGroup = "circles";

        /// <summary>
        /// A folder with a plus sign, for the button that adds a folder to the folder scope.
        /// </summary>
        public const string AddFolder = "folder-plus";

        /// <summary>
        /// A folder with a bar across it, for the button that clears the folder scope.
        /// </summary>
        public const string ClearFolders = "folder-off";

        /// <summary>
        /// A cross, for the buttons that remove an item from a list, such as a condition from a filter.
        /// </summary>
        public const string Remove = "x";

        /// <summary>
        /// A left chevron before a bar, for the button that shows the first page of the rows.
        /// </summary>
        public const string FirstPage = "chevron-left-pipe";

        /// <summary>
        /// A left chevron, for the button that shows the previous page of the rows.
        /// </summary>
        public const string PreviousPage = "chevron-left";

        /// <summary>
        /// A right chevron, for the button that shows the next page of the rows.
        /// </summary>
        public const string NextPage = "chevron-right";

        /// <summary>
        /// A right chevron before a bar, for the button that shows the last page of the rows.
        /// </summary>
        public const string LastPage = "chevron-right-pipe";

        /// <summary>
        /// An arrow pointing up, shown in the header of a column sorted in ascending order.
        /// </summary>
        public const string SortAscending = "arrow-up";

        /// <summary>
        /// An arrow pointing down, shown in the header of a column sorted in descending order.
        /// </summary>
        public const string SortDescending = "arrow-down";

        #endregion

    }

}
