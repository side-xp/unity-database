using System;
using System.Collections.Generic;
using System.Text;

using UnityEngine.UIElements;

using SideXP.Core.EditorOnly;

namespace SideXP.Database.EditorOnly
{

    /// <summary>
    /// The panel of the view, on the right of the table: the settings of the open view that don't fit in the headers, by section. The
    /// name of the view, the folder scope (see <see cref="FolderScopeEditor"/>), then the filters (see <see cref="FilterEditor"/>).
    /// </summary>
    /// <remarks>
    /// The panel stays open while the user edits and sees the result, unlike a popup, which would close when another window takes the
    /// focus (for example Unity's object picker). Each change is applied to the table right away, and remembered with the settings of
    /// the table (see <see cref="TableView.SetRowFilter(FilterNode)"/> and <see cref="TableView.SetFolderScope"/>). The name of the view
    /// is renamed by the owner of the panel (see <see cref="ViewRenamed"/>), which also handles the other operations on the views.
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
        private const string ViewNameClass = "database-view-panel__view-name";
        private const string ViewStatusClass = "database-view-panel__view-status";
        private const string ViewStatusIconClass = "database-view-panel__view-status-icon";
        private const string ViewStatusLabelClass = "database-view-panel__view-status-label";
        private const string SectionSpacerClass = "database-view-panel__section-spacer";
        private const string OverrideMarkClass = "database-view-panel__override-mark";
        private const string OverridesClass = "database-view-panel__overrides";
        private const string OverridesTitleClass = "database-view-panel__overrides-title";
        private const string OverrideClass = "database-view-panel__override";
        private const string OverrideTextClass = "database-view-panel__override-text";
        private const string OverrideResetClass = "database-view-panel__override-reset";
        private const string OverrideActionsClass = "database-view-panel__override-actions";

        private const string OverridesTitle = "Overrides";
        private const string OverrideResetTooltip = "Discards this change, so this setting comes from the project view again.";
        private const string SaveOverridesText = "Save for Everyone";
        private const string SaveOverridesTooltip = "Writes your overrides to the project view, for the whole team. They're saved in " +
            "the project file, which must be committed so the others get them.";
        private const string ResetOverridesText = "Reset All";
        private const string ResetOverridesTooltip = "Discards all your overrides to this project view, so you see it as saved in the " +
            "project again.";
        private const string FoldersOverrideTooltip = "You changed the folders of this project view: your folders are a personal " +
            "override, saved for you only.";
        private const string FiltersOverrideTooltip = "You changed the filters of this project view: your filters are a personal " +
            "override, saved for you only.";

        private const string ViewTitle = "View";
        private const string ViewNameTooltip = "The name of the view, unique among the views of the table. Applied with Enter, or when " +
            "the field loses the focus. A name taken by another view gets a number. Renaming a project view renames it for the team.";
        private const string FiltersTitle = "Filters";
        private const string ClearFiltersText = "Clear";
        private const string ClearFiltersTooltip = "Removes all the conditions, so the filters show all the rows again.";
        private const string FoldersTitle = "Folders";
        private const string AddFolderText = "Add";
        private const string AddFolderTooltip = "Picks a folder of the project to add to the scope. The table only shows the assets of " +
            "the folders of the scope and of their subfolders, and doesn't load the others.";
        private const string ClearFoldersText = "Clear";
        private const string ClearFoldersTooltip = "Removes all the folders, so the table shows the assets of the whole project again.";

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

        /// <summary>
        /// Edits the folder scope of the table.
        /// </summary>
        private FolderScopeEditor _folderScopeEditor = null;

        /// <summary>
        /// The button that clears the folder scope, disabled when the scope is the whole project.
        /// </summary>
        private Button _clearFoldersButton = null;

        /// <summary>
        /// The field that shows and renames the open view.
        /// </summary>
        private TextField _viewNameField = null;

        /// <summary>
        /// The status of the open view, on the right of the title of the <i>View</i> section: an icon and a faded label, which say if
        /// the view is personal, or a project view with or without overrides of the user.
        /// </summary>
        private VisualElement _viewStatus = null;

        /// <summary>
        /// The icon of <see cref="_viewStatus"/>, the same as the icon of the button of the views.
        /// </summary>
        private Icon _viewStatusIcon = null;

        /// <summary>
        /// The label of <see cref="_viewStatus"/>.
        /// </summary>
        private Label _viewStatusLabel = null;

        /// <summary>
        /// The list of the overrides of the user on the open project view, with the buttons to save and reset them, hidden while the
        /// view has no override.
        /// </summary>
        private VisualElement _overrides = null;

