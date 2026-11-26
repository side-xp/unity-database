using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UIElements;

namespace SideXP.Database.EditorOnly
{

    /// <summary>
    /// The pagination of the table view: the visible rows are displayed in pages of a chosen size, with the page controls and the page
    /// size in the status line.
    /// </summary>
    /// <remarks>
    /// The pages only change what the list views display (see <see cref="TablePipeline.PageRows"/>). The selection, the copy, the
    /// edits of several cells and the active cell use all the visible rows, so a range can cover rows of several pages, and the row
    /// numbers continue across the pages.
    /// <para>
    /// The page size is remembered with the settings of the table (see <see cref="ViewSettings.PageSize"/>), and is
    /// <see cref="DefaultPageSize"/> until the user chooses one. The page goes back to the first one when the search, the filters, the
    /// sort or the folder scope change, and is kept otherwise (reloads and edits), in the range of the pages. Showing an asset opens
    /// the page of its row (see <see cref="ShowAsset"/>).
    /// </para>
    /// </remarks>
    internal partial class TableView
    {

        #region Fields

        private const string StatusSpacerClass = "database-status-bar__spacer";
        private const string PageControlsClass = "database-status-bar__pages";
        private const string PageButtonClass = "database-status-bar__page-button";
        private const string PageTextClass = "database-status-bar__page-text";
        private const string PageFieldClass = "database-status-bar__page-field";
        private const string PageSizeClass = "database-status-bar__page-size";

        private const string FirstPageTooltip = "Shows the first page of the rows.";
        private const string PreviousPageTooltip = "Shows the previous page of the rows.";
        private const string NextPageTooltip = "Shows the next page of the rows.";
        private const string LastPageTooltip = "Shows the last page of the rows.";
        private const string PageText = "Page";
        private const string PageFieldTooltip = "The displayed page. Type the number of a page, then press Enter.";
        private const string PageSizeTooltip = "The number of rows per page. Type a number, then press Enter. The selection, the copy " +
            "and the edits of several cells include the rows of all the pages. Remembered with the settings of the table.";
        private const string PageSizeText = "per page";

        /// <summary>
        /// The smallest page size the user can choose. Smaller pages would only make the user browse more pages.
        /// </summary>
        private const int MinPageSize = 10;

        /// <summary>
        /// The page size of a table whose settings have none yet: a few screens of rows, so the list stays short to scroll.
        /// </summary>
        private const int DefaultPageSize = 100;

        /// <summary>
        /// The visible rows of the displayed page, copied from the pipeline after each update, used as the source of the list views.
        /// </summary>
        private List<TableRow> _pageRows = new List<TableRow>();

        /// <summary>
        /// The element that holds the page controls.
        /// </summary>
        private VisualElement _pageControls = null;

        /// <summary>
        /// The button that shows the first page.
        /// </summary>
        private Button _firstPageButton = null;

        /// <summary>
        /// The button that shows the previous page.
        /// </summary>
        private Button _previousPageButton = null;

        /// <summary>
        /// The field that shows the number of the displayed page, from 1, and where the user can type the page to show.
        /// </summary>
        private IntegerField _pageField = null;

        /// <summary>
        /// The label that shows the number of pages, after <see cref="_pageField"/>.
        /// </summary>
        private Label _pageCountLabel = null;

        /// <summary>
        /// The button that shows the next page.
        /// </summary>
        private Button _nextPageButton = null;

        /// <summary>
        /// The button that shows the last page.
        /// </summary>
        private Button _lastPageButton = null;

        /// <summary>
        /// The field where the user types the page size.
        /// </summary>
        private IntegerField _pageSizeField = null;

        /// <summary>
        /// The page index last notified with <see cref="PageChanged"/>, or given to <see cref="SetTable"/>.
        /// </summary>
        private int _notifiedPageIndex = 0;

        /// <summary>
        /// Called when the displayed page changes, with its index from 0: chosen by the user, or changed by the view itself (back to
        /// the first page after a new search, kept in range after the rows changed, the page of a shown asset). Used to remember the
        /// page across domain reloads, like the search.
        /// </summary>
        public event Action<int> PageChanged;

        #endregion


        #region Public API

        /// <summary>
        /// Gets the index of the displayed page, from 0.
        /// </summary>
        public int PageIndex => _pipeline != null ? _pipeline.PageIndex : 0;

