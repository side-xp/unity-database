using System.Collections.Generic;
using System.Text;

namespace SideXP.Database.EditorOnly
{

    /// <summary>
    /// Writes and parses delimited text: rows of values separated by a delimiter, such as tab-separated values (the clipboard of
    /// spreadsheet apps) or comma-separated values.
    /// </summary>
    /// <remarks>
    /// Follows the rules of spreadsheet apps (RFC 4180): a value that contains the delimiter, a quote or a line break is enclosed in
    /// quotes, and its quotes are doubled. Spaces are kept as they are.
    /// </remarks>
    internal static class DelimitedText
    {

        #region Fields

        /// <summary>
        /// The delimiter of tab-separated values, used by the clipboard of spreadsheet apps.
        /// </summary>
        public const char Tab = '\t';

        /// <summary>
        /// The delimiter of comma-separated values.
        /// </summary>
        public const char Comma = ',';

        private const char Quote = '"';

        #endregion


        #region Public API

        /// <summary>
        /// Writes rows of values as delimited text.
        /// </summary>
        /// <param name="rows">The rows to write, each a list of values. A null value is written as an empty value.</param>
        /// <param name="delimiter">The character that separates the values of a row.</param>
        /// <param name="lineBreak">The text that separates the rows.</param>
        /// <returns>Returns the written text, without a line break after the last row.</returns>
        public static string Write(IReadOnlyList<IReadOnlyList<string>> rows, char delimiter, string lineBreak = "\n")
        {
            StringBuilder builder = new StringBuilder();
            for (int rowIndex = 0; rowIndex < rows.Count; rowIndex++)
            {
                if (rowIndex > 0)
                    builder.Append(lineBreak);

                IReadOnlyList<string> row = rows[rowIndex];
                for (int i = 0; i < row.Count; i++)
                {
                    if (i > 0)
                        builder.Append(delimiter);

                    AppendValue(builder, row[i] ?? string.Empty, delimiter);
                }
            }

            return builder.ToString();
        }

        /// <summary>
        /// Parses delimited text into rows of values.
        /// </summary>
        /// <remarks>
        /// Line breaks can be <c>\r\n</c>, <c>\n</c> or <c>\r</c>. A line break at the end of the text doesn't start an empty row,
        /// since spreadsheet apps add one after the last row they copy. Quoted values can contain delimiters, quotes (doubled) and line
        /// breaks. A quote inside a value that doesn't start with one is kept as it is.
        /// </remarks>
        /// <param name="text">The text to parse.</param>
        /// <param name="delimiter">The character that separates the values of a row.</param>
        /// <returns>Returns the parsed rows. Empty if the text is null or empty.</returns>
        public static List<List<string>> Parse(string text, char delimiter)
        {
            List<List<string>> rows = new List<List<string>>();
            if (string.IsNullOrEmpty(text))
                return rows;

            List<string> row = new List<string>();
            StringBuilder value = new StringBuilder();
            bool isQuoted = false;
            bool isValueStart = true;
            int index = 0;
            while (index < text.Length)
            {
                char character = text[index];
                if (isQuoted)
                {
                    if (character == Quote)
                    {
                        // A doubled quote is a quote of the value, a single one ends the quoted part
                        if (index + 1 < text.Length && text[index + 1] == Quote)
                        {
                            value.Append(Quote);
                            index += 2;
                            continue;
                        }

                        isQuoted = false;
                    }
                    else
                    {
                        value.Append(character);
                    }

                    index++;
                    continue;
                }

                if (character == Quote && isValueStart)
                {
                    isQuoted = true;
                    isValueStart = false;
                    index++;
                    continue;
                }

                if (character == delimiter)
                {
                    row.Add(value.ToString());
                    value.Clear();
                    isValueStart = true;
                    index++;
                    continue;
                }

                if (character == '\r' || character == '\n')
                {
                    row.Add(value.ToString());
                    value.Clear();
                    rows.Add(row);
                    row = new List<string>();
                    isValueStart = true;

                    // \r\n is a single line break
                    index += character == '\r' && index + 1 < text.Length && text[index + 1] == '\n' ? 2 : 1;
                    continue;
                }

                value.Append(character);
                isValueStart = false;
                index++;
            }

            // The last row, unless the text ends with a line break
            if (!isValueStart || row.Count > 0 || value.Length > 0)
            {
                row.Add(value.ToString());
                rows.Add(row);
            }

            return rows;
        }

        #endregion


        #region Private API

        /// <summary>
        /// Appends a value, enclosed in quotes if it contains the delimiter, a quote or a line break.
        /// </summary>
        /// <param name="builder">The text being written.</param>
        /// <param name="value">The value to append.</param>
        /// <param name="delimiter">The character that separates the values of a row.</param>
        private static void AppendValue(StringBuilder builder, string value, char delimiter)
        {
            bool isQuoted = value.IndexOf(delimiter) >= 0
                || value.IndexOf(Quote) >= 0
                || value.IndexOf('\n') >= 0
                || value.IndexOf('\r') >= 0;

            if (!isQuoted)
            {
                builder.Append(value);
                return;
            }

            builder.Append(Quote);
            builder.Append(value.Replace("\"", "\"\""));
            builder.Append(Quote);
        }

        #endregion

    }

}
