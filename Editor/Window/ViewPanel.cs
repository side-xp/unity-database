using UnityEngine.UIElements;

namespace SideXP.Database.EditorOnly
{

    /// <summary>
    /// The panel of the view, on the right of the table: the settings of the view that don't fit in the headers, by section. For now,
    /// the filters (see <see cref="FilterEditor"/>).
    /// </summary>
    /// <remarks>
    /// The panel stays open while the user edits and sees the result, unlike a popup, which would close when another window takes the
    /// focus (for example Unity's object picker). Each change is applied to the table right away, and remembered with the settings of
    /// the table (see <see cref="TableView.SetRowFilter(FilterNode)"/>).
    /// <para>
    /// @todo 21b3: the section of the folder scope. Step 23: the section of the views.
    /// </para>
    /// </remarks>
    internal class ViewPanel : VisualElement
    {

        #region Fields

        private const string PanelClass = "database-view-panel";
        private const string ContentClass = "database-view-panel__content";
        private const string SectionClass = "database-view-panel__section";
        private const string SectionHeaderClass = "database-view-panel__section-header";
        private const string SectionTitleClass = "database-view-panel__section-title";
        private const string SectionButtonClass = "database-view-panel__section-button";

        private const string FiltersTitle = "Filters";
        private const string ClearFiltersText = "Clear";
        private const string ClearFiltersTooltip = "Removes all the conditions, so the filters show all the rows again.";

        /// <summary>
        /// The table view whose settings the panel edits.
        /// </summary>
        private TableView _tableView = null;

        /// <summary>
        /// Edits the filters of the table.
        /// </summary>
        private FilterEditor _filterEditor = null;

        /// <summary>
        /// The button that clears the filters, disabled when there is nothing to clear.
        /// </summary>
        private Button _clearFiltersButton = null;

        #endregion


        #region Lifecycle

        /// <inheritdoc cref="ViewPanel"/>
        /// <param name="tableView"><inheritdoc cref="_tableView" path="/summary"/></param>
        public ViewPanel(TableView tableView)
        {
            _tableView = tableView;
            AddToClassList(PanelClass);

            ScrollView content = new ScrollView(ScrollViewMode.Vertical);
            content.AddToClassList(ContentClass);
            Add(content);

            _clearFiltersButton = new Button(ClearFilters) { tooltip = ClearFiltersTooltip };
            DatabaseElements.SetIcon(_clearFiltersButton, DatabaseIcons.ClearFilters, ClearFiltersText);
            VisualElement filtersSection = CreateSection(content, FiltersTitle, _clearFiltersButton);

            _filterEditor = new FilterEditor();
            _filterEditor.Changed += OnFilterChanged;
            filtersSection.Add(_filterEditor);
        }

        #endregion


        #region Public API

        /// <summary>
        /// Shows the settings of the table displayed by the table view. Call it after the table changes.
        /// </summary>
        public void Refresh()
        {
            // A filter set from code may not be a group: it becomes the first node of the root edited by the panel
            FilterNode filter = _tableView.RowFilter;
            FilterGroup root = filter as FilterGroup;
            if (root == null && filter != null)
                root = new FilterGroup(FilterOperator.And, filter);

            _filterEditor.SetFilter(root, _tableView.GetFilterableColumns(), columnId => !_tableView.IsColumnDisplayed(columnId));
            UpdateClearFiltersButton();
        }

        /// <summary>
        /// Adds a condition on a column at the end of the filters, and applies it (see <see cref="FilterEditor.AddCondition(string)"/>).
        /// </summary>
        /// <param name="columnId">The identifier of the column.</param>
        public void AddCondition(string columnId)
        {
            _filterEditor.AddCondition(columnId);
        }

        #endregion


        #region Private API

        /// <summary>
        /// Creates a section of the panel: a header with a title and optional buttons, then the content of the section.
        /// </summary>
        /// <param name="parent">The element to which the section is added.</param>
        /// <param name="title">The title of the section.</param>
        /// <param name="buttons">The buttons shown on the right of the title.</param>
        /// <returns>Returns the element that holds the content of the section.</returns>
        private static VisualElement CreateSection(VisualElement parent, string title, params Button[] buttons)
        {
            VisualElement section = new VisualElement();
            section.AddToClassList(SectionClass);

            VisualElement header = new VisualElement();
            header.AddToClassList(SectionHeaderClass);
            Label titleLabel = new Label(title);
            titleLabel.AddToClassList(SectionTitleClass);
            header.Add(titleLabel);
            foreach (Button button in buttons)
            {
                button.AddToClassList(SectionButtonClass);
                header.Add(button);
            }

            section.Add(header);
            parent.Add(section);
            return section;
        }

        /// <summary>
        /// Called after each change of the filter editor. Applies the filter to the table, which also remembers it.
        /// </summary>
        private void OnFilterChanged()
        {
            _tableView.SetRowFilter(_filterEditor.Root);
            UpdateClearFiltersButton();
        }

        /// <summary>
        /// Removes all the conditions of the filters.
        /// </summary>
        private void ClearFilters()
        {
            _tableView.SetRowFilter(null);
            Refresh();
        }

        /// <summary>
        /// Enables the button that clears the filters only if the filters have nodes.
        /// </summary>
        private void UpdateClearFiltersButton()
        {
            _clearFiltersButton.SetEnabled(_filterEditor.Root.Nodes.Count > 0);
        }

        #endregion

    }

}
