using System.Collections.Generic;

using UnityEngine;
using UnityEditor;

using SideXP.Core.EditorOnly;

using Object = UnityEngine.Object;

namespace SideXP.Database.EditorOnly
{

    /// <summary>
    /// The <i>Open In Database</i> button of the Inspector header, on the assets listed by a table: opens the table of the asset in a
    /// Database window and shows its row, like the item of the context menu of the Project window (see
    /// <see cref="OpenInDatabaseMenu"/>). When several tables list the asset, a menu under the button lets the user choose one.
    /// </summary>
    /// <remarks>
    /// Drawn after the default header of the editors (<see cref="Editor.finishedDefaultHeaderGUI"/>), on the right, with an icon that
    /// says if the button opens the table directly (<see cref="DatabaseIcons.OpenInDatabase"/>) or the menu of the tables
    /// (<see cref="DatabaseIcons.OpenInDatabaseWithChoice"/>), like the buttons that create assets. Only shown when a
    /// single object is inspected, since a row is shown for one asset. The tables only list scriptable objects, so the headers of the
    /// other objects (game objects, textures, materials...) are skipped before the table registry is read: otherwise, the first
    /// Inspector shown after a domain reload would build the registry, and log the issues of the declarations at an unexpected time.
    /// </remarks>
    [InitializeOnLoad]
    internal static class InspectorHeaderButton
    {

        #region Fields

        private const string ButtonText = "Open In Database";
        private const string SingleTableTooltip = "Opens the table of this asset in the Database window, and shows its row.";
        private const string SeveralTablesTooltip = "Opens a table of this asset in the Database window, and shows its row. Several " +
            "tables list this asset: choose the table first.";

        /// <summary>
        /// The size of the icon of the button, the size of the icons of the small buttons of the editor.
        /// </summary>
        private const EIconSize IconSize = EIconSize.Small;

        #endregion


        #region Lifecycle

        /// <summary>
        /// Draws the button after the header of the editors, from the load of the editor and after each domain reload.
        /// </summary>
        static InspectorHeaderButton()
        {
            Editor.finishedDefaultHeaderGUI += OnFinishedDefaultHeaderGUI;
        }

        #endregion


        #region Private API

        /// <summary>
        /// Called after the default header of an editor is drawn. Draws the button if the editor inspects a single asset listed by a
        /// table.
        /// </summary>
        /// <param name="editor">The editor whose header has been drawn.</param>
        private static void OnFinishedDefaultHeaderGUI(Editor editor)
        {
            if (editor.targets.Length != 1 || editor.target is not ScriptableObject asset)
                return;

            if (!OpenInDatabaseMenu.TryGetTables(asset, out List<TableDefinition> tables))
                return;

            bool hasChoice = tables.Count > 1;
            // Made again on each draw, since its icon is a placeholder until the icon is downloaded
            GUIContent content = EditorIcons.IconContent(
                hasChoice ? DatabaseIcons.OpenInDatabaseWithChoice : DatabaseIcons.OpenInDatabase,
                ButtonText,
                hasChoice ? SeveralTablesTooltip : SingleTableTooltip,
                size: IconSize);

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            // The icon is measured and drawn at its size on screen, not at the size of its texture
            using (new IconSizeScope(IconSize))
            {
                Rect buttonRect = GUILayoutUtility.GetRect(content, EditorStyles.miniButton, GUILayout.ExpandWidth(false));
                if (GUI.Button(buttonRect, content, EditorStyles.miniButton))
                {
                    // The menu of the tables needs the event of the click. A single table is opened after the GUI of the Inspector, so
                    // the Database window doesn't take the focus in the middle of it
                    if (hasChoice)
                        OpenInDatabaseMenu.Open(asset, tables, buttonRect);
                    else
                        EditorApplication.delayCall += () => OpenTable(asset, tables);
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// Opens the table of an asset after a click on the button.
        /// </summary>
        /// <param name="asset">The asset of which to show the row.</param>
        /// <param name="tables">The tables that list the asset.</param>
        private static void OpenTable(Object asset, List<TableDefinition> tables)
        {
            // The asset may have been deleted in between
            if (asset != null)
                OpenInDatabaseMenu.Open(asset, tables, null);
        }

        #endregion

    }

}