        /// <summary>
        /// Shows a page of the rows, and scrolls the table to its first row.
        /// </summary>
        /// <param name="pageIndex">The index of the page, from 0. An index out of the range of the pages shows the first or the last
        /// page.</param>
        public void SetPage(int pageIndex)
        {
            if (_pipeline == null)
                return;

            // The cell elements are bound to the rows of the other page
            EndEdit(true, false);
            _pipeline.PageIndex = pageIndex;
            Refresh();
            ScrollToTop();
        }

        /// <summary>
        /// Displays the rows in pages of a given size, and remembers it with the settings of the table. The page that holds the first
        /// row of the displayed page is shown, so the user doesn't lose their place.
        /// </summary>
        /// <param name="pageSize">The number of rows per page. A size under <see cref="MinPageSize"/> (including 0 and negative sizes)
        /// is raised to it.</param>
        public void SetPageSize(int pageSize)
        {
            if (_pipeline == null)
                return;

            pageSize = Math.Max(MinPageSize, pageSize);
            // @todo After the views: the page size becomes a user setting of the whole package, instead of a field of the open view
            if (_viewSettings.Top.SetPageSize(pageSize))
                UserViews.instance.ScheduleSave();

            if (pageSize == _pipeline.PageSize)
                return;

            EndEdit(true, false);
            int firstRowIndex = _pipeline.PageStartIndex;
            _pipeline.PageSize = pageSize;
            _pipeline.PageIndex = firstRowIndex / pageSize;
            Refresh();
            ScrollToTop();
        }

        #endregion


        #region UI

        /// <summary>
        /// Creates the page controls and the page size field, at the end of the status line.
        /// </summary>
        /// <param name="statusBar">The status line, which already holds its label.</param>
        private void CreatePageControls(VisualElement statusBar)
        {
            VisualElement spacer = new VisualElement();
            spacer.AddToClassList(StatusSpacerClass);
            statusBar.Add(spacer);

            _pageControls = new VisualElement();
            _pageControls.AddToClassList(PageControlsClass);

            _firstPageButton = CreatePageButton(DatabaseIcons.FirstPage, FirstPageTooltip, () => SetPage(0));
            _previousPageButton = CreatePageButton(DatabaseIcons.PreviousPage, PreviousPageTooltip, () => SetPage(PageIndex - 1));

            Label pageLabel = new Label(PageText);
            pageLabel.AddToClassList(PageTextClass);
            _pageControls.Add(pageLabel);

            // Delayed, so the page changes when the user confirms the number, not on each digit
            _pageField = new IntegerField { isDelayed = true, tooltip = PageFieldTooltip };
            _pageField.AddToClassList(PageFieldClass);
            _pageField.RegisterValueChangedCallback(evt => SetPage(evt.newValue - 1));
            _pageControls.Add(_pageField);

            _pageCountLabel = new Label();
            _pageCountLabel.AddToClassList(PageTextClass);
            _pageControls.Add(_pageCountLabel);

            _nextPageButton = CreatePageButton(DatabaseIcons.NextPage, NextPageTooltip, () => SetPage(PageIndex + 1));
            // The last page is brought back in range by the pipeline
            _lastPageButton = CreatePageButton(DatabaseIcons.LastPage, LastPageTooltip, () => SetPage(int.MaxValue));
            statusBar.Add(_pageControls);

            // Delayed, so a size is applied when the user confirms it, not on each digit
            _pageSizeField = new IntegerField { isDelayed = true, tooltip = PageSizeTooltip };
            _pageSizeField.AddToClassList(PageSizeClass);
            _pageSizeField.RegisterValueChangedCallback(OnPageSizeTyped);
            statusBar.Add(_pageSizeField);

            Label pageSizeLabel = new Label(PageSizeText);
            pageSizeLabel.AddToClassList(PageTextClass);
            statusBar.Add(pageSizeLabel);
        }