        /// <summary>
        /// The rows of <see cref="_overrides"/>, one per kind of setting the user changed.
        /// </summary>
        private VisualElement _overrideRows = null;

        /// <summary>
        /// The kinds and counts of the overrides shown by <see cref="_overrides"/>, so the list is only built again when they change
        /// (see <see cref="SetOverrides"/>). Null when no override is shown.
        /// </summary>
        private string _shownOverridesKey = null;

        /// <summary>
        /// The mark after the title of the <i>Folders</i> section, shown while the folders are overridden.
        /// </summary>
        private VisualElement _foldersOverrideMark = null;

        /// <summary>
        /// The mark after the title of the <i>Filters</i> section, shown while the filters are overridden.
        /// </summary>
        private VisualElement _filtersOverrideMark = null;

        /// <summary>
        /// Called when the user confirms a new name for the open view, with the typed name. The owner renames the view, then shows
        /// its actual name with <see cref="SetView"/>.
        /// </summary>
        public event Action<string> ViewRenamed;

        /// <summary>
        /// Called when the user asks to write their overrides to the open project view, for the whole team.
        /// </summary>
        public event Action OverridesSaveRequested;

        /// <summary>
        /// Called when the user asks to discard all their overrides of the open project view.
        /// </summary>
        public event Action OverridesResetRequested;

        /// <summary>
        /// Called when the user asks to discard one kind of override of the open project view, with that kind.
        /// </summary>
        public event Action<ViewSettingKind> OverrideResetRequested;

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

            // Delayed, so the view is renamed once, not on each typed character (a taken name would get a number on the way)
            _viewNameField = new TextField { isDelayed = true, tooltip = ViewNameTooltip };
            _viewNameField.AddToClassList(ViewNameClass);
            _viewNameField.RegisterValueChangedCallback(evt => ViewRenamed?.Invoke(evt.newValue));

            // The icon and the text are set with the view
            _viewStatus = new VisualElement();
            _viewStatus.AddToClassList(ViewStatusClass);
            _viewStatusIcon = new Icon(DatabaseIcons.PersonalView) { pickingMode = PickingMode.Ignore };
            _viewStatusIcon.AddToClassList(ViewStatusIconClass);
            _viewStatus.Add(_viewStatusIcon);
            _viewStatusLabel = new Label { pickingMode = PickingMode.Ignore };
            _viewStatusLabel.AddToClassList(ViewStatusLabelClass);
            _viewStatus.Add(_viewStatusLabel);

            VisualElement viewSection = CreateSection(content, ViewTitle, null, _viewStatus);
            viewSection.Add(_viewNameField);
            viewSection.Add(CreateOverrides());

            // The folders first: they limit the assets of the table, which the filters then filter
            _folderScopeEditor = new FolderScopeEditor();
            _folderScopeEditor.Changed += OnFolderScopeChanged;

            Button addFolderButton = new Button(_folderScopeEditor.AddFolderFromDialog) { tooltip = AddFolderTooltip };
            DatabaseElements.SetIcon(addFolderButton, DatabaseIcons.AddFolder, AddFolderText);
            _clearFoldersButton = new Button(_folderScopeEditor.ClearFolders) { tooltip = ClearFoldersTooltip };
            DatabaseElements.SetIcon(_clearFoldersButton, DatabaseIcons.ClearFolders, ClearFoldersText);
            _foldersOverrideMark = CreateOverrideMark(FoldersOverrideTooltip);
            VisualElement foldersSection = CreateSection(content, FoldersTitle, _foldersOverrideMark, addFolderButton,
                _clearFoldersButton);
            foldersSection.Add(_folderScopeEditor);

            _clearFiltersButton = new Button(ClearFilters) { tooltip = ClearFiltersTooltip };
            DatabaseElements.SetIcon(_clearFiltersButton, DatabaseIcons.ClearFilters, ClearFiltersText);
            _filtersOverrideMark = CreateOverrideMark(FiltersOverrideTooltip);
            VisualElement filtersSection = CreateSection(content, FiltersTitle, _filtersOverrideMark, _clearFiltersButton);

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

