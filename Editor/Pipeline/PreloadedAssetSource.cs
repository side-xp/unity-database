using System.Collections.Generic;

using Object = UnityEngine.Object;

namespace SideXP.Database.EditorOnly
{

    /// <summary>
    /// Provides assets that have already been loaded the first time, then the assets of another source.
    /// </summary>
    /// <remarks>
    /// Used when the assets must be known before the pipeline is created (for example to validate the columns against the first
    /// asset), so they are not loaded twice.
    /// </remarks>
    internal class PreloadedAssetSource : IAssetSource
    {

        #region Fields

        /// <summary>
        /// The assets returned by the first load, or null once they have been returned.
        /// </summary>
        private IReadOnlyList<Object> _preloadedAssets = null;

        /// <summary>
        /// The source of the next loads.
        /// </summary>
        private IAssetSource _source = null;

        #endregion


        #region Lifecycle

        /// <inheritdoc cref="PreloadedAssetSource"/>
        /// <param name="preloadedAssets"><inheritdoc cref="_preloadedAssets" path="/summary"/></param>
        /// <param name="source"><inheritdoc cref="_source" path="/summary"/></param>
        public PreloadedAssetSource(IReadOnlyList<Object> preloadedAssets, IAssetSource source)
        {
            _preloadedAssets = preloadedAssets;
            _source = source;
        }

        #endregion


        #region Public API

        /// <inheritdoc/>
        public IReadOnlyList<Object> LoadAssets()
        {
            if (_preloadedAssets == null)
                return _source.LoadAssets();

            IReadOnlyList<Object> assets = _preloadedAssets;
            _preloadedAssets = null;
            return assets;
        }

        #endregion

    }

}
