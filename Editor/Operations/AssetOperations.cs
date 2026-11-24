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
        private const string DuplicateUndoName = "Duplicate Asset";
        private const string DeleteUndoName = "Delete Asset";

        private const string NotAnAssetError = "The object is not saved in an asset file.";

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
                error = NotAnAssetError;
                return false;
            }

            if (AssetDatabase.IsSubAsset(asset))
            {
                StartUndoGroup(RenameUndoName);
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

        /// <summary>
        /// Duplicates an asset. A main asset is copied with its file (<see cref="AssetDatabase.CopyAsset(string, string)"/>), next to
        /// the original, with its sub-assets, which can't be undone, like a duplicate in the Project window. A sub-asset is copied into
        /// the same file, as a single Undo step, and the file is saved.
        /// </summary>
        /// <remarks>
        /// The copy gets a unique name: the main asset a unique path in the folder, the sub-asset a name that no other object of its
        /// file has.
        /// </remarks>
        /// <param name="asset">The asset to duplicate.</param>
        /// <param name="copy">Outputs the created asset (the main asset of the new file for a main asset), or null if the asset has not
        /// been duplicated.</param>
        /// <param name="error">Outputs the reason why the asset has not been duplicated, or null if it has been duplicated.</param>
        /// <returns>Returns true if the asset has been duplicated.</returns>
        /// <exception cref="ArgumentNullException">Thrown if the asset is null.</exception>
        public static bool TryDuplicate(Object asset, out Object copy, out string error)
        {
            if (asset == null)
                throw new ArgumentNullException(nameof(asset));

            copy = null;
            error = null;
            string path = AssetDatabase.GetAssetPath(asset);
            if (string.IsNullOrEmpty(path))
            {
                error = NotAnAssetError;
                return false;
            }

            if (AssetDatabase.IsSubAsset(asset))
            {
                copy = Object.Instantiate(asset);
                copy.name = ObjectNames.GetUniqueName(GetObjectNames(path), asset.name);

                StartUndoGroup(DuplicateUndoName);
                AssetDatabase.AddObjectToAsset(copy, path);
                Undo.RegisterCreatedObjectUndo(copy, DuplicateUndoName);
                AssetDatabase.SaveAssetIfDirty(AssetDatabase.GUIDFromAssetPath(path));
                return true;
            }

            string copyPath = AssetDatabase.GenerateUniqueAssetPath(path);
            if (string.IsNullOrEmpty(copyPath) || !AssetDatabase.CopyAsset(path, copyPath))
            {
                error = $"The file {path} can't be copied.";
                return false;
            }

            copy = AssetDatabase.LoadMainAssetAtPath(copyPath);
            return copy != null;
        }

        /// <summary>
        /// Deletes an asset. A main asset is moved to the trash of the system with its file and its sub-assets
        /// (<see cref="AssetDatabase.MoveAssetToTrash(string)"/>), so it can be recovered from there, but not undone. A sub-asset is
        /// removed from its file, as a single Undo step, and the file is saved.
        /// </summary>
        /// <param name="asset">The asset to delete.</param>
        /// <param name="error">Outputs the reason why the asset has not been deleted, or null if it has been deleted.</param>
        /// <returns>Returns true if the asset has been deleted.</returns>
        /// <exception cref="ArgumentNullException">Thrown if the asset is null.</exception>
        public static bool TryDelete(Object asset, out string error)
        {
            if (asset == null)
                throw new ArgumentNullException(nameof(asset));

            error = null;
            string path = AssetDatabase.GetAssetPath(asset);
            if (string.IsNullOrEmpty(path))
            {
                error = NotAnAssetError;
                return false;
            }

            if (AssetDatabase.IsSubAsset(asset))
            {
                // Undo recreates the object in its file, unlike AssetDatabase.RemoveObjectFromAsset()
                StartUndoGroup(DeleteUndoName);
                Undo.DestroyObjectImmediate(asset);

                Object mainAsset = AssetDatabase.LoadMainAssetAtPath(path);
                if (mainAsset != null)
                    EditorUtility.SetDirty(mainAsset);

                AssetDatabase.SaveAssetIfDirty(AssetDatabase.GUIDFromAssetPath(path));
                return true;
            }

            if (!AssetDatabase.MoveAssetToTrash(path))
            {
                error = $"The file {path} can't be moved to the trash.";
                return false;
            }

            return true;
        }

        /// <summary>
        /// Counts the sub-assets of the file of an asset that are visible in the Project window.
        /// </summary>
        /// <param name="asset">An asset of the file, main asset or sub-asset.</param>
        /// <returns>Returns the number of visible sub-assets of the file, or 0 if the object is not saved in an asset file.</returns>
        public static int CountSubAssets(Object asset)
        {
            string path = asset != null ? AssetDatabase.GetAssetPath(asset) : null;
            return !string.IsNullOrEmpty(path)
                ? AssetDatabase.LoadAllAssetRepresentationsAtPath(path).Length
                : 0;
        }

        #endregion


        #region Private API

        /// <summary>
        /// Starts a new Undo group, so the next recorded changes form their own Undo step, never merged with a change made just before.
        /// </summary>
        /// <param name="undoName">The name of the Undo step, shown in the Edit menu.</param>
        private static void StartUndoGroup(string undoName)
        {
            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName(undoName);
        }

        /// <summary>
        /// Gets the names of all the objects of a file, hidden ones included.
        /// </summary>
        /// <param name="path">The path of the file.</param>
        /// <returns>Returns the names of the objects of the file.</returns>
        private static string[] GetObjectNames(string path)
        {
            Object[] objects = AssetDatabase.LoadAllAssetsAtPath(path);
            string[] names = new string[objects.Length];
            for (int i = 0; i < objects.Length; i++)
                names[i] = objects[i] != null ? objects[i].name : string.Empty;

            return names;
        }

        #endregion

    }

}
