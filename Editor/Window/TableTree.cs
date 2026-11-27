using System;
using System.Collections.Generic;
using System.Linq;

namespace SideXP.Database.EditorOnly
{

    /// <summary>
    /// An item of the tree of the table list: a group of tables, or a table.
    /// </summary>
    internal class TableTreeNode
    {

        #region Fields

        /// <summary>
        /// The name shown in the tree: the last part of the name of the table, or the name of the group.
        /// </summary>
        private string _name = null;

        /// <summary>
        /// The path of the group, its name and the names of its parent groups joined with <see cref="TableTree.Separator"/>. Null for
        /// a table.
        /// </summary>
        private string _groupPath = null;

        /// <summary>
        /// The table of the item, or null for a group.
        /// </summary>
        private TableDefinition _table = null;

        /// <summary>
        /// The groups and tables of the group, or null for a table.
        /// </summary>
        private List<TableTreeNode> _children = null;

        #endregion


        #region Lifecycle

        /// <summary>
        /// Creates the item of a group, without children.
        /// </summary>
        /// <param name="name"><inheritdoc cref="_name" path="/summary"/></param>
        /// <param name="groupPath"><inheritdoc cref="_groupPath" path="/summary"/></param>
        public TableTreeNode(string name, string groupPath)
        {
            _name = name;
            _groupPath = groupPath;
            _children = new List<TableTreeNode>();
        }

        /// <summary>
        /// Creates the item of a table.
        /// </summary>
        /// <param name="name"><inheritdoc cref="_name" path="/summary"/></param>
        /// <param name="table"><inheritdoc cref="_table" path="/summary"/></param>
        public TableTreeNode(string name, TableDefinition table)
        {
            _name = name;
            _table = table;
        }

        #endregion


        #region Public API

        /// <inheritdoc cref="_name"/>
        public string Name => _name;

        /// <inheritdoc cref="_groupPath"/>
        public string GroupPath => _groupPath;

        /// <inheritdoc cref="_table"/>
        public TableDefinition Table => _table;

        /// <summary>
        /// The groups and tables of the group, sorted by name, or an empty list for a table.
        /// </summary>
        public IReadOnlyList<TableTreeNode> Children => _children ?? (IReadOnlyList<TableTreeNode>)Array.Empty<TableTreeNode>();

        /// <summary>
        /// Checks if the item is a group of tables, not a table.
        /// </summary>
        public bool IsGroup => _table == null;

        /// <summary>
        /// Adds a group or a table to the group.
        /// </summary>
        /// <param name="child">The item to add.</param>
        internal void AddChild(TableTreeNode child)
        {
            _children.Add(child);
        }

        /// <summary>
        /// Sorts the children of the group by name, and the children of its groups.
        /// </summary>
        internal void SortChildren()
        {
            if (_children == null)
                return;

            TableTree.Sort(_children);
            foreach (TableTreeNode child in _children)
                child.SortChildren();
        }

        #endregion

    }

    /// <summary>
    /// Builds the tree of the table list, which groups the tables by the <see cref="Separator"/> in their names.
    /// </summary>
    /// <remarks>
    /// The groups are compared ignoring case, like the sort of the tables: <i>Items/Weapons</i> and <i>items/Armors</i> are in the same
    /// group, named after the first of these tables in the order of the names. Groups and tables of a same group are sorted together by
    /// name.
    /// </remarks>
    internal static class TableTree
    {

        #region Fields

        /// <summary>
        /// The character that separates the groups in the names of the tables.
        /// </summary>
        public const char Separator = '/';

        #endregion


        #region Public API

        /// <summary>
        /// Builds the tree of tables.
        /// </summary>
        /// <param name="tables">The tables to show in the tree, usually the tables that match the search of the list.</param>
        /// <returns>Returns the items at the root of the tree, sorted by name. Groups without table are never created.</returns>
        public static List<TableTreeNode> Build(IEnumerable<TableDefinition> tables)
        {
            // The root is a group without name, never shown: its children are the root items
            TableTreeNode root = new TableTreeNode(string.Empty, string.Empty);

            // Sorted first, so the name of a group comes from the same table whatever the order of the given tables
            IEnumerable<TableDefinition> sortedTables = tables.OrderBy(i => i.Name, StringComparer.OrdinalIgnoreCase);
            foreach (TableDefinition table in sortedTables)
            {
                string[] parts = GetNameParts(table.Name);
                TableTreeNode group = root;
                for (int i = 0; i < parts.Length - 1; i++)
                    group = GetOrAddGroup(group, parts[i]);

                group.AddChild(new TableTreeNode(parts[parts.Length - 1], table));
            }

            root.SortChildren();
            return new List<TableTreeNode>(root.Children);
        }

        /// <summary>
        /// Splits the name of a table into the names of its groups, followed by its own name.
        /// </summary>
        /// <remarks>
        /// Each part is trimmed, and empty parts are skipped: <c>"Items / /Weapons/"</c> gives <c>["Items", "Weapons"]</c>. A name with
        /// no part left is kept as a single part, so the table still shows.
        /// </remarks>
        /// <param name="name">The name of the table.</param>
        /// <returns>Returns the parts of the name, at least one.</returns>
        public static string[] GetNameParts(string name)
        {
            string[] parts = (name ?? string.Empty)
                .Split(Separator)
                .Select(i => i.Trim())
                .Where(i => i.Length > 0)
                .ToArray();

            return parts.Length > 0 ? parts : new[] { name ?? string.Empty };
        }

        #endregion


        #region Private API

        /// <summary>
        /// Sorts the items of a group by name, ignoring case. A group comes before a table of the same name.
        /// </summary>
        /// <param name="items">The items to sort.</param>
        internal static void Sort(List<TableTreeNode> items)
        {
            // OrderBy() is stable, unlike List<T>.Sort(): two tables of the same name keep the order of their full names
            List<TableTreeNode> sortedItems = items
                .OrderBy(i => i.Name, StringComparer.OrdinalIgnoreCase)
                .ThenBy(i => i.IsGroup ? 0 : 1)
                .ToList();

            items.Clear();
            items.AddRange(sortedItems);
        }

        /// <summary>
        /// Gets a group of a parent group by its name, ignoring case, or adds it.
        /// </summary>
        /// <param name="parent">The group that contains the group.</param>
        /// <param name="name">The name of the group.</param>
        /// <returns>Returns the found or added group.</returns>
        private static TableTreeNode GetOrAddGroup(TableTreeNode parent, string name)
        {
            foreach (TableTreeNode child in parent.Children)
            {
                if (child.IsGroup && string.Equals(child.Name, name, StringComparison.OrdinalIgnoreCase))
                    return child;
            }

            string groupPath = string.IsNullOrEmpty(parent.GroupPath)
                ? name
                : $"{parent.GroupPath}{Separator}{name}";

            TableTreeNode group = new TableTreeNode(name, groupPath);
            parent.AddChild(group);
            return group;
        }

        #endregion

    }

}
