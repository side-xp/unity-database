using System;
using System.Collections.Generic;

namespace SideXP.Database.EditorOnly
{

    /// <summary>
    /// The matching rules of the search field of a table: a row matches if each word of the search is found in its asset name or in
    /// the text of one of its cells, ignoring case.
    /// </summary>
    /// <remarks>
    /// The text of a row is built once from the text of its cells (see <see cref="BuildText(IEnumerable{string})"/>), and cached until
    /// its values are read again, so typing in the search field only compares texts.
    /// </remarks>
    internal static class RowSearch
    {

        #region Fields

        /// <summary>
        /// The character between the texts of the cells in the text of a row. A line break can't be typed in the search field, so a
        /// word never matches across two cells.
        /// </summary>
        private const char CellSeparator = '\n';

        /// <summary>
        /// The characters that separate the words of a search.
        /// </summary>
        private static readonly char[] WordSeparators = { ' ', '\t', '\n', '\r' };

        #endregion


        #region Public API

        /// <summary>
        /// Splits a search into words.
        /// </summary>
        /// <param name="search">The text of the search field.</param>
        /// <returns>Returns the words of the search, or an empty array if the search is null or blank (all the rows match).</returns>
        public static string[] SplitWords(string search)
        {
            return string.IsNullOrWhiteSpace(search)
                ? Array.Empty<string>()
                : search.Split(WordSeparators, StringSplitOptions.RemoveEmptyEntries);
        }

        /// <summary>
        /// Builds the text of a row, compared with the words of a search.
        /// </summary>
        /// <param name="cellTexts">The text of each cell of the row, and the asset name. Null texts count as empty texts.</param>
        /// <returns>Returns the text of the row.</returns>
        public static string BuildText(IEnumerable<string> cellTexts)
        {
            return string.Join(CellSeparator.ToString(), cellTexts);
        }

        /// <summary>
        /// Checks if the text of a row matches the words of a search.
        /// </summary>
        /// <param name="rowText">The text of the row (see <see cref="BuildText(IEnumerable{string})"/>).</param>
        /// <param name="words">The words of the search (see <see cref="SplitWords(string)"/>).</param>
        /// <returns>Returns true if each word is found in the text, ignoring case, or if there is no word.</returns>
        public static bool Matches(string rowText, IReadOnlyList<string> words)
        {
            if (words == null || words.Count == 0)
                return true;

            if (string.IsNullOrEmpty(rowText))
                return false;

            foreach (string word in words)
            {
                if (rowText.IndexOf(word, StringComparison.OrdinalIgnoreCase) < 0)
                    return false;
            }

            return true;
        }

        #endregion

    }

}
