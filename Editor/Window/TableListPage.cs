using System;
using System.Collections.Generic;

using UnityEngine.UIElements;
using UnityEditor.UIElements;

using SideXP.Core.EditorOnly;

namespace SideXP.Database.EditorOnly
{

    /// <summary>
    /// The page of the Database window that lists the tables, as a tree grouped by the <see cref="TableTree.Separator"/> in their names,
    /// with a search field to filter them.
    /// </summary>
    /// <remarks>
    /// The groups are collapsed until the user expands them, and the expanded groups are remembered by the window. While a search is
    /// typed, all the groups of the matching tables are expanded, and expanding or collapsing a group is not remembered.
    /// </remarks>
    internal class TableListPage : VisualElement
    {

        #region Fields

        private const string PageClass = "database-page";
        private const string ToolbarClass = "database-toolbar";
        private const string SearchFieldClass = "database-toolbar__search";
        private const string ListClass = "database-table-list";
        private const string ItemClass = "database-table-list__item";
        private const string ItemIconClass = "database-table-list__icon";
        private const string ItemNameClass = "database-table-list__name";
        private const string GroupItemClass = "database-table-list__item--group";
        private const string MessageClass = "database-message";

        /// <summary>
        /// The height of the items of the list, in pixels. Taller than the default height of the list views of the editor, to fit the
        /// larger names with some space around them.
        /// </summary>
        private const int ItemHeight = 28;

        private const string ItemNameElement = "name";
        private const string ObsoleteMarkElement = "obsolete-mark";

        private const string NoTableMessage = "No table found in the project.\nAdd [Table] to a ScriptableObject class to create one.";
        private const string NoMatchMessage = "No table matches the search.";

        /// <summary>
        /// All the tables of the project, sorted by name.
        /// </summary>
        private IReadOnlyList<TableDefinition> _tables = null;

        /// <summary>
        /// The paths of the groups expanded by the user, ignoring case (see <see cref="TableTreeNode.GroupPath"/>). Can contain the
        /// paths of groups that don't exist anymore.
        /// </summary>
        private HashSet<string> _expandedGroups = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// The identifiers of the groups shown in the tree, by path, ignoring case.
        /// </summary>
        private Dictionary<string, int> _groupIds = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Checks if a search is typed, in which case all the groups are expanded.
        /// </summary>
        private bool _isSearching = false;

        /// <summary>
        /// Checks if the page is expanding or collapsing the groups itself, so the changes are not taken as the choices of the user.
        /// </summary>
        private bool _isApplyingExpansion = false;

        /// <summary>
        /// The field used to filter the tables by name.
        /// </summary>
        private ToolbarSearchField _searchField = null;

        /// <summary>
        /// The tree of the tables that match the search.
        /// </summary>
        private TreeView _treeView = null;

        /// <summary>
        /// The message shown instead of the tree when there is no table to display.
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

        /// <summary>
        /// Called when the user expands or collapses a group, with the path of the group and true if it's expanded. Not called while a
        /// search is typed.
        /// </summary>
        public event Action<string, bool> GroupExpandedChanged;

        #endregion


        #region Lifecycle

