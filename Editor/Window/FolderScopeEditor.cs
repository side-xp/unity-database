using System;
using System.Collections.Generic;
using System.IO;

using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;

using Object = UnityEngine.Object;

namespace SideXP.Database.EditorOnly
{

    /// <summary>
    /// Edits the folders of a folder scope (see <see cref="FolderScope"/>): lists them, removes them, adds them with the folder dialog
    /// of the system or by dropping folders from the Project window.
    /// </summary>
    /// <remarks>
    /// The editor raises <see cref="Changed"/> with the new folders after each change, so its owner applies the scope right away. A
    /// folder that doesn't exist anymore stays in the list, marked, since the scope ignores it: the user can remove it, or restore the
    /// folder.
    /// </remarks>
    internal class FolderScopeEditor : VisualElement
    {

        #region Fields

        private const string EditorClass = "database-folder-scope";
        private const string DragOverClass = "database-folder-scope--drag-over";
        private const string FolderClass = "database-folder-scope__folder";
        private const string FolderPathClass = "database-folder-scope__path";
        private const string FolderMissingClass = "database-folder-scope__path--missing";
        private const string HintClass = "database-folder-scope__hint";
        private const string RemoveButtonClass = "database-filter-editor__remove";

        private const string WholeProjectText = "No folder: the table shows the assets of the whole project.";
        private const string DropHintText = "Drop folders here from the Project window.";
        private const string PingTooltip = "Click to show the folder in the Project window.";
        private const string MissingTooltip = "This folder doesn't exist: it's ignored. Remove it, or restore the folder.";
        private const string RemoveTooltip = "Removes this folder from the scope.";
        private const string DialogTitle = "Add Folder to the Scope";
        private const string NotProjectFolderMessage = "The folder must be a folder of the project: in Assets, or in a package " +
            "embedded in the Packages folder.";
        private const string DialogOkText = "OK";

        /// <summary>
        /// The folder in which the folder dialog opens when the scope has no folder.
        /// </summary>
        private const string DefaultDialogFolder = "Assets";

        /// <summary>
        /// The folders of the scope, normalized, in the order they were added.
        /// </summary>
        private List<string> _folders = new List<string>();

        /// <summary>
        /// The element that lists the folders.
        /// </summary>
        private VisualElement _list = null;

        /// <summary>
        /// Called after each change of the folders, with the new folders.
        /// </summary>
        public event Action<IReadOnlyList<string>> Changed;

        #endregion


        #region Lifecycle

        /// <inheritdoc cref="FolderScopeEditor"/>
        public FolderScopeEditor()
        {
            AddToClassList(EditorClass);

            _list = new VisualElement();
            Add(_list);

            Label dropHint = new Label(DropHintText);
            dropHint.AddToClassList(HintClass);
            Add(dropHint);

            // The whole editor is the target of the drops, so the folders can be dropped on the list or on the hint
            RegisterCallback<DragUpdatedEvent>(OnDragUpdated);
            RegisterCallback<DragPerformEvent>(OnDragPerform);
            RegisterCallback<DragLeaveEvent>(evt => RemoveFromClassList(DragOverClass));
            RegisterCallback<DragExitedEvent>(evt => RemoveFromClassList(DragOverClass));

            Rebuild();
        }

        #endregion


        #region Public API

        /// <inheritdoc cref="_folders"/>
        public IReadOnlyList<string> Folders => _folders;

        /// <summary>
        /// Edits the folders of a scope.
        /// </summary>
        /// <param name="folders">The folders of the scope, or null for the whole project.</param>
        public void SetFolders(IEnumerable<string> folders)
        {
            _folders = folders != null ? new List<string>(folders) : new List<string>();
            Rebuild();
        }

        /// <summary>
        /// Opens the folder dialog of the system, and adds the picked folder to the scope. Refused with a message if the folder is not
        /// a folder of the project.
        /// </summary>
        public void AddFolderFromDialog()
        {
            string projectPath = GetProjectPath();
            string startFolder = _folders.Count > 0 && AssetDatabase.IsValidFolder(_folders[0]) ? _folders[0] : DefaultDialogFolder;
            string absolutePath = EditorUtility.OpenFolderPanel(DialogTitle, Path.Combine(projectPath, startFolder), string.Empty);

            // Empty if the dialog has been cancelled
            if (string.IsNullOrEmpty(absolutePath))
                return;

            if (!FolderScope.TryGetProjectFolder(absolutePath, projectPath, out string folder) || !AssetDatabase.IsValidFolder(folder))
            {
                EditorUtility.DisplayDialog(DialogTitle, NotProjectFolderMessage, DialogOkText);
                return;
            }

            AddFolders(new[] { folder });
        }

        /// <summary>
        /// Removes all the folders, so the scope is the whole project.
        /// </summary>
        public void ClearFolders()
        {
            if (_folders.Count == 0)
                return;

            _folders.Clear();
            NotifyChanged();
        }

        #endregion


        #region Private API

