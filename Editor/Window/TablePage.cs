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

        private const string BackText = "Back";
        private const string BackTooltip = "Back to the table list.";
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
        /// Displays the rows of the table.
        /// </summary>
        private TableView _tableView = null;

        /// <summary>
        /// Called when the user asks to go back to the table list.
        /// </summary>
        public event Action BackClicked;

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
        public void SetTable(TableDefinition table)
        {
            _table = table;
            _titleLabel.text = table.Name;
            _titleLabel.tooltip = table.Type.FullName;
            _obsoleteMark.SetDisplayed(table.IsObsolete);
            _tableView.SetTable(table);
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

    }

}
