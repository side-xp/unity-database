using UnityEditor;

using SideXP.Core.EditorOnly;

namespace SideXP.Database.EditorOnly
{

    /// <summary>
    /// Editor window that displays assets as tables, and lets you view and edit their values like in a spreadsheet.
    /// </summary>
    public class DatabaseWindow : EditorWindow
    {

        #region Fields

        private const string WindowTitle = "Database";
        private const string MenuItem = EditorConstants.EditorWindowMenu + "/Database";

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

        #endregion


        #region UI

        /// <summary>
        /// Builds the UI of the window.
        /// </summary>
        private void CreateGUI()
        {
            // @note The content of the window (table list and table view) is built in the next steps of the implementation.
        }

        #endregion

    }

}