        /// <summary>
        /// Creates the elements of the folders again.
        /// </summary>
        private void Rebuild()
        {
            _list.Clear();
            if (_folders.Count == 0)
            {
                Label wholeProjectLabel = new Label(WholeProjectText);
                wholeProjectLabel.AddToClassList(HintClass);
                _list.Add(wholeProjectLabel);
                return;
            }

            foreach (string folder in _folders)
                _list.Add(CreateFolderElement(folder));
        }

        /// <summary>
        /// Creates the element of a folder: its path, which pings the folder when clicked, and the button that removes it.
        /// </summary>
        /// <param name="folder">The path of the folder.</param>
        /// <returns>Returns the created element.</returns>
        private VisualElement CreateFolderElement(string folder)
        {
            VisualElement element = new VisualElement();
            element.AddToClassList(FolderClass);

            bool exists = AssetDatabase.IsValidFolder(folder);
            string tooltip = exists ? PingTooltip : MissingTooltip;
            Label pathLabel = new Label(folder) { tooltip = $"{folder}\n{tooltip}" };
            pathLabel.AddToClassList(FolderPathClass);
            pathLabel.EnableInClassList(FolderMissingClass, !exists);
            if (exists)
                pathLabel.AddManipulator(new Clickable(() => PingFolder(folder)));

            element.Add(pathLabel);

            Button removeButton = new Button(() => RemoveFolder(folder)) { tooltip = RemoveTooltip };
            DatabaseElements.SetIcon(removeButton, DatabaseIcons.Remove);
            removeButton.AddToClassList(RemoveButtonClass);
            element.Add(removeButton);
            return element;
        }

        /// <summary>
        /// Adds folders to the scope. The folders already in the scope are skipped.
        /// </summary>
        /// <param name="folders">The paths of the folders, relative to the project.</param>
        private void AddFolders(IEnumerable<string> folders)
        {
            bool hasChanged = false;
            foreach (string folder in folders)
            {
                string normalizedFolder = FolderScope.NormalizeFolder(folder);
                if (normalizedFolder.Length == 0 || Contains(normalizedFolder))
                    continue;

                _folders.Add(normalizedFolder);
                hasChanged = true;
            }

            if (hasChanged)
                NotifyChanged();
        }

        /// <summary>
        /// Removes a folder from the scope.
        /// </summary>
        /// <param name="folder">The path of the folder.</param>
        private void RemoveFolder(string folder)
        {
            if (_folders.Remove(folder))
                NotifyChanged();
        }

        /// <summary>
        /// Checks if a folder is in the scope, ignoring case like the scope does.
        /// </summary>
        /// <param name="folder">The normalized path of the folder.</param>
        /// <returns>Returns true if the folder is in the list.</returns>
        private bool Contains(string folder)
        {
            return _folders.Exists(item => string.Equals(item, folder, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Shows a folder in the Project window.
        /// </summary>
        /// <param name="folder">The path of the folder.</param>
        private static void PingFolder(string folder)
        {
            Object folderAsset = AssetDatabase.LoadAssetAtPath<DefaultAsset>(folder);
            if (folderAsset != null)
                EditorGUIUtility.PingObject(folderAsset);
        }

        /// <summary>
        /// Gets the folders among the paths of a drag and drop.
        /// </summary>
        /// <returns>Returns the dragged folders, relative to the project.</returns>
        private static List<string> GetDraggedFolders()
        {
            List<string> folders = new List<string>();
            if (DragAndDrop.paths == null)
                return folders;

            foreach (string path in DragAndDrop.paths)
            {
                if (AssetDatabase.IsValidFolder(path))
                    folders.Add(path);
            }

            return folders;
        }

        /// <summary>
        /// Called while something is dragged over the editor. Accepts the drag if it contains folders of the project.
        /// </summary>
        /// <param name="evt">The emitted event.</param>
        private void OnDragUpdated(DragUpdatedEvent evt)
        {
            bool hasFolders = GetDraggedFolders().Count > 0;
            DragAndDrop.visualMode = hasFolders ? DragAndDropVisualMode.Copy : DragAndDropVisualMode.Rejected;
            EnableInClassList(DragOverClass, hasFolders);
        }

        /// <summary>
        /// Called when something is dropped on the editor. Adds the dropped folders to the scope.
        /// </summary>
        /// <param name="evt">The emitted event.</param>
        private void OnDragPerform(DragPerformEvent evt)
        {
            RemoveFromClassList(DragOverClass);
            List<string> folders = GetDraggedFolders();
            if (folders.Count == 0)
                return;

            DragAndDrop.AcceptDrag();
            AddFolders(folders);
        }

        /// <summary>
        /// Gets the absolute path of the project folder, which contains <c>Assets</c>.
        /// </summary>
        /// <returns>Returns the path of the project folder.</returns>
        private static string GetProjectPath()
        {
            return Path.GetDirectoryName(Application.dataPath);
        }

        /// <summary>
        /// Shows the folders again, and notifies the change.
        /// </summary>
        private void NotifyChanged()
        {
            Rebuild();
            Changed?.Invoke(_folders);
        }

        #endregion

    }

}
