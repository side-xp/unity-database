using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SideXP.Database.EditorOnly
{

    /// <summary>
    /// Exports the rows of a table to delimited text files (CSV and TSV), which spreadsheet apps can open.
    /// </summary>
    /// <remarks>
    /// A file starts with a header row: the GUID column (see <see cref="GuidColumnName"/>), always first, then the names of the
    /// exported columns. Each next row is an asset: its identifier (see <see cref="AssetIds"/>), then its values converted to text by
    /// the handlers of the columns, like the copied cells (see <see cref="TableViewColumn.ToText(object, string)"/>): paths for
    /// references, names for enums, numbers in the invariant culture. The <i>Controls</i> column has no value, so it's never exported.
    /// <para>
    /// The values are quoted following the rules of <see cref="DelimitedText"/>, the rows end with <see cref="LineBreak"/>, and the text
    /// is encoded in UTF-8 with a byte order mark, without which Excel reads it in the code page of the system.
    /// </para>
    /// </remarks>
    internal static class TableExport
    {

        #region Fields

        /// <summary>
        /// The name of the column that identifies the assets, first in the header row of the exported files.
        /// </summary>
        public const string GuidColumnName = "GUID";

        /// <summary>
        /// The line break written after each row of the exported files: the one of RFC 4180, and of the files written by spreadsheet
        /// apps.
        /// </summary>
        public const string LineBreak = "\r\n";

        /// <summary>
        /// Why the values are converted to text, at the end of the error message of a cell handler that fails.
        /// </summary>
        private const string ExportPurpose = "to export it";

        #endregion


        #region Public API

        /// <summary>
        /// Gets the character that separates the values of a row in a format.
        /// </summary>
        /// <param name="format">The format of the file.</param>
        /// <returns>Returns the delimiter of the format.</returns>
        public static char GetDelimiter(DelimitedFormat format)
        {
            return format == DelimitedFormat.Tsv ? DelimitedText.Tab : DelimitedText.Comma;
        }

        /// <summary>
        /// Gets the extension of the files of a format.
        /// </summary>
        /// <param name="format">The format of the file.</param>
        /// <returns>Returns the extension, without the dot (<c>csv</c> or <c>tsv</c>).</returns>
        public static string GetExtension(DelimitedFormat format)
        {
            return format == DelimitedFormat.Tsv ? "tsv" : "csv";
        }

        /// <summary>
        /// Gets the name of a format, as shown to the user.
        /// </summary>
        /// <param name="format">The format of the file.</param>
        /// <returns>Returns the name of the format (<c>CSV</c> or <c>TSV</c>).</returns>
        public static string GetFormatName(DelimitedFormat format)
        {
            return GetExtension(format).ToUpperInvariant();
        }

        /// <summary>
        /// Gets the texts of the exported rows: the header row, then a row per asset.
        /// </summary>
        /// <param name="columns">All the columns of the table. The index of a column is its index in the pipeline of the rows.</param>
        /// <param name="columnIndices">The indices of the columns to export, in their order in the file. The <i>Controls</i> column is
        /// skipped.</param>
        /// <param name="rows">The rows to export, in their order in the file.</param>
        /// <returns>Returns the rows of texts, the header row first. Each row starts with the GUID column.</returns>
        public static List<List<string>> GetRows(IReadOnlyList<TableViewColumn> columns, IReadOnlyList<int> columnIndices,
            IReadOnlyList<TableRow> rows)
        {
            List<TableViewColumn> exportedColumns = new List<TableViewColumn>(columnIndices.Count);
            List<int> exportedIndices = new List<int>(columnIndices.Count);
            foreach (int columnIndex in columnIndices)
            {
                if (columns[columnIndex].IsControls)
                    continue;

                exportedColumns.Add(columns[columnIndex]);
                exportedIndices.Add(columnIndex);
            }

            List<List<string>> texts = new List<List<string>>(rows.Count + 1);
            List<string> header = new List<string>(exportedColumns.Count + 1) { GuidColumnName };
            header.AddRange(exportedColumns.ConvertAll(column => column.Name));
            texts.Add(header);

            foreach (TableRow row in rows)
            {
                List<string> rowTexts = new List<string>(exportedColumns.Count + 1) { AssetIds.GetId(row.Asset) };
                for (int i = 0; i < exportedColumns.Count; i++)
                    rowTexts.Add(exportedColumns[i].ToText(row.GetValue(exportedIndices[i]), ExportPurpose) ?? string.Empty);

                texts.Add(rowTexts);
            }

            return texts;
        }

        /// <summary>
        /// Loads all the rows of a table, ignoring the folder scope, the filters and the search of the view: every asset of the table
        /// type in the project, with the values of all the columns, computed columns included.
        /// </summary>
        /// <remarks>
        /// The rows come from a pipeline of their own, so the rows of the view don't change. Without filter, the computed values are
        /// computed for all the rows, with all the assets as the visible assets of their context.
        /// </remarks>
        /// <param name="tableType">The type of the assets of the table.</param>
        /// <param name="accessors">The accessors of the columns, in the order of the columns of the table.</param>
        /// <returns>Returns the rows, in the order of the assets in the project (by path, sub-assets after their main asset).</returns>
        public static IReadOnlyList<TableRow> LoadAllRows(Type tableType, IReadOnlyList<IValueAccessor> accessors)
        {
            TablePipeline pipeline = new TablePipeline(new ProjectAssetSource(tableType), accessors);
            pipeline.Update();
            return pipeline.Rows;
        }

        /// <summary>
        /// Writes rows of texts in a format, each row ended by <see cref="LineBreak"/>.
        /// </summary>
        /// <param name="rows">The rows to write (see <see cref="GetRows"/>).</param>
        /// <param name="format">The format of the file.</param>
        /// <returns>Returns the text of the file.</returns>
        public static string Write(IReadOnlyList<IReadOnlyList<string>> rows, DelimitedFormat format)
        {
            if (rows.Count == 0)
                return string.Empty;

            // The last row ends with a line break too, like the files written by spreadsheet apps
            return $"{DelimitedText.Write(rows, GetDelimiter(format), LineBreak)}{LineBreak}";
        }

        /// <summary>
        /// Writes the text of an exported file, in UTF-8 with a byte order mark. Replaces the file if it exists.
        /// </summary>
        /// <param name="path">The path of the file.</param>
        /// <param name="text">The text of the file (see <see cref="Write"/>).</param>
        /// <exception cref="IOException">Thrown if the file can't be written, for example if a spreadsheet app has it open.</exception>
        /// <exception cref="UnauthorizedAccessException">Thrown if the user can't write the file.</exception>
        public static void WriteFile(string path, string text)
        {
            File.WriteAllText(path, text, new UTF8Encoding(true));
        }

        #endregion

    }

}
