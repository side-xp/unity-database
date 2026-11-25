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
