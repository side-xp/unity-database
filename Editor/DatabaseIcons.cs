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

        #endregion

    }

}
