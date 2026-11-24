using NUnit.Framework;

using SideXP.Database.EditorOnly;

namespace SideXP.Database.Tests
{

    /// <summary>
    /// Tests for the matching rules of the search of the tables (<see cref="RowSearch"/>).
    /// </summary>
    public class RowSearchTests
    {

        #region Words

        /// <summary>
        /// A search is split into words on spaces, tabs and line breaks, without empty words.
        /// </summary>
        [Test]
        public void SplitWords_SeveralSpaces_NoEmptyWords()
        {
            CollectionAssert.AreEqual(new[] { "long", "sword", "10" }, RowSearch.SplitWords("  long \tsword   10 "));
        }

        /// <summary>
        /// A null or blank search has no word.
        /// </summary>
        /// <param name="search">The tested search.</param>
        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void SplitWords_Blank_NoWord(string search)
        {
            CollectionAssert.IsEmpty(RowSearch.SplitWords(search));
        }

        #endregion


        #region Matching

        /// <summary>
        /// A row matches if each word is found in its text, in any cell and in any order, ignoring case.
        /// </summary>
        [Test]
        public void Matches_AllWordsInAnyCell_True()
        {
            string rowText = RowSearch.BuildText(new[] { "Long Sword", "Legendary", "120" });
            Assert.IsTrue(RowSearch.Matches(rowText, RowSearch.SplitWords("legendary SWORD 12")));
        }

        /// <summary>
        /// A row doesn't match if one of the words is missing.
        /// </summary>
        [Test]
        public void Matches_OneWordMissing_False()
        {
            string rowText = RowSearch.BuildText(new[] { "Long Sword", "Legendary", "120" });
            Assert.IsFalse(RowSearch.Matches(rowText, RowSearch.SplitWords("sword common")));
        }

        /// <summary>
        /// A word never matches across two cells.
        /// </summary>
        [Test]
        public void Matches_WordAcrossTwoCells_False()
        {
            string rowText = RowSearch.BuildText(new[] { "Sword", "Legendary" });
            Assert.IsFalse(RowSearch.Matches(rowText, RowSearch.SplitWords("swordlegendary")));
        }

        /// <summary>
        /// Every row matches a search without word, even a row without text.
        /// </summary>
        [Test]
        public void Matches_NoWord_True()
        {
            Assert.IsTrue(RowSearch.Matches("Sword", RowSearch.SplitWords(string.Empty)));
            Assert.IsTrue(RowSearch.Matches(null, null));
        }

        /// <summary>
        /// A row without text matches no word, and null cell texts count as empty texts.
        /// </summary>
        [Test]
        public void Matches_EmptyText_False()
        {
            Assert.IsFalse(RowSearch.Matches(null, RowSearch.SplitWords("sword")));
            Assert.IsFalse(RowSearch.Matches(RowSearch.BuildText(new string[] { null, null }), RowSearch.SplitWords("sword")));
        }

        #endregion

    }

}