            _folderScopeEditor.SetFolders(_tableView.FolderScope.Folders);
            UpdateClearFoldersButton();
        }

        /// <summary>
        /// Shows the name and the status of the open view, without notifying <see cref="ViewRenamed"/>.
        /// </summary>
        /// <param name="name">The name of the open view.</param>
        /// <param name="iconName">The icon of the status of the view, one of the constants of <see cref="DatabaseIcons"/>.</param>
        /// <param name="statusText">The short text of the status of the view, for example "Personal view".</param>
        /// <param name="statusTooltip">The tooltip of the status, which says what it means for the changes of the user.</param>
        public void SetView(string name, string iconName, string statusText, string statusTooltip)
        {
            _viewNameField.SetValueWithoutNotify(name);
            _viewStatusIcon.IconName = iconName;
            _viewStatusLabel.text = statusText;
            _viewStatus.tooltip = statusTooltip;
        }

        /// <summary>
        /// Shows the overrides of the user on the open project view: the list of the kinds of settings they changed, each with a reset
        /// button, the buttons to save and reset all of them, and the marks of the <i>Folders</i> and <i>Filters</i> sections.
        /// </summary>
        /// <remarks>
        /// The list is only built again when the kinds or the counts of the overrides change, since the owner calls this method on
        /// each change of the settings of the view, including each move of the pointer while a column is resized.
        /// </remarks>
        /// <param name="overrides">The layer of the overrides of the user, or null for a personal view. A layer that sets nothing hides
        /// the list.</param>
        public void SetOverrides(ViewSettings overrides)
        {
            bool hasOverrides = overrides != null && !overrides.IsEmpty;
            _foldersOverrideMark.SetDisplayed(hasOverrides && overrides.IsSet(ViewSettingKind.Folders));
            _filtersOverrideMark.SetDisplayed(hasOverrides && overrides.IsSet(ViewSettingKind.Filters));

            string key = hasOverrides ? GetOverridesKey(overrides) : null;
            if (key == _shownOverridesKey)
                return;

            _shownOverridesKey = key;
            _overrideRows.Clear();
            _overrides.SetDisplayed(hasOverrides);
            if (!hasOverrides)
                return;

            foreach (ViewSettingKind kind in ViewSettings.AllKinds)
            {
                int count = overrides.GetSetCount(kind);
                if (count > 0)
                    _overrideRows.Add(CreateOverrideRow(kind, count));
            }
        }

        /// <summary>
        /// Gives the focus to the name of the open view, with its text selected, so the user can type a new name.
        /// </summary>
        public void FocusViewName()
        {
            // On the next frame, since the panel may have just been displayed and the field can't take the focus before its layout
            schedule.Execute(() =>
            {
                _viewNameField.Focus();
                _viewNameField.SelectAll();
            });
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
        /// Creates a section of the panel: a header with a title and optional elements (buttons, a status), then the content of the
        /// section.
        /// </summary>
        /// <param name="parent">The element to which the section is added.</param>
        /// <param name="title">The title of the section.</param>
        /// <param name="titleMark">An element shown right after the title (for example the mark of an override), or null.</param>
        /// <param name="headerItems">The elements shown on the right of the header. The buttons get the style of the buttons of the
        /// sections.</param>
        /// <returns>Returns the element that holds the content of the section.</returns>
        private static VisualElement CreateSection(VisualElement parent, string title, VisualElement titleMark,
            params VisualElement[] headerItems)
        {
            VisualElement section = new VisualElement();
            section.AddToClassList(SectionClass);

            VisualElement header = new VisualElement();
            header.AddToClassList(SectionHeaderClass);
            Label titleLabel = new Label(title);
            titleLabel.AddToClassList(SectionTitleClass);
            header.Add(titleLabel);
            if (titleMark != null)
                header.Add(titleMark);

            // Pushes the other elements to the right, so the mark stays next to the title
            VisualElement spacer = new VisualElement();
            spacer.AddToClassList(SectionSpacerClass);
            header.Add(spacer);

            foreach (VisualElement item in headerItems)
            {
                if (item is Button)
                    item.AddToClassList(SectionButtonClass);

                header.Add(item);
            }

            section.Add(header);
            parent.Add(section);
            return section;
        }

        /// <summary>
        /// Creates the list of the overrides of the open project view, empty and hidden: a title, the rows (see
        /// <see cref="SetOverrides"/>), then the buttons that save and reset all the overrides.
        /// </summary>
        /// <returns>Returns the created list.</returns>
        private VisualElement CreateOverrides()
        {
            _overrides = new VisualElement();
            _overrides.AddToClassList(OverridesClass);
            _overrides.SetDisplayed(false);

            Label title = new Label(OverridesTitle);
            title.AddToClassList(OverridesTitleClass);
            _overrides.Add(title);

            _overrideRows = new VisualElement();
            _overrides.Add(_overrideRows);

            VisualElement actions = new VisualElement();
            actions.AddToClassList(OverrideActionsClass);
            Button saveButton = new Button(() => OverridesSaveRequested?.Invoke()) { tooltip = SaveOverridesTooltip };
            DatabaseElements.SetIcon(saveButton, DatabaseIcons.SaveOverrides, SaveOverridesText);
            actions.Add(saveButton);
            Button resetButton = new Button(() => OverridesResetRequested?.Invoke()) { tooltip = ResetOverridesTooltip };
            DatabaseElements.SetIcon(resetButton, DatabaseIcons.ResetOverrides, ResetOverridesText);
            actions.Add(resetButton);
            _overrides.Add(actions);
            return _overrides;
        }

        /// <summary>
        /// Creates the row of a kind of override: what the user changed, and a button that discards it.
        /// </summary>
        /// <param name="kind">The kind of setting the user changed.</param>
        /// <param name="count">The number of columns for the widths and the visibility, or 1.</param>
        /// <returns>Returns the created row.</returns>
        private VisualElement CreateOverrideRow(ViewSettingKind kind, int count)
        {
            VisualElement row = new VisualElement();
            row.AddToClassList(OverrideClass);

            Label text = new Label(GetOverrideText(kind, count));
            text.AddToClassList(OverrideTextClass);
            row.Add(text);

            Button resetButton = new Button(() => OverrideResetRequested?.Invoke(kind)) { tooltip = OverrideResetTooltip };
            DatabaseElements.SetIcon(resetButton, DatabaseIcons.ResetOverrides);
            resetButton.AddToClassList(OverrideResetClass);
            row.Add(resetButton);
            return row;
        }

        /// <summary>
        /// Creates the mark shown after the title of a section while its setting is overridden: the icon of the overridden project
        /// views, hidden by default.
        /// </summary>
        /// <param name="tooltip">The tooltip of the mark, which says what the user changed.</param>
        /// <returns>Returns the created mark.</returns>
        private static VisualElement CreateOverrideMark(string tooltip)
        {
            Icon mark = new Icon(DatabaseIcons.OverriddenProjectView) { tooltip = tooltip };
            mark.AddToClassList(OverrideMarkClass);
            mark.SetDisplayed(false);
            return mark;
        }

        /// <summary>
        /// Gets the text of a kind of override in the list of the overrides.
        /// </summary>
        /// <param name="kind">The kind of setting the user changed.</param>
        /// <param name="count">The number of columns for the widths and the visibility, or 1.</param>
        /// <returns>Returns the text, for example <c>Widths of 2 columns</c>.</returns>
        private static string GetOverrideText(ViewSettingKind kind, int count)
        {
            switch (kind)
            {
                case ViewSettingKind.ColumnOrder:
                    return "Column order";

                case ViewSettingKind.ColumnWidths:
                    return count == 1 ? "Width of 1 column" : $"Widths of {count} columns";

                case ViewSettingKind.ColumnVisibility:
                    return count == 1 ? "Visibility of 1 column" : $"Visibility of {count} columns";

                case ViewSettingKind.Sort:
                    return "Sort";

                case ViewSettingKind.Filters:
                    return "Filters";

                case ViewSettingKind.Folders:
                    return "Folders";

                default:
                    return kind.ToString();
            }
        }

        /// <summary>
        /// Gets a key of the kinds and the counts of the overrides of a layer, to know if the shown list is still right.
        /// </summary>
        /// <param name="overrides">The layer of the overrides.</param>
        /// <returns>Returns the key, for example <c>ColumnWidths:2;Sort:1;</c>.</returns>
        private static string GetOverridesKey(ViewSettings overrides)
        {
            StringBuilder key = new StringBuilder();
            foreach (ViewSettingKind kind in ViewSettings.AllKinds)
            {
                int count = overrides.GetSetCount(kind);
                if (count > 0)
                    key.Append($"{kind}:{count};");
            }

            return key.ToString();
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

        /// <summary>
        /// Called after each change of the folder scope editor. Applies the scope to the table, which loads its assets again and
        /// remembers the scope.
        /// </summary>
        /// <param name="folders">The new folders of the scope.</param>
        private void OnFolderScopeChanged(IReadOnlyList<string> folders)
        {
            _tableView.SetFolderScope(folders);
            UpdateClearFoldersButton();
        }

        /// <summary>
        /// Enables the button that clears the folder scope only if the scope has folders.
        /// </summary>
        private void UpdateClearFoldersButton()
        {
            _clearFoldersButton.SetEnabled(_folderScopeEditor.Folders.Count > 0);
        }

        #endregion

    }

}
