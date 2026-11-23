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
        private const string ContentClass = "database-page__content";
        private const string TitleClass = "database-toolbar__title";
        private const string ObsoleteTagClass = "database-obsolete-tag";

        private const string BackText = "Back";
        private const string BackTooltip = "Back to the table list.";
        private const string ObsoleteText = "Obsolete";
        private const string ObsoleteTooltip = "The type of this table is marked [Obsolete].";

        /// <summary>
        /// The displayed table, or null if none has been set.
        /// </summary>
        private TableDefinition _table = null;

        /// <summary>
        /// The label that displays the name of the table in the toolbar.
        /// </summary>
        private Label _titleLabel = null;

        /// <summary>
        /// The tag shown in the toolbar if the table is obsolete.
        /// </summary>
        private Label _obsoleteTag = null;

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
            ToolbarButton backButton = new ToolbarButton(() => BackClicked?.Invoke()) { text = BackText, tooltip = BackTooltip };
            toolbar.Add(backButton);

            _titleLabel = new Label();
            _titleLabel.AddToClassList(TitleClass);
            toolbar.Add(_titleLabel);

            _obsoleteTag = new Label(ObsoleteText) { tooltip = ObsoleteTooltip };
            _obsoleteTag.AddToClassList(ObsoleteTagClass);
            _obsoleteTag.SetDisplayed(false);
            toolbar.Add(_obsoleteTag);
            Add(toolbar);

            // @note The table view (header, rows and status line) is built in step 5 of the implementation
            VisualElement content = new VisualElement();
            content.AddToClassList(ContentClass);
            Add(content);
        }

        #endregion


        #region Public API

        /// <inheritdoc cref="_table"/>
        public TableDefinition Table => _table;

        /// <summary>
        /// Sets the displayed table.
        /// </summary>
        /// <param name="table">The table to display.</param>
        public void SetTable(TableDefinition table)
        {
            _table = table;
            _titleLabel.text = table.Name;
            _titleLabel.tooltip = table.Type.FullName;
            _obsoleteTag.SetDisplayed(table.IsObsolete);
        }

        #endregion

    }

}
