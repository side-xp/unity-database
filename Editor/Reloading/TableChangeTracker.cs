using System;
using System.Collections.Generic;

using UnityEditor;

using Object = UnityEngine.Object;

namespace SideXP.Database.EditorOnly
{

    /// <summary>
    /// Tracks the changes of the assets of a table, and marks the outdated stages of its pipeline.
    /// </summary>
    /// <remarks>
    /// Two kinds of changes are tracked:
    /// <list type="bullet">
    /// <item>Imports, deletions and moves (<see cref="DatabaseAssetPostprocessor"/>): if they concern the table, its source is
    /// invalidated. A file that has no row concerns the table only if it's in the folder scope of the table (see
    /// <see cref="Scope"/>).</item>
    /// <item>Changes that don't go through an import (<see cref="ObjectChangeEvents"/>): edits in the Inspector, Undo and redo, scripts
    /// that record their changes with <see cref="Undo.RecordObject(Object, string)"/>. The rows of the changed files are invalidated,
    /// or the source if assets have been created or destroyed in a file of the table.</item>
    /// </list>
    /// The tracker never updates the pipeline itself: it raises <see cref="Changed"/>, so its owner updates the pipeline later, outside
    /// of the postprocess.
    /// </remarks>
    internal class TableChangeTracker
    {

        #region Fields

        /// <summary>
        /// The type of the table, used to check if a new file contains an asset of the table.
        /// </summary>
        private Type _tableType = null;

        /// <summary>
        /// The pipeline of the table.
        /// </summary>
        private TablePipeline _pipeline = null;

        /// <summary>
        /// The folders the table is limited to: a new file outside of them doesn't concern the table.
        /// </summary>
        private FolderScope _scope = FolderScope.WholeProject;

        /// <summary>
        /// The assets of the rows, mapped by the path of their file. Main assets and sub-assets of the same file share an entry.
        /// </summary>
        /// <remarks>
        /// The paths are stored, since the path of a deleted asset can't be found anymore once it's deleted.
        /// </remarks>
        private Dictionary<string, List<Object>> _assetsByPath = new Dictionary<string, List<Object>>();

        /// <summary>
        /// The version of the source of the pipeline when <see cref="_assetsByPath"/> was built, or -1 if it has never been built.
        /// </summary>
        private int _indexedSourceVersion = -1;

        /// <summary>
        /// Checks if the tracker listens to the changes.
        /// </summary>
        private bool _isStarted = false;

        /// <summary>
        /// Called when stages of the pipeline have been invalidated, so the pipeline must be updated.
        /// </summary>
        public event Action Changed;

        #endregion


        #region Lifecycle

        /// <inheritdoc cref="TableChangeTracker"/>
        /// <param name="tableType"><inheritdoc cref="_tableType" path="/summary"/></param>
        /// <param name="pipeline"><inheritdoc cref="_pipeline" path="/summary"/></param>
        public TableChangeTracker(Type tableType, TablePipeline pipeline)
        {
            _tableType = tableType;
            _pipeline = pipeline;
        }

        #endregion


        #region Public API

        /// <summary>
        /// The folders the table is limited to. Set it with the scope of the source of the pipeline. Setting null is the whole
        /// project.
        /// </summary>
        public FolderScope Scope
        {
            get => _scope;
            set => _scope = value ?? FolderScope.WholeProject;
        }

        /// <summary>
        /// Starts listening to the changes. Does nothing if the tracker is already started.
        /// </summary>
        public void Start()
        {
            if (_isStarted)
                return;

            _isStarted = true;
            DatabaseAssetPostprocessor.AssetsChanged += OnAssetsChanged;
            ObjectChangeEvents.changesPublished += OnChangesPublished;
        }

        /// <summary>
        /// Stops listening to the changes. Does nothing if the tracker is not started.
        /// </summary>
        public void Stop()
        {
            if (!_isStarted)
                return;

            _isStarted = false;
            DatabaseAssetPostprocessor.AssetsChanged -= OnAssetsChanged;
            ObjectChangeEvents.changesPublished -= OnChangesPublished;
        }

        /// <summary>
        /// Indexes the files of the rows again if the source of the pipeline has been loaded since the last call. Call it after each
        /// update of the pipeline.
        /// </summary>
        public void UpdateIndex()
        {
            if (_indexedSourceVersion == _pipeline.SourceVersion)
                return;

            _indexedSourceVersion = _pipeline.SourceVersion;
            _assetsByPath.Clear();
            foreach (TableRow row in _pipeline.Rows)
            {
                string path = AssetDatabase.GetAssetPath(row.Asset);
                if (string.IsNullOrEmpty(path))
                    continue;

                if (!_assetsByPath.TryGetValue(path, out List<Object> assets))
                {
                    assets = new List<Object>();
                    _assetsByPath.Add(path, assets);
                }
                assets.Add(row.Asset);
            }
        }

        #endregion


        #region Private API

