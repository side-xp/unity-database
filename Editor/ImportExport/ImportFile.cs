using System;
using System.Collections.Generic;
using System.IO;

namespace SideXP.Database.EditorOnly
{

    /// <summary>
    /// A delimited text file read for an import: its header row and its data rows, as texts.
    /// </summary>
    /// <remarks>
    /// The first row is the header, which names the columns. The rows whose values are all blank are skipped, like the empty lines at
    /// the end of a file written by hand. A row can have fewer or more values than the header: a missing value is read as an empty
    /// text, and the values without header are ignored.
    /// <para>
    /// The values of a CSV file are separated by commas, or by semicolons when its header has more semicolons than commas: spreadsheet
    /// apps save CSV files with semicolons in the languages that write decimal numbers with a comma (French and German, for example).
    /// </para>
    /// </remarks>
    internal class ImportFile
    {

        #region Fields

        /// <summary>
        /// The delimiter of the CSV files written by spreadsheet apps in the languages that write decimal numbers with a comma.
        /// </summary>
        public const char Semicolon = ';';

        private const char Quote = '"';

        /// <summary>
        /// The name of the file, with its extension, or null if the text doesn't come from a file.
        /// </summary>
        private string _name = null;

        /// <summary>
        /// The character that separates the values of the rows.
        /// </summary>
        private char _delimiter = DelimitedText.Comma;

        /// <summary>
        /// The texts of the header row, which name the columns of the file. Empty if the file has no row.
        /// </summary>
        private List<string> _header = new List<string>();

        /// <summary>
        /// The texts of the data rows, without the header and the blank rows, in the order of the file.
        /// </summary>
        private List<List<string>> _rows = new List<List<string>>();

        /// <summary>
        /// The number of each data row in the file, from 1 for the header, so the messages can name the rows as a spreadsheet app
        /// shows them.
        /// </summary>
        private List<int> _rowNumbers = new List<int>();

        #endregion


        #region Lifecycle

        /// <summary>
        /// Creates a file from its parsed rows.
        /// </summary>
        /// <param name="name"><inheritdoc cref="_name" path="/summary"/></param>
        /// <param name="delimiter"><inheritdoc cref="_delimiter" path="/summary"/></param>
        /// <param name="rows">All the parsed rows of the file, the header first.</param>
        private ImportFile(string name, char delimiter, List<List<string>> rows)
        {
            _name = name;
            _delimiter = delimiter;
            if (rows.Count == 0)
                return;

            _header = rows[0];
            for (int i = 1; i < rows.Count; i++)
            {
                if (IsBlank(rows[i]))
                    continue;

                _rows.Add(rows[i]);
                _rowNumbers.Add(i + 1);
            }
        }

        #endregion


        #region Public API

        /// <inheritdoc cref="_name"/>
        public string Name => _name;

        /// <inheritdoc cref="_delimiter"/>
        public char Delimiter => _delimiter;

        /// <inheritdoc cref="_header"/>
        public IReadOnlyList<string> Header => _header;

        /// <summary>
        /// Gets the number of data rows, without the header and the blank rows.
        /// </summary>
        public int RowCount => _rows.Count;

        /// <summary>
        /// Gets a value of a data row.
        /// </summary>
        /// <param name="rowIndex">The index of the data row, from 0.</param>
        /// <param name="columnIndex">The index of the column in the file, from 0.</param>
        /// <returns>Returns the text of the value, or an empty text if the row has no value in that column.</returns>
        public string GetText(int rowIndex, int columnIndex)
        {
            List<string> row = _rows[rowIndex];
            return columnIndex >= 0 && columnIndex < row.Count ? row[columnIndex] ?? string.Empty : string.Empty;
        }

        /// <summary>
        /// Gets the number of a data row in the file, as a spreadsheet app shows it.
        /// </summary>
        /// <param name="rowIndex">The index of the data row, from 0.</param>
        /// <returns>Returns the number of the row, from 2, since the header is the row 1.</returns>
        public int GetRowNumber(int rowIndex)
        {
            return _rowNumbers[rowIndex];
        }

