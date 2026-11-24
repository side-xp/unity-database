using System;

using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace SideXP.Database.EditorOnly
{

    /// <summary>
    /// The page of the Database window that displays a table.
    /// </summary>
    internal class TablePage : VisualElement
    {

        #region Fields

        private const string PageClass = "database-page";
        private const string TitleClass = "database-toolbar__title";
        private const string SpacerClass = "database-toolbar__spacer";
        private const string RowSearchClass = "database-toolbar__row-search";

        private const string SearchTooltip = "Shows the rows that contain each word of the search, in their asset name or in their " +
            "cells, ignoring case. Cells are searched as they are displayed: names for enums, paths for references.";

        private const string BackText = "Back";
        private const string BackTooltip = "Back to the table list.";
        private const string CreateText = "Create";
        private const string RefreshText = "Refresh";
        private const string RefreshTooltip = "Loads the assets of the table again. Use it if a change made outside of the window " +
            "doesn't show up, such as a value written by a script without notifying Unity.";

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
        /// Displays the rows of the table.
        /// </summary>
        private TableView _tableView = null;

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
            _createButton = new ToolbarButton { text = CreateText };
            _createButton.clicked += () => _tableView.StartCreateAsset(_createButton);
            toolbar.Add(_createButton);

            ToolbarButton refreshButton = new ToolbarButton(_tableView.Reload) { tooltip = RefreshTooltip };
            DatabaseElements.SetIcon(refreshButton, DatabaseIcons.Refresh, RefreshText);
            toolbar.Add(refreshButton);
            Add(toolbar);
            Add(_tableView);
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
        }

        /// <summary>
        /// Removes the displayed table, so it stops tracking the changes of its assets while the page is hidden.
        /// </summary>
        public void ClearTable()
        {
            _table = null;
            _tableView.ClearTable();
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
