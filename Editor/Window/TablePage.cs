using System;

using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.UIElements;

using SideXP.Core.EditorOnly;

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
        private const string ViewButtonClass = "database-toolbar__view";
        private const string ViewMenuArrowClass = "database-toolbar__view-arrow";

        /// <summary>
        /// The separator of the submenus in the items of a <see cref="GenericMenu"/>.
        /// </summary>
        private const char MenuPathSeparator = '/';

        /// <summary>
        /// Shown instead of <see cref="MenuPathSeparator"/> in the names of the views listed in the menu of the views, so a view named
        /// with a slash doesn't make a submenu.
        /// </summary>
        private const char MenuSlashReplacement = '∕';

        private const string ViewTooltip = "The open view of the table: the order, widths and visibility of the columns, the sort, the " +
            "filters and the folders. Click to switch to another view, or to create, duplicate, rename, delete or reset a view. The " +
            "views are only visible to you.";
        private const string NewViewText = "New View";
        private const string DuplicateViewText = "Duplicate View";
        private const string RenameViewText = "Rename View...";
        private const string DeleteViewText = "Delete View";
        private const string DeleteViewTitle = "Delete View";
        private const string DeleteText = "Delete";
        private const string CancelText = "Cancel";

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
            "and the folders of the table are edited.";

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
        /// The button that shows the name of the open view, and opens the menu of the views.
        /// </summary>
        private ToolbarButton _viewButton = null;

        /// <summary>
        /// The arrow after the name of the open view, kept after the label of the button.
        /// </summary>
        private Icon _viewMenuArrow = null;

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

        /// <inheritdoc cref="TableView.PageChanged"/>
        public event Action<int> PageChanged;

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
            _tableView.PageChanged += pageIndex => PageChanged?.Invoke(pageIndex);

            _searchField = new ToolbarSearchField { tooltip = SearchTooltip };
            _searchField.AddToClassList(RowSearchClass);
            _searchField.RegisterValueChangedCallback(OnSearchFieldChanged);
            toolbar.Add(_searchField);

            // The name of the view is set with the table. The icon is created first, so the arrow isn't the icon of the button
            _viewButton = new ToolbarButton(ShowViewMenu) { tooltip = ViewTooltip };
            _viewButton.AddToClassList(ViewButtonClass);
            DatabaseElements.SetIcon(_viewButton, DatabaseIcons.View);
            _viewMenuArrow = new Icon(DatabaseIcons.MenuArrow) { pickingMode = PickingMode.Ignore };
            _viewMenuArrow.AddToClassList(ViewMenuArrowClass);
            _viewButton.Add(_viewMenuArrow);
            toolbar.Add(_viewButton);

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
            _viewPanel.ViewRenamed += OnViewRenamed;
            content.Add(_viewPanel);
            Add(content);
            SetViewPanelOpen(false);
            _tableView.FilterByColumnRequested += OnFilterByColumnRequested;
        }

        #endregion


        #region Public API

        /// <inheritdoc cref="_table"/>
        public TableDefinition Table => _table;

        /// <summary>
        /// Sets the displayed table, and loads its assets, with the open view of the table (see <see cref="PersonalViews.GetOpenView"/>).
        /// </summary>
        /// <param name="table">The table to display.</param>
        /// <param name="search">The search that filters the rows, or null to show all the rows.</param>
        /// <param name="pageIndex">The page to show, from 0.</param>
        public void SetTable(TableDefinition table, string search = null, int pageIndex = 0)
        {
            _table = table;
            _titleLabel.text = table.Name;
            _titleLabel.tooltip = table.Type.FullName;
            _obsoleteMark.SetDisplayed(table.IsObsolete);
            _searchField.SetValueWithoutNotify(search ?? string.Empty);
            _tableView.SetTable(table, search, pageIndex);
            _tableView.UpdateCreateButton(_createButton, CreateText);
            _viewPanel.Refresh();
            UpdateViewControls();
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


        #region Views

        /// <summary>
        /// The personal views of the tables.
        /// </summary>
        private static PersonalViews Views => UserViews.instance.PersonalViews;

        /// <summary>
        /// Gets the open view of the displayed table.
        /// </summary>
        /// <returns>Returns the open view, or null if no table is displayed.</returns>
        private PersonalView GetOpenView()
        {
            return _table != null ? Views.GetOpenView(_table.Id) : null;
        }

        /// <summary>
        /// Shows the name of the open view on the button of the views and in the view panel.
        /// </summary>
        private void UpdateViewControls()
        {
            PersonalView view = GetOpenView();
            if (view == null)
                return;

            DatabaseElements.SetIcon(_viewButton, DatabaseIcons.View, view.Name);
            // The label is added at the end of the button the first time it's set
            _viewMenuArrow.BringToFront();
            _viewPanel.SetViewName(view.Name);
        }

        /// <summary>
        /// Called when the button of the views is clicked. Opens the menu of the views under it: the views of the table (the open one
        /// checked), then the operations on the open view.
        /// </summary>
        private void ShowViewMenu()
        {
            PersonalView openView = GetOpenView();
            if (openView == null)
                return;

            GenericMenu menu = new GenericMenu();
            foreach (PersonalView view in Views.GetViews(_table.Id))
            {
                string text = view.Name.Replace(MenuPathSeparator, MenuSlashReplacement);
                menu.AddItem(new GUIContent(text), view == openView, () => OpenView(view));
            }

            menu.AddSeparator(string.Empty);
            menu.AddItem(new GUIContent(NewViewText), false, CreateView);
            menu.AddItem(new GUIContent(DuplicateViewText), false, DuplicateView);
            menu.AddItem(new GUIContent(RenameViewText), false, StartRenameView);
            // The last view of a table can't be deleted: Reset View clears it instead
            if (Views.CanDeleteView(openView))
                menu.AddItem(new GUIContent(DeleteViewText), false, DeleteView);
            else
                menu.AddDisabledItem(new GUIContent(DeleteViewText));

            menu.AddSeparator(string.Empty);
            menu.AddItem(new GUIContent(TableView.ResetViewText), false, _tableView.ResetView);
            menu.DropDown(_viewButton.worldBound);
        }

        /// <summary>
        /// Opens another view of the table, and shows the table with its settings.
        /// </summary>
        /// <param name="view">The view to open.</param>
        private void OpenView(PersonalView view)
        {
            if (!Views.SetOpenView(view))
                return;

            UserViews.instance.ScheduleSave();
            ShowOpenView();
        }

        /// <summary>
        /// Creates a view with the defaults of the table, and opens it.
        /// </summary>
        private void CreateView()
        {
            PersonalView view = Views.CreateView(_table.Id);
            Views.SetOpenView(view);
            UserViews.instance.ScheduleSave();
            ShowOpenView();
        }

        /// <summary>
        /// Creates a copy of the open view, and opens it.
        /// </summary>
        private void DuplicateView()
        {
            PersonalView copy = Views.DuplicateView(GetOpenView());
            Views.SetOpenView(copy);
            UserViews.instance.ScheduleSave();
            ShowOpenView();
        }

        /// <summary>
        /// Opens the view panel if it's closed, and gives the focus to the name of the open view, so the user can type a new name.
        /// </summary>
        private void StartRenameView()
        {
            if (!IsViewPanelOpen)
            {
                SetViewPanelOpen(true);
                ViewPanelToggled?.Invoke(true);
            }

            _viewPanel.FocusViewName();
        }

        /// <summary>
        /// Called when the user confirms a new name for the open view in the view panel. Renames the view, and shows its actual name
        /// (with a number if the name was taken, or the previous name if the typed one is blank).
        /// </summary>
        /// <param name="name">The typed name.</param>
        private void OnViewRenamed(string name)
        {
            PersonalView view = GetOpenView();
            if (view == null)
                return;

            if (Views.RenameView(view, name))
                UserViews.instance.ScheduleSave();

            UpdateViewControls();
        }

        /// <summary>
        /// Deletes the open view after a confirmation, and opens the next view of the table.
        /// </summary>
        private void DeleteView()
        {
            PersonalView view = GetOpenView();
            string message = $"Delete the view \"{view.Name}\" of the table {_table.Name}? Its settings (the columns, the sort, the " +
                "filters and the folders) are lost. This can't be undone.";
            if (!EditorUtility.DisplayDialog(DeleteViewTitle, message, DeleteText, CancelText))
                return;

            if (!Views.DeleteView(view))
                return;

            UserViews.instance.ScheduleSave();
            ShowOpenView();
        }

        /// <summary>
        /// Shows the table with the settings of its open view, after another view has been opened.
        /// </summary>
        /// <remarks>
        /// The search is kept, like after a reload, since it's not a setting of the views. The first page is shown, since the rows of
        /// another view are filtered and sorted differently (like after another sort).
        /// </remarks>
        private void ShowOpenView()
        {
            SetTable(_table, _searchField.value, 0);
            // The table view doesn't notify the page it's given
            PageChanged?.Invoke(0);
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

        /// <summary>
        /// Called when the user asks to filter the rows by a column from the menu of its header. Opens the view panel if it's closed,
        /// and adds a condition on the column to the filters.
        /// </summary>
        /// <param name="columnId">The identifier of the column.</param>
        private void OnFilterByColumnRequested(string columnId)
        {
            if (!IsViewPanelOpen)
            {
                SetViewPanelOpen(true);
                ViewPanelToggled?.Invoke(true);
            }

            _viewPanel.AddCondition(columnId);
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
