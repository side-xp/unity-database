using System;
using System.Collections.Generic;

using UnityEngine;

using SideXP.Core.EditorOnly;

using Object = UnityEngine.Object;

namespace SideXP.Database.EditorOnly
{

    /// <summary>
    /// Provides the assets of a type found in the project, including the assets of its derived types and sub-assets.
    /// </summary>
    /// <remarks>
    /// The assets are loaded with <see cref="ObjectUtility.LoadAllAssetsOfType(Type)"/> of the core lib: they are ordered by path, and
    /// sub-assets come after their main asset, ordered by name, like in the Project window. Sub-assets hidden from the Project window
    /// (<see cref="HideFlags.HideInHierarchy"/>) are skipped.
    /// </remarks>
    internal class ProjectAssetSource : IAssetSource
    {

        #region Fields

        /// <summary>
        /// The type of the assets to provide.
        /// </summary>
        private Type _assetType = null;

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

        /// <inheritdoc/>
        public IReadOnlyList<Object> LoadAssets()
        {
            return ObjectUtility.LoadAllAssetsOfType(_assetType);
        }

        #endregion

    }

}
