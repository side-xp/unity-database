using System.Collections.Generic;

using NUnit.Framework;

using SideXP.Database.EditorOnly;

namespace SideXP.Database.Tests
{

    /// <summary>
    /// Tests for <see cref="DelimitedText"/>: writing and parsing tab-separated and comma-separated values.
    /// </summary>
    public class DelimitedTextTests
    {

        #region Tests

        /// <summary>
        /// Rows are separated by line breaks and values by the delimiter, without a line break after the last row.
        /// </summary>
        [Test]
        public void Write_Rows_DelimitedText()
        {
            List<List<string>> rows = new List<List<string>>
            {
                new List<string> { "Sword", "12" },
                new List<string> { "Shield", null },
            };

            Assert.AreEqual("Sword\t12\nShield\t", DelimitedText.Write(rows, DelimitedText.Tab));
            Assert.AreEqual("Sword,12\r\nShield,", DelimitedText.Write(rows, DelimitedText.Comma, "\r\n"));
        }

        /// <summary>
        /// The values that contain the delimiter, a quote or a line break are quoted, with their quotes doubled.
        /// </summary>
        [Test]
        public void Write_SpecialCharacters_Quoted()
        {
            List<List<string>> rows = new List<List<string>>
            {
                new List<string> { "a\tb", "say \"hi\"", "two\nlines", "plain, text" },
            };

            Assert.AreEqual("\"a\tb\"\t\"say \"\"hi\"\"\"\t\"two\nlines\"\tplain, text", DelimitedText.Write(rows, DelimitedText.Tab));
        }

        /// <summary>
        /// The three kinds of line breaks separate rows, and a line break at the end doesn't start an empty row.
        /// </summary>
        [Test]
        public void Parse_LineBreaks_Rows()
        {
            List<List<string>> rows = DelimitedText.Parse("a\tb\r\nc\td\ne\tf\rg\th\r\n", DelimitedText.Tab);

            Assert.AreEqual(4, rows.Count);
            CollectionAssert.AreEqual(new[] { "a", "b" }, rows[0]);
            CollectionAssert.AreEqual(new[] { "g", "h" }, rows[3]);
        }

        /// <summary>
        /// Quoted values keep their delimiters, quotes and line breaks.
        /// </summary>
        [Test]
        public void Parse_QuotedValues_Unquoted()
        {
            List<List<string>> rows = DelimitedText.Parse("\"a\tb\"\t\"say \"\"hi\"\"\"\t\"two\nlines\"", DelimitedText.Tab);

            Assert.AreEqual(1, rows.Count);
            CollectionAssert.AreEqual(new[] { "a\tb", "say \"hi\"", "two\nlines" }, rows[0]);
        }

        /// <summary>
        /// Empty values are kept: between delimiters, at the end of a row, and a single empty cell copied by a spreadsheet app.
        /// </summary>
        [Test]
        public void Parse_EmptyValues_Kept()
        {
            CollectionAssert.AreEqual(new[] { "a", string.Empty, "c", string.Empty },
                DelimitedText.Parse("a\t\tc\t", DelimitedText.Tab)[0]);

            List<List<string>> emptyCell = DelimitedText.Parse("\r\n", DelimitedText.Tab);
            Assert.AreEqual(1, emptyCell.Count);
            CollectionAssert.AreEqual(new[] { string.Empty }, emptyCell[0]);

            Assert.AreEqual(0, DelimitedText.Parse(string.Empty, DelimitedText.Tab).Count);
        }

        /// <summary>
        /// A quote inside a value that doesn't start with one is kept as it is, and spaces are kept.
        /// </summary>
        [Test]
        public void Parse_UnquotedQuoteAndSpaces_Kept()
        {
            CollectionAssert.AreEqual(new[] { "12\" blade", " padded " },
                DelimitedText.Parse("12\" blade\t padded ", DelimitedText.Tab)[0]);
        }

        /// <summary>
        /// A written text is parsed back to the same values.
        /// </summary>
        [Test]
        public void WriteThenParse_SpecialCharacters_SameValues()
        {
            List<List<string>> rows = new List<List<string>>
            {
                new List<string> { "a,b", "\"quoted\"", "line\r\nbreak" },
                new List<string> { string.Empty, "x", " y " },
            };

            List<List<string>> parsed = DelimitedText.Parse(DelimitedText.Write(rows, DelimitedText.Comma), DelimitedText.Comma);

            Assert.AreEqual(rows.Count, parsed.Count);
            for (int i = 0; i < rows.Count; i++)
                CollectionAssert.AreEqual(rows[i], parsed[i]);
        }

        #endregion

    }

}
