using System;
using System.Collections.Generic;

namespace SideXP.Database.EditorOnly
{

    /// <summary>
    /// The folders a table is limited to: only the assets of these folders and of their subfolders are rows of the table. Without
    /// folders, the scope is the whole project.
    /// </summary>
    /// <remarks>
    /// The folders are paths relative to the project (<c>Assets/Items</c>, or a folder of a package), compared ignoring case, like
    /// the asset database does. The scope doesn't check that its folders exist: the source loads nothing from a folder that doesn't
    /// exist (see <see cref="ProjectAssetSource"/>).
    /// </remarks>
    internal sealed class FolderScope
    {

        #region Fields

        /// <summary>
        /// The character between the folders of a path in the asset database.
        /// </summary>
        private const char Separator = '/';

        /// <summary>
        /// The scope of the whole project.
        /// </summary>
        public static readonly FolderScope WholeProject = new FolderScope(null);

        /// <summary>
        /// The folders of the scope, normalized (see <see cref="NormalizeFolder(string)"/>), without duplicates. Empty for the whole
        /// project.
        /// </summary>
        private string[] _folders = null;

        #endregion


        #region Lifecycle

        /// <inheritdoc cref="FolderScope"/>
        /// <param name="folders">
        /// The paths of the folders, relative to the project. Empty paths and duplicates are skipped. If null or empty, the scope is the
        /// whole project.
        /// </param>
        public FolderScope(IEnumerable<string> folders)
        {
            List<string> normalizedFolders = new List<string>();
            if (folders != null)
            {
                foreach (string folder in folders)
                {
                    string normalizedFolder = NormalizeFolder(folder);
                    if (normalizedFolder.Length > 0 && !normalizedFolders.Exists(item => IsSamePath(item, normalizedFolder)))
                        normalizedFolders.Add(normalizedFolder);
                }
            }

            _folders = normalizedFolders.ToArray();
        }

        #endregion


        #region Public API

        /// <inheritdoc cref="_folders"/>
        public IReadOnlyList<string> Folders => _folders;

        /// <summary>
        /// Checks if the scope is the whole project: it has no folder.
        /// </summary>
        public bool IsWholeProject => _folders.Length == 0;

        /// <summary>
        /// Checks if an asset is in the scope: in one of its folders, or in one of their subfolders.
        /// </summary>
        /// <param name="assetPath">The path of the asset, relative to the project.</param>
        /// <returns>Returns true if the asset is in the scope, always true for the whole project. False for an empty path.</returns>
        public bool Contains(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
                return false;

            if (IsWholeProject)
                return true;

            string path = NormalizeFolder(assetPath);
            foreach (string folder in _folders)
            {
                // The separator is checked, so "Assets/Items" doesn't contain "Assets/ItemsOld/Sword.asset"
                bool isInFolder = path.Length > folder.Length
                    && path[folder.Length] == Separator
                    && path.StartsWith(folder, StringComparison.OrdinalIgnoreCase);

                if (isInFolder)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Gets a path of folder in the format of the asset database: forward slashes, without spaces around it nor a slash at the end.
        /// </summary>
        /// <example>
        /// <c>NormalizeFolder(@"Assets\Items\")</c> returns <c>Assets/Items</c>.
        /// </example>
        /// <param name="folder">The path of the folder.</param>
        /// <returns>Returns the normalized path, or an empty string if the path is null or blank.</returns>
        public static string NormalizeFolder(string folder)
        {
            if (string.IsNullOrWhiteSpace(folder))
                return string.Empty;

            return folder.Trim().Replace('\\', Separator).TrimEnd(Separator);
        }

        /// <summary>
        /// Gets the path relative to the project of a folder given as an absolute path, for example by the folder dialog of the system.
        /// </summary>
        /// <remarks>
        /// Only checks that the folder is inside the project folder: check that the asset database knows it (for example with
        /// <c>AssetDatabase.IsValidFolder()</c>), since the project folder also contains folders that aren't assets (<c>Library</c>,
        /// <c>Temp</c>).
        /// </remarks>
        /// <example>
        /// With the project in <c>D:/Game</c>, <c>D:\Game\Assets\Items</c> gives <c>Assets/Items</c>.
        /// </example>
        /// <param name="absolutePath">The absolute path of the folder.</param>
        /// <param name="projectPath">The absolute path of the project folder, which contains <c>Assets</c>.</param>
        /// <param name="folder">Outputs the path of the folder relative to the project, or null if it's not in the project.</param>
        /// <returns>Returns true if the folder is in the project folder (not the project folder itself).</returns>
        public static bool TryGetProjectFolder(string absolutePath, string projectPath, out string folder)
        {
            folder = null;
            string path = NormalizeFolder(absolutePath);
            string root = NormalizeFolder(projectPath);
            if (path.Length == 0 || root.Length == 0)
                return false;

            bool isInProject = path.Length > root.Length
                && path[root.Length] == Separator
                && path.StartsWith(root, StringComparison.OrdinalIgnoreCase);

            if (!isInProject)
                return false;

            folder = path.Substring(root.Length + 1);
            return true;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return IsWholeProject ? "Whole project" : string.Join(", ", _folders);
        }

        #endregion


        #region Private API

        /// <summary>
        /// Checks if two normalized paths are the same, ignoring case.
        /// </summary>
        /// <param name="a">The first path.</param>
        /// <param name="b">The second path.</param>
        /// <returns>Returns true if the paths are the same.</returns>
        private static bool IsSamePath(string a, string b)
        {
            return string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
        }

        #endregion

    }

}
