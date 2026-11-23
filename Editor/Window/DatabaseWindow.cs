using System;

using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;

using SideXP.Core.EditorOnly;

namespace SideXP.Database.EditorOnly
{

    /// <summary>
    /// Editor window that displays assets as tables, and lets you view and edit their values like in a spreadsheet.
    /// </summary>
    /// <remarks>
    /// The window shows either the list of the tables, or an open table. The open table and the search text of the list are
    /// remembered across domain reloads and editor sessions.
    /// </remarks>
    public class DatabaseWindow : EditorWindow
    {

        #region Fields

        private const string WindowTitle = "Database";
        private const string MenuItem = EditorConstants.EditorWindowMenu + "/Database";
        private const string StyleSheetPath = DatabaseConstants.WindowFolderPath + "DatabaseWindow.uss";

        /// <summary>
        /// The identifier of the open table, or null if the table list is shown.
        /// </summary>
        [SerializeField]
        private string _openTableId = null;

        /// <summary>
        /// The text of the search field of the table list.
        /// </summary>
        [SerializeField]
        private string _tableSearch = string.Empty;

        /// <summary>
        /// The page that lists the tables. Null until the UI is built.
        /// </summary>
        private TableListPage _tableListPage = null;

        /// <summary>
        /// The page that displays the open table. Null until the UI is built.
        /// </summary>
        private TablePage _tablePage = null;

        #endregion


        #region Lifecycle

        /// <summary>
        /// Builds the UI of the window, and shows the page of the open table, or the table list if there is none.
        /// </summary>
        private void CreateGUI()
        {
            StyleSheet styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(StyleSheetPath);
            if (styleSheet != null)
                rootVisualElement.styleSheets.Add(styleSheet);
            else
                Debug.LogWarning($"{DatabaseConstants.LogPrefix}The style sheet of the window can't be found at \"{StyleSheetPath}\".");

            _tableListPage = new TableListPage(TableRegistry.Tables, _tableSearch);
            _tableListPage.TableChosen += OpenTable;
            _tableListPage.SearchChanged += search => _tableSearch = search;
            rootVisualElement.Add(_tableListPage);

            _tablePage = new TablePage();
            _tablePage.BackClicked += ShowTableList;
            rootVisualElement.Add(_tablePage);

            RefreshPage();
        }

        #endregion


        #region Public API

        /// <summary>
        /// Opens a Database window, or focuses the one already open.
        /// </summary>
        /// <returns>Returns the opened window.</returns>
        [MenuItem(MenuItem)]
        public static DatabaseWindow Open()
        {
            DatabaseWindow window = GetWindow<DatabaseWindow>(false, WindowTitle, true);
            window.Show();
            return window;
        }

        /// <summary>
        /// Opens a Database window on the table of a given type, or focuses the one already open and opens the table in it.
        /// </summary>
        /// <param name="tableType">The type of the table to open. If it's not a table (including a type derived from a table that is
        /// not inherited), a warning is logged and the table list is shown.</param>
        /// <returns>Returns the opened window.</returns>
        /// <exception cref="ArgumentNullException">Thrown if the type is null.</exception>
        public static DatabaseWindow Open(Type tableType)
        {
            if (tableType == null)
                throw new ArgumentNullException(nameof(tableType));

            DatabaseWindow window = Open();
            if (TableRegistry.TryGet(tableType, out TableDefinition table))
            {
                window.OpenTable(table);
            }
            else
            {
                Debug.LogWarning($"{DatabaseConstants.LogPrefix}{tableType} is not a table: the table list is shown instead.");
                window.ShowTableList();
            }

            return window;
        }

        #endregion


        #region Navigation

        /// <summary>
        /// Shows the page of a table.
        /// </summary>
        /// <param name="table">The table to open.</param>
        private void OpenTable(TableDefinition table)
        {
            _openTableId = table.Id;
            RefreshPage();
        }

        /// <summary>
        /// Shows the table list.
        /// </summary>
        private void ShowTableList()
        {
            _openTableId = null;
            RefreshPage();
        }

        /// <summary>
        /// Shows the page of the open table, or the table list if there is none.
        /// </summary>
        /// <remarks>
        /// If the open table doesn't exist anymore (for example after its type has been renamed, or its <see cref="TableAttribute"/>
        /// removed), the table list is shown instead.
        /// </remarks>
        private void RefreshPage()
        {
            // The UI is not built yet: CreateGUI() will refresh the page
            if (_tableListPage == null)
                return;

            bool isTableOpen = TableRegistry.TryGet(_openTableId, out TableDefinition table);
            if (isTableOpen)
                _tablePage.SetTable(table);
            else
                _openTableId = null;

            _tablePage.SetDisplayed(isTableOpen);
            _tableListPage.SetDisplayed(!isTableOpen);
        }

        #endregion

    }

}
