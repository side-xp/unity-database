using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

using SideXP.Core.EditorOnly;

using Object = UnityEngine.Object;

namespace SideXP.Database.EditorOnly
{

    /// <summary>
    /// Provides the assets of a type found in the project, or in the folders of a scope, including the assets of its derived types
    /// and sub-assets.
    /// </summary>
    /// <remarks>
    /// The assets are loaded with <see cref="ObjectUtility.LoadAllAssetsOfType(Type, string[])"/> of the core lib: they are ordered by
    /// path, and sub-assets come after their main asset, ordered by name, like in the Project window. Sub-assets hidden from the
    /// Project window (<see cref="HideFlags.HideInHierarchy"/>) are skipped. The assets outside of the scope are never loaded.
    /// </remarks>
    internal class ProjectAssetSource : IAssetSource
    {

        #region Fields

        /// <summary>
        /// The type of the assets to provide.
        /// </summary>
        private Type _assetType = null;

        /// <summary>
        /// The folders the assets are loaded from, subfolders included. The folders that don't exist are ignored: if none of them
        /// exists, no asset is loaded.
        /// </summary>
        private FolderScope _scope = FolderScope.WholeProject;

        #endregion


        #region Lifecycle

        /// <inheritdoc cref="ProjectAssetSource"/>
        /// <param name="assetType"><inheritdoc cref="_assetType" path="/summary"/></param>
        /// <exception cref="ArgumentException">Thrown if the type doesn't derive from <see cref="ScriptableObject"/>.</exception>
        public ProjectAssetSource(Type assetType)
        {
            if (assetType == null || !typeof(ScriptableObject).IsAssignableFrom(assetType))
                throw new ArgumentException($"The type of a table must derive from {nameof(ScriptableObject)}.", nameof(assetType));

            _assetType = assetType;
        }

        #endregion


        #region Public API

        /// <inheritdoc cref="_assetType"/>
        public Type AssetType => _assetType;

        /// <summary>
        /// The folders the assets are loaded from. Changing it applies to the next loads. Setting null is the whole project.
        /// </summary>
        public FolderScope Scope
        {
            get => _scope;
            set => _scope = value ?? FolderScope.WholeProject;
        }

        /// <inheritdoc/>
        public IReadOnlyList<Object> LoadAssets()
        {
            string[] folders = _scope.IsWholeProject ? null : _scope.Folders.ToArray();
            return ObjectUtility.LoadAllAssetsOfType(_assetType, folders);
        }

        #endregion

    }

}
