namespace SideXP.Database.EditorOnly
{

    /// <summary>
    /// How a row of a file to import has been matched with an asset of the table (see <see cref="ImportRowMatcher"/>).
    /// </summary>
    internal enum ImportMatchKind
    {

        /// <summary>
        /// The row matches no asset.
        /// </summary>
        None,

        /// <summary>
        /// Matched by the identifier of the asset in the identity column (see <see cref="AssetIds"/>).
        /// </summary>
        Id,

        /// <summary>
        /// Matched by the path of the asset in the identity column.
        /// </summary>
        Path,

        /// <summary>
        /// Matched by the name of the asset in the <i>Asset Name</i> column.
        /// </summary>
        Name,

    }

}
