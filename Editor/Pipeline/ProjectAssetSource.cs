using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEditor;

using Object = UnityEngine.Object;

namespace SideXP.Database.EditorOnly
{

    /// <summary>
    /// Provides the assets of a type found in the project, including the assets of its derived types and sub-assets.
    /// </summary>
    /// <remarks>
    /// Assets are ordered by path. Sub-assets come after their main asset, ordered by name, like in the Project window. Sub-assets
    /// hidden from the Project window (<see cref="HideFlags.HideInHierarchy"/>) are skipped.
    /// </remarks>
    internal class ProjectAssetSource : IAssetSource
    {

        #region Fields

        /// <summary>
        /// The type of the assets to provide.
        /// </summary>
        private Type _assetType = null;

        /// <summary>
        /// The search filters used to find the files that contain assets of the type. Null until they are needed.
        /// </summary>
        private List<string> _searchFilters = null;

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
            HashSet<string> paths = new HashSet<string>();
            foreach (string filter in GetSearchFilters())
            {
                foreach (string guid in AssetDatabase.FindAssets(filter))
                    paths.Add(AssetDatabase.GUIDToAssetPath(guid));
            }

            List<string> sortedPaths = new List<string>(paths);
            sortedPaths.Sort(StringComparer.OrdinalIgnoreCase);

            List<Object> assets = new List<Object>();
            List<Object> subAssets = new List<Object>();
            foreach (string path in sortedPaths)
            {
                Object mainAsset = AssetDatabase.LoadMainAssetAtPath(path);
                if (_assetType.IsInstanceOfType(mainAsset))
                    assets.Add(mainAsset);

                subAssets.Clear();
                foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(path))
                {
                    if (asset == mainAsset || !_assetType.IsInstanceOfType(asset))
                        continue;

                    if ((asset.hideFlags & HideFlags.HideInHierarchy) != 0)
                        continue;

                    subAssets.Add(asset);
                }

                subAssets.Sort((a, b) => string.Compare(a.name, b.name, StringComparison.OrdinalIgnoreCase));
                assets.AddRange(subAssets);
            }

            return assets;
        }

        #endregion


        #region Private API

        /// <summary>
        /// Gets the search filters used to find the files that contain assets of the type: one per concrete type among the type and its
        /// derived types.
        /// </summary>
        /// <remarks>
        /// Each concrete type is searched on its own, since the documentation doesn't say whether a type filter matches derived types.
        /// The filters only use type names, so the files found may contain assets of other types with the same name: the loaded assets
        /// are filtered by type afterwards.
        /// </remarks>
        /// <returns>Returns the search filters, built once.</returns>
        private List<string> GetSearchFilters()
        {
            if (_searchFilters != null)
                return _searchFilters;

            HashSet<string> typeNames = new HashSet<string>();
            if (IsConcrete(_assetType))
                typeNames.Add(_assetType.Name);

            foreach (Type derivedType in TypeCache.GetTypesDerivedFrom(_assetType))
            {
                if (IsConcrete(derivedType))
                    typeNames.Add(derivedType.Name);
            }

            _searchFilters = new List<string>(typeNames.Count);
            foreach (string typeName in typeNames)
                _searchFilters.Add($"t:{typeName}");

            return _searchFilters;
        }

        /// <summary>
        /// Checks if a type can have assets.
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <returns>Returns true if the type is neither abstract nor an open generic type.</returns>
        private static bool IsConcrete(Type type)
        {
            return !type.IsAbstract && !type.ContainsGenericParameters;
        }

        #endregion

    }

}
