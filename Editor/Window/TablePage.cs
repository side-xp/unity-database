using System;

using UnityEngine.UIElements;
using UnityEditor.UIElements;

using Object = UnityEngine.Object;

namespace SideXP.Database.EditorOnly
{

    /// <summary>
    /// The page of the Database window that displays a table.
    /// </summary>
    internal class TablePage : VisualElement
    {

        #region Fields

        private const string PageClass = "database-page";
        private const string ContentClass = "database-page__content";
        private const string ToolbarClass = "database-toolbar";
        private const string TitleClass = "database-toolbar__title";
        private const string SpacerClass = "database-toolbar__spacer";
        private const string RowSearchClass = "database-toolbar__row-search";
        private const string CheckedButtonClass = "database-toolbar__button--checked";

        private const string SearchTooltip = "Shows the rows that contain each word of the search, in their asset name or in their " +
            "cells, ignoring case. Cells are searched as they are displayed: names for enums, paths for references.";

        private const string BackText = "Back";
        private const string BackTooltip = "Back to the table list.";
        private const string CreateText = "Create";
        private const string RefreshText = "Refresh";
        private const string RefreshTooltip = "Loads the assets of the table again. Use it if a change made outside of the window " +
            "doesn't show up, such as a value written by a script without notifying Unity.";
        private const string SyncSelectionText = "Sync Selection";
        private const string SyncSelectionTooltip = "When enabled, selecting cells selects the assets of their rows in Unity, so the " +
            "Inspector shows them (and edits several of them at once). The rows of the assets selected elsewhere are highlighted. " +
            "Disabled by default, since it changes the selection of the whole editor, which other windows react to.";
        private const string ViewPanelText = "View";
        private const string ViewPanelTooltip = "Shows or hides the panel of the view, on the right of the table, where the filters " +
            "are edited.";

        /// <summary>
        /// The displayed table, or null if none has been set.
        /// </summary>
        private TableDefinition _table = null;

        /// <summary>
        /// The label that displays the name of the table in the toolbar.
        /// </summary>
        private Label _titleLabel = null;

        /// <summary>
        /// The mark shown in the toolbar if the table is obsolete.
        /// </summary>
        private VisualElement _obsoleteMark = null;

        /// <summary>
        /// The field that filters the rows of the table.
        /// </summary>
        private ToolbarSearchField _searchField = null;

        /// <summary>
        /// The button that creates an asset of the table, hidden if the table has no type that can be created.
        /// </summary>
        private ToolbarButton _createButton = null;

        /// <summary>
        /// The button that enables and disables the selection sync (see <see cref="TableView.SetSelectionSynced(bool)"/>). A button
        /// rather than a toggle, so it shows an icon like the other buttons of the toolbar: a link while the sync is enabled, a broken
        /// link otherwise.
        /// </summary>
        private ToolbarButton _syncSelectionButton = null;

        /// <summary>
        /// The button that shows and hides the view panel, with the background of a checked toolbar button while the panel is open.
        /// </summary>
        private ToolbarButton _viewPanelButton = null;

        /// <summary>
        /// Displays the rows of the table.
        /// </summary>
        private TableView _tableView = null;

        /// <summary>
        /// The panel of the view, on the right of the table.
        /// </summary>
        private ViewPanel _viewPanel = null;

        /// <summary>
        /// Called when the user enables or disables the selection sync, with its new state.
        /// </summary>
        public event Action<bool> SelectionSyncChanged;

        /// <summary>
        /// Called when the user shows or hides the view panel, with its new state.
        /// </summary>
        public event Action<bool> ViewPanelToggled;

        /// <summary>
        /// Called when the user asks to go back to the table list.
        /// </summary>
        public event Action BackClicked;

        /// <summary>
        /// Called when the search of the table changes, typed by the user or cleared by the table view.
        /// </summary>
        public event Action<string> SearchChanged;

        #endregion


        #region Lifecycle

        /// <inheritdoc cref="TablePage"/>
        public TablePage()
        {
            AddToClassList(PageClass);

            Toolbar toolbar = new Toolbar();
            toolbar.AddToClassList(ToolbarClass);
            ToolbarButton backButton = new ToolbarButton(() => BackClicked?.Invoke()) { tooltip = BackTooltip };
            DatabaseElements.SetIcon(backButton, DatabaseIcons.Back, BackText);
            toolbar.Add(backButton);

            _titleLabel = new Label();
            _titleLabel.AddToClassList(TitleClass);
            toolbar.Add(_titleLabel);

            _obsoleteMark = DatabaseElements.CreateObsoleteMark();
            _obsoleteMark.SetDisplayed(false);
            toolbar.Add(_obsoleteMark);

            VisualElement spacer = new VisualElement();
            spacer.AddToClassList(SpacerClass);
            toolbar.Add(spacer);

            _tableView = new TableView();
            _tableView.SearchReset += OnSearchReset;

            _searchField = new ToolbarSearchField { tooltip = SearchTooltip };
            _searchField.AddToClassList(RowSearchClass);
            _searchField.RegisterValueChangedCallback(OnSearchFieldChanged);
            toolbar.Add(_searchField);

            // Its icon and tooltip depend on the types of the table: set when the table is set
            _createButton = new ToolbarButton();
            _createButton.clicked += () => _tableView.StartCreateAsset(_createButton);
            toolbar.Add(_createButton);

            ToolbarButton refreshButton = new ToolbarButton(_tableView.Reload) { tooltip = RefreshTooltip };
            DatabaseElements.SetIcon(refreshButton, DatabaseIcons.Refresh, RefreshText);
            toolbar.Add(refreshButton);

            _syncSelectionButton = new ToolbarButton(OnSyncSelectionClicked) { tooltip = SyncSelectionTooltip };
            UpdateSyncSelectionButton();
            toolbar.Add(_syncSelectionButton);

            _viewPanelButton = new ToolbarButton(OnViewPanelClicked) { tooltip = ViewPanelTooltip };
            DatabaseElements.SetIcon(_viewPanelButton, DatabaseIcons.ViewPanel, ViewPanelText);
            toolbar.Add(_viewPanelButton);
            Add(toolbar);

            // The panel is beside the table, so the status line of the table stays under the rows only
            VisualElement content = new VisualElement();
            content.AddToClassList(ContentClass);
            content.Add(_tableView);
            _viewPanel = new ViewPanel(_tableView);
            content.Add(_viewPanel);
            Add(content);
            SetViewPanelOpen(false);
        }

