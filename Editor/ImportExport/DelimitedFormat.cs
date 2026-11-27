namespace SideXP.Database.EditorOnly
{

    /// <summary>
    /// The formats of delimited text files that the tables can be exported to (see <see cref="TableExport"/>).
    /// </summary>
    internal enum DelimitedFormat
    {

        /// <summary>
        /// Comma-separated values, in a <c>.csv</c> file.
        /// </summary>
        Csv,

        /// <summary>
        /// Tab-separated values, in a <c>.tsv</c> file.
        /// </summary>
        Tsv,

    }

}
