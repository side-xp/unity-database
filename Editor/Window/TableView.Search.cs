using System;
using System.Collections.Generic;

using UnityEngine.UIElements;

using Object = UnityEngine.Object;

namespace SideXP.Database.EditorOnly
{

    /// <summary>
    /// The search of the table view: filters the rows on their asset name and on the text of their cells (see <see cref="RowSearch"/>).
    /// It's combined with the filters of the view (see <see cref="ApplyRowFilter"/>).
    /// </summary>
    /// <remarks>
    /// The text of a cell comes from the text conversion of its handler (<see cref="CellHandler.ToText(object)"/>), so the search finds
    /// what the cells display: names of enums, paths of references, numbers in the invariant culture. The <i>Controls</i> column has
    /// no text. The hidden columns are not searched, except the asset name, which is always searched.
    /// </remarks>
    internal partial class TableView
    {

        #region Fields

        private const string NoSearchMatchMessage = "No row matches the search.";

        /// <summary>
        /// The label shown by the main list when the search or the filters hide all the rows, or null if the list hasn't created it.
        /// Its text is updated on each refresh (see <see cref="GetNoMatchMessage"/>).
        /// </summary>
        private Label _noMatchLabel = null;

        /// <summary>
        /// The text of the search, or an empty string if there is none.
        /// </summary>
        private string _search = string.Empty;

        /// <summary>
        /// The words of <see cref="_search"/>, each of which a row must contain to be visible.
        /// </summary>
        private string[] _searchWords = Array.Empty<string>();

        /// <summary>
        /// The texts of the cells of a row, reused to build the text of each row.
        /// </summary>
        private List<string> _cellTexts = new List<string>();

        /// <summary>
        /// Called when the view clears the search itself, so the search field can be cleared too. Not called when the search is set
        /// with <see cref="SetSearch(string)"/>.
        /// </summary>
        public event Action SearchReset;

        #endregion


        #region Public API

        /// <inheritdoc cref="_search"/>
        public string Search => _search;

        /// <summary>
        /// Checks if the search hides rows: true if it has at least one word.
        /// </summary>
        public bool IsSearching => _searchWords.Length > 0;

        /// <summary>
        /// Shows only the rows that contain each word of a search, in their asset name or in the text of their cells, ignoring case.
        /// </summary>
        /// <remarks>
        /// The active cell is cleared if its row is hidden, and the first page is shown. Setting another table replaces the search with
        /// the one given to <see cref="SetTable(TableDefinition, string, int)"/>.
        /// </remarks>
        /// <param name="search">The text of the search. If null or blank, all the rows are visible.</param>
        public void SetSearch(string search)
        {
            StoreSearch(search);
            if (_pipeline == null)
                return;

            ResetPage();
            ApplyRowFilter();
            Refresh();
        }

        #endregion


        #region Private API

        /// <summary>
        /// Stores the text and the words of a search, without filtering the rows.
        /// </summary>
        /// <param name="search">The text of the search.</param>
        private void StoreSearch(string search)
        {
            _search = search ?? string.Empty;
            _searchWords = RowSearch.SplitWords(_search);
        }

        /// <summary>
        /// Checks if a row matches the search.
        /// </summary>
        /// <param name="row">The row to check.</param>
        /// <returns>Returns true if the text of the row contains each word of the search.</returns>
        private bool MatchesSearch(TableRow row)
        {
            return RowSearch.Matches(GetSearchText(row), _searchWords);
        }

        /// <summary>
        /// Gets the text of a row compared with the search, and builds it if the values of the row have been read since.
        /// </summary>
        /// <remarks>
        /// The text contains the displayed columns only, so every match can be seen on screen, and the asset name, which is searched
        /// even if its column is hidden. It must be built again when the displayed columns change (see <see cref="ClearSearchTexts"/>).
        /// </remarks>
        /// <param name="row">The row of which to get the text.</param>
        /// <returns>Returns the text of the row.</returns>
        private string GetSearchText(TableRow row)
        {
            if (row.SearchText != null)
                return row.SearchText;

            _cellTexts.Clear();
            for (int i = 0; i < _columns.Count; i++)
            {
                TableViewColumn column = _columns[i];
                if (column.IsControls || (!column.IsAssetName && !IsColumnDisplayed(i)))
                    continue;

                _cellTexts.Add(ToSearchText(column, row.GetValue(i)));
            }

            row.SearchText = RowSearch.BuildText(_cellTexts);
            return row.SearchText;
        }

        /// <summary>
        /// Clears the text of all the rows compared with the search, so they are built again with the displayed columns.
        /// </summary>
        private void ClearSearchTexts()
        {
            foreach (TableRow row in _pipeline.Rows)
                row.SearchText = null;
        }

        /// <summary>
        /// Converts the value of a cell to text through the handler of its column.
        /// </summary>
        /// <remarks>
        /// An error of the handler is logged once per column (see <see cref="ToCellText"/>), and the cell is then searched as an empty
        /// text, so a broken custom handler doesn't break the search.
        /// </remarks>
        /// <param name="column">The column of the cell.</param>
        /// <param name="value">The cached value of the cell.</param>
        /// <returns>Returns the text of the cell, or null if the handler failed.</returns>
        private static string ToSearchText(TableViewColumn column, object value)
        {
            return ToCellText(column, value, "for the search");
        }

        /// <summary>
        /// Checks if an asset has a visible row: in the table, and not hidden by the search.
        /// </summary>
        /// <param name="asset">The asset to check.</param>
        /// <returns>Returns true if the asset has a visible row.</returns>
        private bool IsAssetVisible(Object asset)
        {
            return asset != null && _pipeline != null && _pipeline.TryGetRow(asset, out TableRow row) && _rows.Contains(row);
        }

        /// <summary>
        /// Clears the search if it hides the row of an asset, so a new asset is always visible, and notifies it with
        /// <see cref="SearchReset"/>. Call it only when the filters don't hide the row, since the search would be cleared for nothing.
        /// </summary>
        /// <param name="asset">The asset that must be visible.</param>
        private void ResetSearchIfHidden(Object asset)
        {
            if (!IsSearching || IsAssetVisible(asset))
                return;

            SetSearch(string.Empty);
            SearchReset?.Invoke();
        }

        /// <summary>
        /// Creates the element shown by the main list when it has no row, which only happens when the search or the filters hide all
        /// the rows: a table without asset shows its own message instead of the lists.
        /// </summary>
        /// <returns>Returns the created element.</returns>
        private VisualElement MakeNoMatchElement()
        {
            _noMatchLabel = new Label(GetNoMatchMessage());
            _noMatchLabel.AddToClassList(MessageClass);
            return _noMatchLabel;
        }

        #endregion

    }

}