        /// <summary>
        /// Gets the format of a file from its extension.
        /// </summary>
        /// <param name="path">The path of the file.</param>
        /// <param name="format">Outputs the format of the file.</param>
        /// <returns>Returns true if the extension is the one of a supported format (<c>.csv</c> or <c>.tsv</c>, ignoring case).</returns>
        public static bool TryGetFormat(string path, out DelimitedFormat format)
        {
            string extension = Path.GetExtension(path ?? string.Empty).TrimStart('.');
            foreach (DelimitedFormat candidate in new[] { DelimitedFormat.Csv, DelimitedFormat.Tsv })
            {
                if (string.Equals(extension, TableExport.GetExtension(candidate), StringComparison.OrdinalIgnoreCase))
                {
                    format = candidate;
                    return true;
                }
            }

            format = DelimitedFormat.Csv;
            return false;
        }

        /// <summary>
        /// Reads a file to import. The format comes from its extension, and the encoding from its byte order mark (UTF-8 without it).
        /// </summary>
        /// <param name="path">The absolute path of the file, or its path from the project folder.</param>
        /// <param name="file">Outputs the read file, or null if it can't be read.</param>
        /// <param name="error">Outputs the reason why the file can't be read, or null if it has been read.</param>
        /// <returns>Returns true if the file has been read and has a header row.</returns>
        public static bool TryRead(string path, out ImportFile file, out string error)
        {
            file = null;
            if (!TryGetFormat(path, out DelimitedFormat format))
            {
                error = $"The file {path} is not a CSV or TSV file (.csv or .tsv).";
                return false;
            }

            string text;
            try
            {
                text = File.ReadAllText(path);
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
            {
                error = $"The file {path} can't be read: {exception.Message} If a spreadsheet app has it open, close it, then import " +
                    "again.";
                return false;
            }

            file = Parse(text, format, Path.GetFileName(path));
            if (file.Header.Count == 0)
            {
                file = null;
                error = $"The file {path} is empty.";
                return false;
            }

            error = null;
            return true;
        }

        /// <summary>
        /// Parses the text of a file to import.
        /// </summary>
        /// <param name="text">The text of the file.</param>
        /// <param name="format">The format of the file. For a CSV file, the delimiter is detected (see <see cref="DetectDelimiter"/>).
        /// </param>
        /// <param name="name"><inheritdoc cref="_name" path="/summary"/></param>
        /// <returns>Returns the parsed file, without header nor rows if the text is empty.</returns>
        public static ImportFile Parse(string text, DelimitedFormat format, string name = null)
        {
            char delimiter = DetectDelimiter(text, format);
            return new ImportFile(name, delimiter, DelimitedText.Parse(text, delimiter));
        }

        /// <summary>
        /// Detects the delimiter of a text: a tab for a TSV file, a comma or a semicolon for a CSV file, whichever the header row has
        /// more of (a comma if it has as many).
        /// </summary>
        /// <param name="text">The text of the file.</param>
        /// <param name="format">The format of the file.</param>
        /// <returns>Returns the delimiter of the text.</returns>
        public static char DetectDelimiter(string text, DelimitedFormat format)
        {
            if (format == DelimitedFormat.Tsv)
                return DelimitedText.Tab;

            if (string.IsNullOrEmpty(text))
                return DelimitedText.Comma;

            // Only the header row, outside of the quoted values, which can contain both
            int commaCount = 0;
            int semicolonCount = 0;
            bool isQuoted = false;
            foreach (char character in text)
            {
                if (character == Quote)
                    isQuoted = !isQuoted;
                else if (!isQuoted && (character == '\n' || character == '\r'))
                    break;
                else if (!isQuoted && character == DelimitedText.Comma)
                    commaCount++;
                else if (!isQuoted && character == Semicolon)
                    semicolonCount++;
            }

            return semicolonCount > commaCount ? Semicolon : DelimitedText.Comma;
        }

        #endregion


        #region Private API

        /// <summary>
        /// Checks if all the values of a row are blank.
        /// </summary>
        /// <param name="row">The values of the row.</param>
        /// <returns>Returns true if the row has no value, or only empty or white-space values.</returns>
        private static bool IsBlank(List<string> row)
        {
            foreach (string value in row)
            {
                if (!string.IsNullOrWhiteSpace(value))
                    return false;
            }

            return true;
        }

        #endregion

    }

}
