namespace SideXP.Database.EditorOnly
{

    /// <summary>
    /// The visibility of a column in a layer of the settings of a view (see <see cref="ColumnSettings"/>).
    /// </summary>
    internal enum ColumnVisibility
    {

        /// <summary>
        /// The layer doesn't set the visibility of the column: it comes from the layer below, or from the defaults of the table.
        /// </summary>
        Unset = 0,

        /// <summary>
        /// The column is displayed.
        /// </summary>
        Visible = 1,

        /// <summary>
        /// The column is hidden.
        /// </summary>
        Hidden = 2,

    }

}
