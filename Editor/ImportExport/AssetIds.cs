using UnityEditor;

using Object = UnityEngine.Object;

namespace SideXP.Database.EditorOnly
{

    /// <summary>
    /// The text that identifies an asset in an exported file, written in its GUID column (see <see cref="TableExport.GuidColumnName"/>).
    /// </summary>
    /// <remarks>
    /// A main asset is identified by the GUID of its file (<c>4f2a...</c>). A sub-asset shares the GUID of its file, so its local file
    /// identifier follows after a <see cref="LocalIdSeparator"/> (<c>4f2a...:-6273913854398815</c>). Both stay the same when the asset is
    /// renamed or moved, and don't depend on the names of the sub-assets, which are not unique in their file.
    /// </remarks>
    internal static class AssetIds
    {

        #region Fields

        /// <summary>
        /// The character between the GUID of a file and the local file identifier of a sub-asset.
        /// </summary>
        /// <remarks>
        /// Not the <c>#</c> of the text of references (see <see cref="ObjectReferenceCellHandler"/>), which is followed by the name of
        /// the sub-asset, not by its identifier.
        /// </remarks>
        public const char LocalIdSeparator = ':';

        #endregion


        #region Public API

        /// <summary>
        /// Gets the text that identifies an asset: the GUID of its file, followed by its local file identifier if it's a sub-asset.
        /// </summary>
        /// <param name="asset">The asset to identify.</param>
        /// <returns>Returns the identifier of the asset, or an empty string if the object is null or is not saved in an asset file.
        /// </returns>
        public static string GetId(Object asset)
        {
            if (asset == null || !AssetDatabase.TryGetGUIDAndLocalFileIdentifier(asset, out string guid, out long localId))
                return string.Empty;

            if (string.IsNullOrEmpty(guid))
                return string.Empty;

            return AssetDatabase.IsMainAsset(asset)
                ? guid
                : $"{guid}{LocalIdSeparator}{localId}";
        }

        #endregion

    }

}
