using System;

using UnityEditor;

namespace SideXP.Database.EditorOnly
{

    /// <summary>
    /// Collects the paths of the imported, deleted and moved assets, and notifies the open tables.
    /// </summary>
    /// <remarks>
    /// The listeners must only check the paths and mark what is outdated: loading assets during the postprocess is fragile, so the
    /// tables reload on the next editor update. This class never opens or focuses a window.
    /// </remarks>
    internal class DatabaseAssetPostprocessor : AssetPostprocessor
    {

        #region Fields

        /// <summary>
        /// Called after assets have been imported, deleted or moved.
        /// </summary>
        public static event Action<AssetChanges> AssetsChanged;

        #endregion


        #region Messages

        /// <summary>
        /// Called by Unity after assets have been imported, deleted or moved.
        /// </summary>
        /// <param name="importedAssets">The paths of the imported assets, created or changed.</param>
        /// <param name="deletedAssets">The paths of the deleted assets.</param>
        /// <param name="movedAssets">The new paths of the moved or renamed assets.</param>
        /// <param name="movedFromAssetPaths">The previous paths of the moved or renamed assets.</param>
        /// <param name="didDomainReload">
        /// Checks if a domain reload happened during the import. Not used: the windows rebuild their UI after a domain reload anyway.
        /// </param>
        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets,
            string[] movedFromAssetPaths, bool didDomainReload)
        {
            if (AssetsChanged == null)
                return;

            AssetChanges changes = new AssetChanges(importedAssets, deletedAssets, movedAssets, movedFromAssetPaths);
            if (!changes.IsEmpty)
                AssetsChanged.Invoke(changes);
        }

        #endregion

    }

}