        #endregion


        #region Public API

        /// <inheritdoc cref="_table"/>
        public TableDefinition Table => _table;

        /// <summary>
        /// Sets the displayed table, and loads its assets.
        /// </summary>
        /// <param name="table">The table to display.</param>
        /// <param name="search">The search that filters the rows, or null to show all the rows.</param>
        public void SetTable(TableDefinition table, string search = null)
        {
            _table = table;
            _titleLabel.text = table.Name;
            _titleLabel.tooltip = table.Type.FullName;
            _obsoleteMark.SetDisplayed(table.IsObsolete);
            _searchField.SetValueWithoutNotify(search ?? string.Empty);
            _tableView.SetTable(table, search);
            _tableView.UpdateCreateButton(_createButton, CreateText);
            _viewPanel.Refresh();
        }

        /// <summary>
        /// Checks if the view panel is open.
        /// </summary>
        public bool IsViewPanelOpen => _viewPanel.IsDisplayed();

        /// <summary>
        /// Shows or hides the view panel, without notifying <see cref="ViewPanelToggled"/>. Used to restore the option.
        /// </summary>
        /// <param name="isOpen">If enabled, the panel is shown.</param>
        public void SetViewPanelOpen(bool isOpen)
        {
            _viewPanel.SetDisplayed(isOpen);
            _viewPanelButton.EnableInClassList(CheckedButtonClass, isOpen);
        }

        /// <inheritdoc cref="TableView.ShowAsset(Object)"/>
        public bool ShowAsset(Object asset)
        {
            return _tableView.ShowAsset(asset);
        }

        /// <summary>
        /// Removes the displayed table, so it stops tracking the changes of its assets while the page is hidden.
        /// </summary>
        public void ClearTable()
        {
            _table = null;
            _tableView.ClearTable();
        }

        /// <summary>
        /// Enables or disables the selection sync, without notifying <see cref="SelectionSyncChanged"/>. Used to restore the option.
        /// </summary>
        /// <param name="isSynced">If enabled, the selection of the table is synced with the selection of Unity.</param>
        public void SetSelectionSynced(bool isSynced)
        {
            _tableView.SetSelectionSynced(isSynced);
            UpdateSyncSelectionButton();
        }

        #endregion


        #region Selection sync

        /// <summary>
        /// Called when the user clicks the button of the selection sync. Enables the sync if it's disabled, and disables it otherwise.
        /// </summary>
        private void OnSyncSelectionClicked()
        {
            bool isSynced = !_tableView.IsSelectionSynced;
            _tableView.SetSelectionSynced(isSynced);
            UpdateSyncSelectionButton();
            SelectionSyncChanged?.Invoke(isSynced);
        }

        /// <summary>
        /// Shows the state of the selection sync on its button: its icon, and the background of a checked toolbar button while the
        /// sync is enabled.
        /// </summary>
        private void UpdateSyncSelectionButton()
        {
            bool isSynced = _tableView.IsSelectionSynced;
            string iconName = isSynced ? DatabaseIcons.SelectionSynced : DatabaseIcons.SelectionNotSynced;
            DatabaseElements.SetIcon(_syncSelectionButton, iconName, SyncSelectionText);
            _syncSelectionButton.EnableInClassList(CheckedButtonClass, isSynced);
        }

        #endregion


        #region View panel

        /// <summary>
        /// Called when the user clicks the button of the view panel. Opens the panel if it's closed, and closes it otherwise.
        /// </summary>
        private void OnViewPanelClicked()
        {
            bool isOpen = !IsViewPanelOpen;
            SetViewPanelOpen(isOpen);
            ViewPanelToggled?.Invoke(isOpen);
        }

        #endregion


        #region Search

        /// <summary>
        /// Called when the user changes the text of the search field. Filters the rows of the table.
        /// </summary>
        /// <param name="evt">The emitted event.</param>
        private void OnSearchFieldChanged(ChangeEvent<string> evt)
        {
            _tableView.SetSearch(evt.newValue);
            SearchChanged?.Invoke(evt.newValue);
        }

        /// <summary>
        /// Called when the table view clears the search itself, for example to show a new asset. Clears the search field.
        /// </summary>
        private void OnSearchReset()
        {
            _searchField.SetValueWithoutNotify(string.Empty);
            SearchChanged?.Invoke(string.Empty);
        }

        #endregion

    }

}
