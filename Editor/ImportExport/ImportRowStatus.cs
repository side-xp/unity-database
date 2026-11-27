namespace SideXP.Database.EditorOnly
{

    /// <summary>
    /// What an import does with a row of its file (see <see cref="ImportPlan"/>).
    /// </summary>
    internal enum ImportRowStatus
    {

        /// <summary>
        /// The row matches an asset, whose values and name are already the ones of the file.
        /// </summary>
        Unchanged,

        /// <summary>
        /// The row matches an asset, and changes some of its values or its name.
        /// </summary>
        Changed,

        /// <summary>
        /// The row matches no asset, and the import creates one with its values.
        /// </summary>
        Created,

        /// <summary>
        /// The row matches no asset, and the import doesn't create the missing assets: the row is skipped.
        /// </summary>
        Missing,

        /// <summary>
        /// The path or the name of the row is shared by several assets, so it matches none of them: the row is skipped.
        /// </summary>
        Ambiguous,

        /// <summary>
        /// The row matches an asset that an earlier row of the file already matches: the row is skipped.
        /// </summary>
        Duplicate,

    }

}