        /// <summary>
        /// Checks if imports, deletions or moves concern the table.
        /// </summary>
        /// <remarks>
        /// Only the type of the main asset of a new file is checked, since checking its sub-assets would load it. So a sub-asset of the
        /// table added to a file that has no row yet, and whose main asset is of another type, is not detected. The <i>Refresh</i>
        /// button covers that case.
        /// <para>
        /// Internal instead of private, so the tests can check the changes without importing assets.
        /// </para>
        /// </remarks>
        /// <param name="changes">The changed paths.</param>
        /// <returns>Returns true if at least one path concerns the table.</returns>
        internal bool Concerns(AssetChanges changes)
        {
            // A deleted or moved file concerns the table if it had rows, which also covers the folders, since Unity reports each file
            // they contain
            foreach (string path in changes.Deleted)
            {
                if (_assetsByPath.ContainsKey(path))
                    return true;
            }

            foreach (string path in changes.MovedFrom)
            {
                if (_assetsByPath.ContainsKey(path))
                    return true;
            }

            // A file of the table moved into the folder scope from outside of it had no row
            foreach (string path in changes.Moved)
            {
                if (IsTableFile(path))
                    return true;
            }

            // An imported file concerns the table if it had rows (its sub-assets may have changed) or if it's an asset of the table
            foreach (string path in changes.Imported)
            {
                if (_assetsByPath.ContainsKey(path) || IsTableFile(path))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Checks if the main asset of a file is an asset of the table, in the folder scope, without loading it.
        /// </summary>
        /// <param name="path">The path of the file.</param>
        /// <returns>
        /// Returns true if the file is in the folder scope, and its main asset is of the table type or one of its derived types.
        /// </returns>
        private bool IsTableFile(string path)
        {
            // The scope is checked first, since it only compares texts
            if (string.IsNullOrEmpty(path) || !_scope.Contains(path))
                return false;

            Type mainAssetType = AssetDatabase.GetMainAssetTypeAtPath(path);
            return mainAssetType != null && _tableType.IsAssignableFrom(mainAssetType);
        }

        /// <summary>
        /// Invalidates the rows of the assets of a file.
        /// </summary>
        /// <param name="path">The path of the file.</param>
        /// <returns>Returns true if the file has rows in the table.</returns>
        private bool InvalidateRowsOfFile(string path)
        {
            if (string.IsNullOrEmpty(path) || !_assetsByPath.TryGetValue(path, out List<Object> assets))
                return false;

            foreach (Object asset in assets)
                _pipeline.InvalidateRow(asset);

            return true;
        }

        /// <summary>
        /// Called after assets have been imported, deleted or moved.
        /// </summary>
        /// <param name="changes">The changed paths.</param>
        private void OnAssetsChanged(AssetChanges changes)
        {
            if (!Concerns(changes))
                return;

            _pipeline.InvalidateSource();
            Changed?.Invoke();
        }

        /// <summary>
        /// Called when changes of objects have been recorded, for example after an edit in the Inspector, or Undo and redo.
        /// </summary>
        /// <remarks>
        /// Changed objects are found by the GUID of their file instead of their instance ID, since instance IDs are replaced by entity
        /// IDs (<c>entityId</c>) in recent versions of Unity, which don't exist in the oldest supported version. As a result, editing a
        /// sub-asset invalidates the rows of all the assets of its file, which only costs a few more reads.
        /// </remarks>
        /// <param name="stream">The recorded changes.</param>
        private void OnChangesPublished(ref ObjectChangeEventStream stream)
        {
            bool hasRowChanges = false;
            bool hasSourceChanges = false;
            for (int i = 0; i < stream.length; i++)
            {
                switch (stream.GetEventType(i))
                {
                    case ObjectChangeKind.ChangeAssetObjectProperties:
                        stream.GetChangeAssetObjectPropertiesEvent(i, out ChangeAssetObjectPropertiesEventArgs changeArgs);
                        hasRowChanges |= InvalidateRowsOfFile(AssetDatabase.GUIDToAssetPath(changeArgs.guid.ToString()));
                        break;

                    // Assets created or destroyed in a file of the table, for example sub-assets added or removed by a script
                    case ObjectChangeKind.CreateAssetObject:
                        stream.GetCreateAssetObjectEvent(i, out CreateAssetObjectEventArgs createArgs);
                        string createdPath = AssetDatabase.GUIDToAssetPath(createArgs.guid.ToString());
                        hasSourceChanges |= _assetsByPath.ContainsKey(createdPath) || IsTableFile(createdPath);
                        break;

                    case ObjectChangeKind.DestroyAssetObject:
                        stream.GetDestroyAssetObjectEvent(i, out DestroyAssetObjectEventArgs destroyArgs);
                        string destroyedPath = AssetDatabase.GUIDToAssetPath(destroyArgs.guid.ToString());
                        hasSourceChanges |= !string.IsNullOrEmpty(destroyedPath) && _assetsByPath.ContainsKey(destroyedPath);
                        break;
                }
            }

            if (hasSourceChanges)
                _pipeline.InvalidateSource();

            if (hasRowChanges || hasSourceChanges)
                Changed?.Invoke();
        }

        #endregion

    }

}
