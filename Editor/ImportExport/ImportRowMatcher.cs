using System;
using System.Collections.Generic;

using UnityEditor;

using Object = UnityEngine.Object;

namespace SideXP.Database.EditorOnly
{

    /// <summary>
    /// Finds the asset of the table that a row of a file to import is about.
    /// </summary>
    /// <remarks>
    /// The text of the identity column is read first, as the identifier of an asset (see <see cref="AssetIds"/>), then as its path
    /// (<c>Assets/Items/Sword.asset</c>, followed by <c>#</c> and the name of a sub-asset, like the text of references). Without a
    /// match, the row is matched by the text of its <i>Asset Name</i> column. Texts are trimmed and compared ignoring case. A path or a
    /// name shared by several assets (sub-assets of the same name, assets of the same name in different folders) matches none of them,
    /// and makes the row ambiguous.
    /// </remarks>
    internal class ImportRowMatcher
    {

        #region Fields

        /// <summary>
        /// The assets by identifier.
        /// </summary>
        private Dictionary<string, Object> _assetsById = new Dictionary<string, Object>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// The assets by path, the paths shared by several assets excluded.
        /// </summary>
        private Dictionary<string, Object> _assetsByPath = new Dictionary<string, Object>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// The assets by name, the names shared by several assets excluded.
        /// </summary>
        private Dictionary<string, Object> _assetsByName = new Dictionary<string, Object>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// The paths shared by several assets.
        /// </summary>
        private HashSet<string> _ambiguousPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// The names shared by several assets.
        /// </summary>
        private HashSet<string> _ambiguousNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        #endregion


        #region Lifecycle

        /// <summary>
        /// Indexes the assets of the table.
        /// </summary>
        /// <param name="assets">All the assets of the table.</param>
        public ImportRowMatcher(IEnumerable<Object> assets)
        {
            foreach (Object asset in assets)
            {
                if (asset == null)
                    continue;

                string id = AssetIds.GetId(asset);
                if (id.Length > 0)
                    _assetsById[id] = asset;

                string path = GetPath(asset);
                if (path.Length > 0)
                    Add(_assetsByPath, _ambiguousPaths, path, asset);

                Add(_assetsByName, _ambiguousNames, asset.name, asset);
            }
        }

        #endregion


        #region Public API

        /// <summary>
        /// Finds the asset of a row.
        /// </summary>
        /// <param name="identityText">The text of the identity column of the row, or null if the file has none.</param>
        /// <param name="nameText">The text of the <i>Asset Name</i> column of the row, or null if the file has none.</param>
        /// <param name="asset">Outputs the asset of the row, or null if it matches none.</param>
        /// <param name="isAmbiguous">Outputs true if the row matches no asset because its path or its name is shared by several
        /// assets.</param>
        /// <returns>Returns how the row has been matched, or <see cref="ImportMatchKind.None"/>.</returns>
        public ImportMatchKind Match(string identityText, string nameText, out Object asset, out bool isAmbiguous)
        {
            isAmbiguous = false;
            string identity = (identityText ?? string.Empty).Trim();
            if (identity.Length > 0)
            {
                if (_assetsById.TryGetValue(identity, out asset))
                    return ImportMatchKind.Id;

                string path = identity.Replace('\\', '/');
                if (_assetsByPath.TryGetValue(path, out asset))
                    return ImportMatchKind.Path;

                isAmbiguous = _ambiguousPaths.Contains(path);
            }

            string name = (nameText ?? string.Empty).Trim();
            if (name.Length > 0)
            {
                if (_assetsByName.TryGetValue(name, out asset))
                {
                    isAmbiguous = false;
                    return ImportMatchKind.Name;
                }

                isAmbiguous |= _ambiguousNames.Contains(name);
            }

            asset = null;
            return ImportMatchKind.None;
        }

        /// <summary>
        /// Gets the path of an asset as the identity column can write it: the path of its file, followed by <c>#</c> and its name for
        /// a sub-asset (see <see cref="ObjectReferenceCellHandler.SubAssetSeparator"/>).
        /// </summary>
        /// <param name="asset">The asset of which to get the path.</param>
        /// <returns>Returns the path of the asset, or an empty text if it's not saved in an asset file.</returns>
        public static string GetPath(Object asset)
        {
            string path = AssetDatabase.GetAssetPath(asset);
            if (string.IsNullOrEmpty(path))
                return string.Empty;

            return AssetDatabase.IsMainAsset(asset)
                ? path
                : $"{path}{ObjectReferenceCellHandler.SubAssetSeparator}{asset.name}";
        }

        #endregion


        #region Private API

        /// <summary>
        /// Indexes an asset by a key, unless another asset has the same key: the key is then ambiguous, and indexes none of them.
        /// </summary>
        /// <param name="assets">The assets by key.</param>
        /// <param name="ambiguousKeys">The keys shared by several assets.</param>
        /// <param name="key">The key of the asset.</param>
        /// <param name="asset">The asset to index.</param>
        private static void Add(Dictionary<string, Object> assets, HashSet<string> ambiguousKeys, string key, Object asset)
        {
            if (ambiguousKeys.Contains(key))
                return;

            if (assets.Remove(key))
            {
                ambiguousKeys.Add(key);
                return;
            }

            assets.Add(key, asset);
        }

        #endregion

    }

}
