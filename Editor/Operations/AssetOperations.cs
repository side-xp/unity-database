using System;
using System.Collections.Generic;
using System.IO;

using UnityEngine;
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

        /// <summary>
        /// The extension of the files of the assets created by the tool, with its dot.
        /// </summary>
        public const string AssetExtension = ".asset";

        /// <summary>
        /// The files of the sub-assets changed by the operations, by the Undo group of the operation, so they are saved again when
        /// the operation is undone or redone (see <see cref="StartSubAssetUndoGroup(string, string)"/>).
        /// </summary>
        private static Dictionary<int, string> s_subAssetFilesByUndoGroup = new Dictionary<int, string>();

        /// <summary>
        /// Checks if <see cref="Undo.undoRedoEvent"/> is listened to. Static events are cleared by domain reloads, like this flag.
        /// </summary>
        private static bool s_isListeningToUndo = false;

        #endregion


        #region Public API

        /// <summary>
        /// Gets the types of the assets that can be created in a table: the table type itself if it's concrete, then its concrete
        /// derived types, sorted by name.
        /// </summary>
        /// <param name="tableType">The type of the table.</param>
        /// <returns>Returns the types that can be created, or an empty list if there is none (for example an abstract type without
        /// concrete derived type).</returns>
        /// <exception cref="ArgumentNullException">Thrown if the type is null.</exception>
        public static List<Type> GetCreatableTypes(Type tableType)
        {
            if (tableType == null)
                throw new ArgumentNullException(nameof(tableType));

            List<Type> derivedTypes = new List<Type>();
            foreach (Type type in TypeCache.GetTypesDerivedFrom(tableType))
            {
                if (IsCreatable(type))
                    derivedTypes.Add(type);
            }

            derivedTypes.Sort((a, b) => EditorUtility.NaturalCompare(GetDisplayName(a), GetDisplayName(b)));

            List<Type> types = new List<Type>(derivedTypes.Count + 1);
            if (IsCreatable(tableType))
                types.Add(tableType);

            types.AddRange(derivedTypes);
            return types;
        }

        /// <summary>
        /// Checks if assets of a type can be created.
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <returns>Returns true if the type is a concrete type of <see cref="ScriptableObject"/>.</returns>
        public static bool IsCreatable(Type type)
        {
            return type != null
                && typeof(ScriptableObject).IsAssignableFrom(type)
                && !type.IsAbstract
                && !type.ContainsGenericParameters;
        }

        /// <summary>
        /// Gets the name of a type shown to the users, in the menus and the dialogs.
        /// </summary>
        /// <param name="type">The type of which to get the name.</param>
        /// <returns>Returns the nicified name of the type.</returns>
        public static string GetDisplayName(Type type)
        {
            return ObjectNames.NicifyVariableName(type.Name);
        }

        /// <summary>
        /// Creates an asset of a type, as a main asset in its own file. It can't be undone, like a creation in the Project window.
        /// </summary>
        /// <remarks>
        /// An existing file at the same path is replaced: confirm it with the user before (the save dialog of Unity does).
        /// </remarks>
        /// <param name="type">The type of the asset to create.</param>
        /// <param name="path">The path of the new file, from the project folder (<c>Assets/Items/Sword.asset</c>), in an existing
        /// folder, with the <see cref="AssetExtension"/> extension. The name of the file is the name of the asset.</param>
        /// <param name="asset">Outputs the created asset, or null if it has not been created.</param>
        /// <param name="error">Outputs the reason why the asset has not been created, or null if it has been created.</param>
        /// <returns>Returns true if the asset has been created.</returns>
        /// <exception cref="ArgumentNullException">Thrown if the type is null.</exception>
        public static bool TryCreate(Type type, string path, out Object asset, out string error)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            asset = null;
            error = null;
            if (!IsCreatable(type))
            {
                error = $"{type} is not a concrete type of {nameof(ScriptableObject)}.";
                return false;
            }

            if (string.IsNullOrEmpty(path) || !path.EndsWith(AssetExtension, StringComparison.OrdinalIgnoreCase))
            {
                error = $"The path of the asset must end with {AssetExtension}.";
                return false;
            }

            string folder = Path.GetDirectoryName(path)?.Replace('\\', '/');
            if (string.IsNullOrEmpty(folder) || !AssetDatabase.IsValidFolder(folder))
            {
                error = $"The folder of {path} is not a folder of the project.";
                return false;
            }

            ScriptableObject instance = ScriptableObject.CreateInstance(type);
            // Unity saves the script of an asset by its file, so a type declared in a file named after another type would be saved
            // with a missing script
            if (MonoScript.FromScriptableObject(instance) == null)
            {
                Object.DestroyImmediate(instance);
                error = $"{type} has no script file named after it, so Unity can't save its assets.";
                return false;
            }

            AssetDatabase.CreateAsset(instance, path);
            asset = instance;
            return true;
        }

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
                StartSubAssetUndoGroup(RenameUndoName, path);
                Undo.RecordObject(asset, RenameUndoName);
                asset.name = trimmedName;
                EditorUtility.SetDirty(asset);
                SaveFile(path);
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

                StartSubAssetUndoGroup(DuplicateUndoName, path);
                AssetDatabase.AddObjectToAsset(copy, path);
                Undo.RegisterCreatedObjectUndo(copy, DuplicateUndoName);
                SaveFile(path);
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
                StartSubAssetUndoGroup(DeleteUndoName, path);
                Undo.DestroyObjectImmediate(asset);
                SaveFile(path);
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
        /// Starts a new Undo group for an operation on a sub-asset, so the next recorded changes form their own Undo step, never merged
        /// with a change made just before. The file of the sub-asset is saved again when the step is undone or redone.
        /// </summary>
        /// <remarks>
        /// The operations save the file right away, but an Undo or a Redo only changes the objects in memory. Without a save, the file
        /// on disk keeps the state of the operation: the Project window doesn't list a restored sub-asset, and a reimport of the file
        /// reports an inconsistent result. The groups are known until the next domain reload: after it, undoing an earlier operation
        /// leaves the file to save with the project.
        /// </remarks>
        /// <param name="undoName">The name of the Undo step, shown in the Edit menu.</param>
        /// <param name="path">The path of the file of the sub-asset.</param>
        private static void StartSubAssetUndoGroup(string undoName, string path)
        {
            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName(undoName);
            s_subAssetFilesByUndoGroup[Undo.GetCurrentGroup()] = path;

            if (!s_isListeningToUndo)
            {
                Undo.undoRedoEvent += OnUndoRedo;
                s_isListeningToUndo = true;
            }
        }

        /// <summary>
        /// Called after any Undo or Redo. Saves the file of a sub-asset if the step comes from an operation on it.
        /// </summary>
        /// <param name="undo">The undone or redone step.</param>
        private static void OnUndoRedo(in UndoRedoInfo undo)
        {
            if (s_subAssetFilesByUndoGroup.TryGetValue(undo.undoGroup, out string path))
                SaveFile(path);
        }

        /// <summary>
        /// Writes a file with the objects it holds in memory, and imports it, so the Project window lists its sub-assets as they are.
        /// </summary>
        /// <remarks>
        /// The main asset is marked dirty, since adding, removing or restoring a sub-asset doesn't mark anything dirty.
        /// </remarks>
        /// <param name="path">The path of the file. Does nothing if the file doesn't exist anymore.</param>
        private static void SaveFile(string path)
        {
            Object mainAsset = AssetDatabase.LoadMainAssetAtPath(path);
            if (mainAsset == null)
                return;

            EditorUtility.SetDirty(mainAsset);
            AssetDatabase.SaveAssetIfDirty(AssetDatabase.GUIDFromAssetPath(path));
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
