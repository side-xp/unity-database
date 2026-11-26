namespace SideXP.Database.EditorOnly
{

    /// <summary>
    /// A kind of setting of a view, which a layer sets or unsets on its own (see <see cref="ViewSettings.IsSet"/> and
    /// <see cref="ViewSettings.Unset"/>). The widths and the visibility are set per column, but listed and reset for all the columns at
    /// once.
    /// </summary>
    internal enum ViewSettingKind
    {

        /// <summary>
        /// The display order of the columns.
        /// </summary>
        ColumnOrder = 0,

        /// <summary>
        /// The widths of the columns chosen by the user.
        /// </summary>
        ColumnWidths = 1,

        /// <summary>
        /// The visibility of the columns.
        /// </summary>
        ColumnVisibility = 2,

        /// <summary>
        /// The columns used to sort the rows.
        /// </summary>
        Sort = 3,

        /// <summary>
        /// The filter tree.
        /// </summary>
        Filters = 4,

        /// <summary>
        /// The folders of the folder scope.
        /// </summary>
        Folders = 5,

    }

}
