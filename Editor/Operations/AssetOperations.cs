using System;

using UnityEditor;

using Object = UnityEngine.Object;

namespace SideXP.Database.EditorOnly
{

    /// <summary>
    /// The operations of the Database window on the assets of a table. Sub-assets are handled as such, never as their main asset file.
    /// </summary>
    internal static class AssetOperations
    {

        #region Fields

        private const string RenameUndoName = "Rename Asset";

        #endregion


        #region Public API

        /// <summary>
        /// Renames an asset. A main asset is renamed with its file (<see cref="AssetDatabase.RenameAsset(string, string)"/>), which
        /// can't be undone, like a rename in the Project window. A sub-asset is renamed itself, as a single Undo step, and its file is
        /// saved.
        /// </summary>
        /// <remarks>
        /// The new name is trimmed, since file names can't end with spaces on every platform, and names with spaces around them are
        /// almost always a typing mistake.
        /// </remarks>
        /// <param name="asset">The asset to rename.</param>
        /// <param name="newName">The new name of the asset, without extension.</param>
        /// <param name="error">Outputs the reason why the asset has not been renamed, or null if it has been renamed or if the name
        /// doesn't change.</param>
        /// <returns>Returns true if the asset has been renamed. Returns false if the name doesn't change (with no error), or if the
        /// name is refused (blank name, name of another file in the same folder, invalid characters for a file name).</returns>
        /// <exception cref="ArgumentNullException">Thrown if the asset is null.</exception>
        public static bool TryRename(Object asset, string newName, out string error)
        {
            if (asset == null)
                throw new ArgumentNullException(nameof(asset));

            error = null;
            string trimmedName = newName != null ? newName.Trim() : string.Empty;
            if (trimmedName.Length == 0)
            {
                error = "The name can't be empty.";
                return false;
            }

            if (trimmedName == asset.name)
                return false;

            string path = AssetDatabase.GetAssetPath(asset);
            if (string.IsNullOrEmpty(path))
            {
                error = "The object is not saved in an asset file.";
                return false;
            }

            if (AssetDatabase.IsSubAsset(asset))
            {
                // Its own Undo step, so it's never merged with a change made just before
                Undo.IncrementCurrentGroup();
                Undo.SetCurrentGroupName(RenameUndoName);
                Undo.RecordObject(asset, RenameUndoName);
                asset.name = trimmedName;
                EditorUtility.SetDirty(asset);
                AssetDatabase.SaveAssetIfDirty(AssetDatabase.GUIDFromAssetPath(path));
                return true;
            }

            // Returns an empty string on success, and the reason of the failure otherwise
            string renameError = AssetDatabase.RenameAsset(path, trimmedName);
            if (!string.IsNullOrEmpty(renameError))
            {
                error = renameError;
                return false;
            }

            return true;
        }

        #endregion

    }

}
