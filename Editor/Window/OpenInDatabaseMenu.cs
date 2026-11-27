using System.Collections.Generic;

using UnityEngine;
using UnityEditor;

using Object = UnityEngine.Object;

namespace SideXP.Database.EditorOnly
{

    /// <summary>
    /// The <i>Open In Database</i> item of the context menu of the Project window (also in the <i>Assets</i> menu of the main menu
    /// bar): opens the table of the selected asset in a Database window, makes the asset the active cell, and scrolls to its row.
    /// </summary>
    /// <remarks>
    /// When several tables list the asset (the table of its type, and the tables of its base types), a menu lets the user choose one,
    /// the nearest table first. Unity has no public API to add menu items at runtime, so this menu opens after the item is clicked,
    /// at the position of the pointer. The button of the Inspector header does the same (see <see cref="InspectorHeaderButton"/>).
    /// </remarks>
    internal static class OpenInDatabaseMenu
    {

        #region Fields

        private const string MenuPath = "Assets/Open In Database";

        /// <summary>
        /// The character that replaces the <c>/</c> in the names of the tables shown in the menu. It looks like a slash, but the menu
        /// doesn't use it to create submenus, so the full names of the tables stay on a single level.
        /// </summary>
        private const char MenuSlash = '∕';

        #endregion


        #region Menu

        /// <summary>
        /// Checks if the <i>Open In Database</i> item is enabled: true if the selected object is an asset listed by at least one table.
        /// </summary>
        /// <returns>Returns true if the item is enabled.</returns>
        [MenuItem(MenuPath, true)]
        private static bool CanOpenSelection()
        {
            return TryGetSelectedAsset(out _, out _);
        }

        /// <summary>
        /// Opens the table of the selected asset, or lets the user choose the table first if several tables list the asset.
        /// </summary>
        /// <remarks>
        /// The menu of the tables needs the position of the pointer, which only exists when the item is clicked in a context menu. When
        /// it's clicked in the main menu bar, the nearest table is opened.
        /// </remarks>
        [MenuItem(MenuPath, false)]
        private static void OpenSelection()
        {
            if (TryGetSelectedAsset(out Object asset, out List<TableDefinition> tables))
                Open(asset, tables, null);
        }

        #endregion


        #region Public API

        /// <summary>
        /// Gets the tables that list an asset.
        /// </summary>
        /// <param name="asset">The asset, main asset or sub-asset.</param>
        /// <param name="tables">Outputs the tables that list the asset, the nearest first (see
        /// <see cref="TableRegistry.GetTablesListing(System.Type)"/>), or null if the object is not an asset.</param>
        /// <returns>Returns true if the object is an asset listed by at least one table.</returns>
        public static bool TryGetTables(Object asset, out List<TableDefinition> tables)
        {
            tables = null;
            // Objects of the scenes are not rows
            if (asset == null || !AssetDatabase.Contains(asset))
                return false;

            tables = TableRegistry.GetTablesListing(asset.GetType());
            return tables.Count > 0;
        }

        /// <summary>
        /// Opens the table of an asset in a Database window, and shows its row. If several tables list the asset, a menu lets the user
        /// choose the table first, the nearest first.
        /// </summary>
        /// <param name="asset">The asset of which to show the row.</param>
        /// <param name="tables">The tables that list the asset, the nearest first (see <see cref="TryGetTables"/>). Not empty.</param>
        /// <param name="menuRect">The area under which the menu of the tables opens, usually the clicked button, or null to open it at
        /// the position of the pointer. Without area and without the event of a click (an item clicked in the main menu bar), the
        /// nearest table is opened.</param>
        public static void Open(Object asset, List<TableDefinition> tables, Rect? menuRect)
        {
            // ShowAsContext() does nothing without the event of the click
            if (tables.Count == 1 || (!menuRect.HasValue && Event.current == null))
            {
                DatabaseWindow.Open(tables[0], asset);
                return;
            }

            GenericMenu menu = new GenericMenu();
            foreach (TableDefinition table in tables)
                menu.AddItem(new GUIContent(GetMenuText(table)), false, () => OpenTable(table, asset));

            if (menuRect.HasValue)
                menu.DropDown(menuRect.Value);
            else
                menu.ShowAsContext();
        }

        #endregion


        #region Private API

        /// <summary>
        /// Gets the selected asset, and the tables that list it.
        /// </summary>
        /// <remarks>
        /// Only the active object of the selection is used, which can be a sub-asset.
        /// </remarks>
        /// <param name="asset">Outputs the selected asset.</param>
        /// <param name="tables">Outputs the tables that list the asset, the nearest first.</param>
        /// <returns>Returns true if the selected object is an asset listed by at least one table.</returns>
        private static bool TryGetSelectedAsset(out Object asset, out List<TableDefinition> tables)
        {
            asset = Selection.activeObject;
            return TryGetTables(asset, out tables);
        }

        /// <summary>
        /// Opens a table chosen in the menu of the tables, and shows the row of an asset.
        /// </summary>
        /// <param name="table">The chosen table.</param>
        /// <param name="asset">The asset of which to show the row.</param>
        private static void OpenTable(TableDefinition table, Object asset)
        {
            // The asset may have been deleted while the menu was open
            if (asset != null)
                DatabaseWindow.Open(table, asset);
        }

        /// <summary>
        /// Gets the text of a table in the menu of the tables.
        /// </summary>
        /// <param name="table">The table to show in the menu.</param>
        /// <returns>Returns the full name of the table, with its <c>/</c> replaced so they don't create submenus.</returns>
        private static string GetMenuText(TableDefinition table)
        {
            return table.Name.Replace('/', MenuSlash);
        }

        #endregion

    }

}