        /// <inheritdoc cref="TableListPage"/>
        /// <param name="tables"><inheritdoc cref="_tables" path="/summary"/></param>
        /// <param name="search">The initial text of the search field.</param>
        /// <param name="expandedGroups">The paths of the groups expanded by the user, or null if there is none.</param>
        public TableListPage(IReadOnlyList<TableDefinition> tables, string search, IEnumerable<string> expandedGroups)
        {
            _tables = tables;
            if (expandedGroups != null)
                _expandedGroups.UnionWith(expandedGroups);

            AddToClassList(PageClass);

            Toolbar toolbar = new Toolbar();
            toolbar.AddToClassList(ToolbarClass);
            _searchField = new ToolbarSearchField();
            _searchField.AddToClassList(SearchFieldClass);
            _searchField.SetValueWithoutNotify(search ?? string.Empty);
            _searchField.RegisterValueChangedCallback(HandleSearchChange);
            toolbar.Add(_searchField);
            Add(toolbar);

            _treeView = new TreeView(ItemHeight, MakeItem, BindItem);
            _treeView.selectionType = SelectionType.Single;
            _treeView.itemsChosen += HandleItemsChosen;
            _treeView.itemExpandedChanged += HandleItemExpandedChanged;
            _treeView.AddToClassList(ListClass);
            Add(_treeView);

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
        /// <remarks>
        /// The whole name is searched, groups included, so the name of a group shows all its tables.
        /// </remarks>
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
        /// Creates the element of an item of the tree.
        /// </summary>
        /// <returns>Returns the created element.</returns>
        private VisualElement MakeItem()
        {
            VisualElement item = new VisualElement();
            item.AddToClassList(ItemClass);

            // The item is the target of the pointer, so its hand cursor shows over its whole area (the cursor isn't inherited)
            Icon icon = new Icon(DatabaseIcons.Table) { pickingMode = PickingMode.Ignore };
            icon.AddToClassList(ItemIconClass);
            item.Add(icon);

            Label nameLabel = new Label { name = ItemNameElement, pickingMode = PickingMode.Ignore };
            nameLabel.AddToClassList(ItemNameClass);
            item.Add(nameLabel);

            VisualElement obsoleteMark = DatabaseElements.CreateObsoleteMark();
            obsoleteMark.name = ObsoleteMarkElement;
            obsoleteMark.pickingMode = PickingMode.Ignore;
            item.Add(obsoleteMark);

            // A single click opens a table, since the list is only used to navigate, or toggles a group. The second click of a
            // double-click is ignored, so a double-click on a group expands it like in the Project window
            item.RegisterCallback<ClickEvent>(evt =>
            {
                if (evt.button == (int)MouseButton.LeftMouse && evt.clickCount == 1)
                    HandleItemClicked(item.userData as TableTreeNode);
            });

            return item;
        }

        /// <summary>
        /// Displays a group or a table in an item of the tree.
        /// </summary>
        /// <param name="item">The element of the item.</param>
        /// <param name="index">The index of the item among the visible items of the tree.</param>
        private void BindItem(VisualElement item, int index)
        {
            TableTreeNode node = _treeView.GetItemDataForIndex<TableTreeNode>(index);
            item.userData = node;
            item.EnableInClassList(GroupItemClass, node.IsGroup);
            item.Q<Label>(ItemNameElement).text = node.Name;

            Icon icon = item.Q<Icon>();
            if (node.IsGroup)
            {
                item.tooltip = node.GroupPath;
                icon.IconName = _treeView.IsExpanded(_treeView.GetIdForIndex(index))
                    ? DatabaseIcons.ExpandedTableGroup
                    : DatabaseIcons.TableGroup;
            }
            else
            {
                item.tooltip = node.Table.Type.FullName;
                icon.IconName = DatabaseIcons.Table;
            }

            item.Q(ObsoleteMarkElement).SetDisplayed(!node.IsGroup && node.Table.IsObsolete);
        }

        /// <summary>
        /// Filters the tree with a search text, and shows a message instead of the tree if no table is visible.
        /// </summary>
        /// <param name="search">The search text.</param>
        private void ApplySearch(string search)
        {
            List<TableDefinition> visibleTables = FilterTables(_tables, search);
            _isSearching = !string.IsNullOrWhiteSpace(search);

            _groupIds.Clear();
            int nextId = 0;
            _treeView.SetRootItems(CreateItems(TableTree.Build(visibleTables), ref nextId));
            ApplyExpansion();

            bool hasVisibleTables = visibleTables.Count > 0;
            _messageLabel.text = _tables.Count > 0 ? NoMatchMessage : NoTableMessage;
            _messageLabel.SetDisplayed(!hasVisibleTables);
            _treeView.SetDisplayed(hasVisibleTables);
        }

        /// <summary>
        /// Creates the items of the tree view, and stores the identifiers of the groups.
        /// </summary>
        /// <param name="nodes">The groups and tables from which to create the items.</param>
        /// <param name="nextId">The identifier of the next item, incremented for each created item.</param>
        /// <returns>Returns the created items, in the order of the given nodes.</returns>
        private List<TreeViewItemData<TableTreeNode>> CreateItems(IReadOnlyList<TableTreeNode> nodes, ref int nextId)
        {
            List<TreeViewItemData<TableTreeNode>> items = new List<TreeViewItemData<TableTreeNode>>(nodes.Count);
            foreach (TableTreeNode node in nodes)
            {
                int id = nextId++;
                if (node.IsGroup)
                    _groupIds[node.GroupPath] = id;

                List<TreeViewItemData<TableTreeNode>> children = node.IsGroup ? CreateItems(node.Children, ref nextId) : null;
                items.Add(new TreeViewItemData<TableTreeNode>(id, node, children));
            }
            return items;
        }

        /// <summary>
        /// Expands the groups expanded by the user, or all the groups while a search is typed, and collapses the others.
        /// </summary>
        private void ApplyExpansion()
        {
            _isApplyingExpansion = true;
            try
            {
                // The ids are given again on each search, so the expanded ids of the previous tree mean nothing
                _treeView.CollapseAll();
                foreach (KeyValuePair<string, int> group in _groupIds)
                {
                    if (_isSearching || _expandedGroups.Contains(group.Key))
                        _treeView.ExpandItem(group.Value, false, false);
                }
            }
            finally
            {
                _isApplyingExpansion = false;
            }

            _treeView.Rebuild();
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
        /// Expands a group if it's collapsed, or collapses it.
        /// </summary>
        /// <param name="group">The group to toggle.</param>
        private void ToggleGroup(TableTreeNode group)
        {
            if (!_groupIds.TryGetValue(group.GroupPath, out int id))
                return;

            if (_treeView.IsExpanded(id))
                _treeView.CollapseItem(id);
            else
                _treeView.ExpandItem(id);
        }

        /// <summary>
        /// Remembers that the user expanded or collapsed a group, and notifies it.
        /// </summary>
        /// <param name="group">The expanded or collapsed group.</param>
        /// <param name="isExpanded">True if the group is expanded, false if it's collapsed.</param>
        /// <param name="isAppliedToSubgroups">True if the groups of the group are expanded or collapsed too.</param>
        private void SetGroupExpanded(TableTreeNode group, bool isExpanded, bool isAppliedToSubgroups)
        {
            bool hasChanged = isExpanded
                ? _expandedGroups.Add(group.GroupPath)
                : _expandedGroups.Remove(group.GroupPath);

            if (hasChanged)
                GroupExpandedChanged?.Invoke(group.GroupPath, isExpanded);

            if (!isAppliedToSubgroups)
                return;

            foreach (TableTreeNode child in group.Children)
            {
                if (child.IsGroup)
                    SetGroupExpanded(child, isExpanded, true);
            }
        }

        /// <summary>
        /// Called when an item of the tree is clicked: opens a table, or toggles a group.
        /// </summary>
        /// <param name="node">The clicked item. Ignored if null.</param>
        private void HandleItemClicked(TableTreeNode node)
        {
            if (node == null)
                return;

            if (node.IsGroup)
                ToggleGroup(node);
            else
                ChooseTable(node.Table);
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
        /// Called when items of the tree are chosen with the keyboard (Enter) or a double-click. Opens a table, and ignores groups: the
        /// click already toggles them, and the arrow keys expand and collapse them.
        /// </summary>
        /// <param name="items">The chosen items.</param>
        private void HandleItemsChosen(IEnumerable<object> items)
        {
            foreach (object item in items)
            {
                if (item is TableTreeNode node && !node.IsGroup)
                    ChooseTable(node.Table);

                return;
            }
        }

        /// <summary>
        /// Called when a group of the tree is expanded or collapsed, by the user (its arrow, a click, the arrow keys) or by the page.
        /// </summary>
        /// <param name="args">The arguments of the change.</param>
        private void HandleItemExpandedChanged(TreeViewExpansionChangedArgs args)
        {
            if (_isApplyingExpansion || _isSearching)
                return;

            TableTreeNode node = _treeView.GetItemDataForId<TableTreeNode>(args.id);
            if (node != null && node.IsGroup)
                SetGroupExpanded(node, args.isExpanded, args.isAppliedToAllChildren);
        }

        #endregion

    }

}
