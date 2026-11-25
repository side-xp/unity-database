using System;
using System.Collections.Generic;

using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace SideXP.Database.EditorOnly
{

    /// <summary>
    /// The page of the Database window that lists the tables, with a search field to filter them.
    /// </summary>
    internal class TableListPage : VisualElement
    {

        #region Fields

        private const string PageClass = "database-page";
        private const string ToolbarClass = "database-toolbar";
        private const string SearchFieldClass = "database-toolbar__search";
        private const string ListClass = "database-table-list";
        private const string ItemClass = "database-table-list__item";
        private const string ItemNameClass = "database-table-list__name";
        private const string MessageClass = "database-message";

        /// <summary>
        /// The height of the items of the list, in pixels. Taller than the default height of the list views of the editor, to fit the
        /// larger names with some space around them.
        /// </summary>
        private const float ItemHeight = 28;

        private const string ItemNameElement = "name";
        private const string ObsoleteMarkElement = "obsolete-mark";

        private const string NoTableMessage = "No table found in the project.\nAdd [Table] to a ScriptableObject class to create one.";
        private const string NoMatchMessage = "No table matches the search.";

        /// <summary>
        /// All the tables of the project, sorted by name.
        /// </summary>
        private IReadOnlyList<TableDefinition> _tables = null;

        /// <summary>
        /// The tables that match the search, displayed in the list.
        /// </summary>
        private List<TableDefinition> _visibleTables = new List<TableDefinition>();

        /// <summary>
        /// The field used to filter the tables by name.
        /// </summary>
        private ToolbarSearchField _searchField = null;

        /// <summary>
        /// The list of the tables that match the search.
        /// </summary>
        private ListView _listView = null;

        /// <summary>
        /// The message shown instead of the list when there is no table to display.
        /// </summary>
        private Label _messageLabel = null;

        /// <summary>
        /// Called when the user chooses a table to open.
        /// </summary>
        public event Action<TableDefinition> TableChosen;

        /// <summary>
        /// Called when the user changes the text of the search field.
        /// </summary>
        public event Action<string> SearchChanged;

        #endregion


        #region Lifecycle

        /// <inheritdoc cref="TableListPage"/>
        /// <param name="tables"><inheritdoc cref="_tables" path="/summary"/></param>
        /// <param name="search">The initial text of the search field.</param>
        public TableListPage(IReadOnlyList<TableDefinition> tables, string search)
        {
            _tables = tables;
            AddToClassList(PageClass);

            Toolbar toolbar = new Toolbar();
            toolbar.AddToClassList(ToolbarClass);
            _searchField = new ToolbarSearchField();
            _searchField.AddToClassList(SearchFieldClass);
            _searchField.SetValueWithoutNotify(search ?? string.Empty);
            _searchField.RegisterValueChangedCallback(HandleSearchChange);
            toolbar.Add(_searchField);
            Add(toolbar);

            _listView = new ListView(_visibleTables, ItemHeight, MakeItem, BindItem);
            _listView.selectionType = SelectionType.Single;
            _listView.itemsChosen += HandleItemsChosen;
            _listView.AddToClassList(ListClass);
            Add(_listView);

            _messageLabel = new Label();
            _messageLabel.AddToClassList(MessageClass);
            Add(_messageLabel);

            ApplySearch(search);
        }

        #endregion


        #region Public API

        /// <summary>
        /// Gets the tables whose name contains a search text, ignoring case.
        /// </summary>
        /// <param name="tables">The tables to filter.</param>
        /// <param name="search">The search text. If null or blank, all the tables match.</param>
        /// <returns>Returns the tables that match, in their original order.</returns>
        public static List<TableDefinition> FilterTables(IEnumerable<TableDefinition> tables, string search)
        {
            string trimmedSearch = search?.Trim();
            List<TableDefinition> matchingTables = new List<TableDefinition>();
            foreach (TableDefinition table in tables)
            {
                if (string.IsNullOrEmpty(trimmedSearch) || table.Name.IndexOf(trimmedSearch, StringComparison.OrdinalIgnoreCase) >= 0)
                    matchingTables.Add(table);
            }
            return matchingTables;
        }

        #endregion


        #region UI

        /// <summary>
        /// Creates the element of an item of the list.
        /// </summary>
        /// <returns>Returns the created element.</returns>
        private VisualElement MakeItem()
        {
            VisualElement item = new VisualElement();
            item.AddToClassList(ItemClass);

            // The item is the target of the pointer, so its hand cursor shows over its whole area (the cursor isn't inherited)
            Label nameLabel = new Label { name = ItemNameElement, pickingMode = PickingMode.Ignore };
            nameLabel.AddToClassList(ItemNameClass);
            item.Add(nameLabel);

            VisualElement obsoleteMark = DatabaseElements.CreateObsoleteMark();
            obsoleteMark.name = ObsoleteMarkElement;
            obsoleteMark.pickingMode = PickingMode.Ignore;
            item.Add(obsoleteMark);

            // A single click opens the table, since the list is only used to navigate
            item.RegisterCallback<ClickEvent>(evt =>
            {
                if (evt.button == (int)MouseButton.LeftMouse)
                    ChooseTable(item.userData as TableDefinition);
            });

            return item;
        }

        /// <summary>
        /// Displays a table in an item of the list.
        /// </summary>
        /// <param name="item">The element of the item.</param>
        /// <param name="index">The index of the table in the visible tables.</param>
        private void BindItem(VisualElement item, int index)
        {
            TableDefinition table = _visibleTables[index];
            item.userData = table;
            item.tooltip = table.Type.FullName;
            item.Q<Label>(ItemNameElement).text = table.Name;
            item.Q(ObsoleteMarkElement).SetDisplayed(table.IsObsolete);
        }

        /// <summary>
        /// Filters the list with a search text, and shows a message instead of the list if no table is visible.
        /// </summary>
        /// <param name="search">The search text.</param>
        private void ApplySearch(string search)
        {
            _visibleTables.Clear();
            _visibleTables.AddRange(FilterTables(_tables, search));
            _listView.Rebuild();

            bool hasVisibleTables = _visibleTables.Count > 0;
            _messageLabel.text = _tables.Count > 0 ? NoMatchMessage : NoTableMessage;
            _messageLabel.SetDisplayed(!hasVisibleTables);
            _listView.SetDisplayed(hasVisibleTables);
        }

        #endregion


        #region Private API

        /// <summary>
        /// Notifies that a table has been chosen.
        /// </summary>
        /// <param name="table">The chosen table. Ignored if null.</param>
        private void ChooseTable(TableDefinition table)
        {
            if (table != null)
                TableChosen?.Invoke(table);
        }

        /// <summary>
        /// Called when the text of the search field changes.
        /// </summary>
        /// <param name="evt">The event of the change.</param>
        private void HandleSearchChange(ChangeEvent<string> evt)
        {
            ApplySearch(evt.newValue);
            SearchChanged?.Invoke(evt.newValue);
        }

        /// <summary>
        /// Called when items of the list are chosen with the keyboard (Enter) or a double-click.
        /// </summary>
        /// <param name="items">The chosen items.</param>
        private void HandleItemsChosen(IEnumerable<object> items)
        {
            foreach (object item in items)
            {
                ChooseTable(item as TableDefinition);
                return;
            }
        }

        #endregion

    }

}