        /// <summary>
        /// Creates a button of the page controls, with an icon, and adds it to the controls.
        /// </summary>
        /// <param name="iconName">The name of the icon of the button, one of the constants of <see cref="DatabaseIcons"/>.</param>
        /// <param name="tooltip">The tooltip of the button, which says what it does.</param>
        /// <param name="clicked">Called when the button is clicked.</param>
        /// <returns>Returns the created button.</returns>
        private Button CreatePageButton(string iconName, string tooltip, Action clicked)
        {
            Button button = new Button(clicked) { tooltip = tooltip };
            DatabaseElements.SetIcon(button, iconName);
            button.AddToClassList(PageButtonClass);
            _pageControls.Add(button);
            return button;
        }

        /// <summary>
        /// Shows the state of the pages in the page controls and the page size field: the displayed page, the number of pages, and the
        /// buttons that can't change the page disabled. Notifies <see cref="PageChanged"/> if the page has changed since the last
        /// notification.
        /// </summary>
        private void UpdatePageControls()
        {
            if (_pipeline == null)
                return;

            int pageIndex = _pipeline.PageIndex;
            int pageCount = _pipeline.PageCount;
            _pageField.SetValueWithoutNotify(pageIndex + 1);
            _pageCountLabel.text = $"of {pageCount}";
            // Also shows the applied size after the user typed a size under the minimum
            _pageSizeField.SetValueWithoutNotify(_pipeline.PageSize);

            bool hasPreviousPage = pageIndex > 0;
            bool hasNextPage = pageIndex < pageCount - 1;
            _firstPageButton.SetEnabled(hasPreviousPage);
            _previousPageButton.SetEnabled(hasPreviousPage);
            _nextPageButton.SetEnabled(hasNextPage);
            _lastPageButton.SetEnabled(hasNextPage);
            _pageField.SetEnabled(pageCount > 1);

            if (pageIndex != _notifiedPageIndex)
            {
                _notifiedPageIndex = pageIndex;
                PageChanged?.Invoke(pageIndex);
            }
        }

        #endregion


        #region Private API

        /// <summary>
        /// Gives the remembered page size and a page to the pipeline of a table that has just been set, before its first update.
        /// </summary>
        /// <remarks>
        /// The remembered size is not saved again: without size, the table gets <see cref="DefaultPageSize"/>, and a size under the
        /// minimum (from a file edited by hand) is only raised for the display.
        /// </remarks>
        /// <param name="pageIndex">The index of the page to show, from 0, brought back in the range of the pages by the update.</param>
        private void ApplyRememberedPagination(int pageIndex)
        {
            int pageSize = _viewSettings.Top.PageSize;
            _pipeline.PageSize = pageSize > 0 ? Math.Max(MinPageSize, pageSize) : DefaultPageSize;
            _pipeline.PageIndex = pageIndex;
            // The owner of the view already knows this page
            _notifiedPageIndex = pageIndex;
        }

        /// <summary>
        /// Shows the first page on the next update, after the rows have changed in a way that makes the displayed page meaningless (a
        /// new search, other filters, another sort or another folder scope).
        /// </summary>
        private void ResetPage()
        {
            if (_pipeline != null)
                _pipeline.PageIndex = 0;
        }

        /// <summary>
        /// Shows the page that displays a row, if it's not the displayed page.
        /// </summary>
        /// <param name="row">The row to show, which must be visible.</param>
        private void ShowPageOf(TableRow row)
        {
            int pageIndex = _pipeline.GetPageOf(row);
            if (pageIndex < 0 || pageIndex == _pipeline.PageIndex)
                return;

            EndEdit(true, false);
            _pipeline.PageIndex = pageIndex;
            Refresh();
        }

        /// <summary>
        /// Scrolls the main list to its first row, keeping its horizontal scroll. The frozen list follows it.
        /// </summary>
        private void ScrollToTop()
        {
            ScrollView scrollView = _listView?.Q<ScrollView>();
            if (scrollView != null)
                scrollView.scrollOffset = new Vector2(scrollView.scrollOffset.x, 0);
        }

        /// <summary>
        /// Called when the user confirms a page size in the field of the status line. Applies it, raised to the minimum.
        /// </summary>
        /// <param name="evt">The emitted event.</param>
        private void OnPageSizeTyped(ChangeEvent<int> evt)
        {
            SetPageSize(evt.newValue);
            // The size may be unchanged (the same size, or a size under the minimum while the minimum is applied), so the view isn't
            // refreshed: the field must show the applied size itself
            UpdatePageControls();
        }

        #endregion

    }

}
