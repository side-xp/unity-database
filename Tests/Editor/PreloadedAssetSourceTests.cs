using System.Collections.Generic;

using NUnit.Framework;

using UnityEngine;

using SideXP.Database.EditorOnly;

using Object = UnityEngine.Object;

namespace SideXP.Database.Tests
{

    /// <summary>
    /// Tests for the <see cref="PreloadedAssetSource"/>.
    /// </summary>
    public class PreloadedAssetSourceTests
    {

        #region Subclasses

        /// <summary>
        /// Provides the assets of a list, and counts how many times they are loaded.
        /// </summary>
        private class CountingAssetSource : IAssetSource
        {

            /// <summary>
            /// The assets to provide.
            /// </summary>
            public List<Object> Assets = new List<Object>();

            /// <summary>
            /// The number of times the assets have been loaded.
            /// </summary>
            public int LoadCount = 0;

            /// <inheritdoc/>
            public IReadOnlyList<Object> LoadAssets()
            {
                LoadCount++;
                return Assets;
            }

        }

        #endregion


        #region Fields

        /// <summary>
        /// The in-memory assets of the test, destroyed after it.
        /// </summary>
        private List<Object> _assets = null;

        #endregion


        #region Lifecycle

        /// <summary>
        /// Creates the in-memory assets of the test.
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            _assets = new List<Object>
            {
                ScriptableObject.CreateInstance<PipelineTestSO>(),
                ScriptableObject.CreateInstance<PipelineTestSO>(),
            };
        }

        /// <summary>
        /// Destroys the in-memory assets.
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            foreach (Object asset in _assets)
                Object.DestroyImmediate(asset);
        }

        #endregion


        #region Tests

        /// <summary>
        /// The first load returns the preloaded assets without loading the other source, and the next loads use the other source.
        /// </summary>
        [Test]
        public void LoadAssets_FirstThenNextLoads_UsesPreloadedAssetsOnce()
        {
            CountingAssetSource source = new CountingAssetSource();
            source.Assets.Add(_assets[1]);
            PreloadedAssetSource preloadedSource = new PreloadedAssetSource(new[] { _assets[0] }, source);

            CollectionAssert.AreEqual(new[] { _assets[0] }, preloadedSource.LoadAssets());
            Assert.AreEqual(0, source.LoadCount);

            CollectionAssert.AreEqual(new[] { _assets[1] }, preloadedSource.LoadAssets());
            Assert.AreEqual(1, source.LoadCount);
        }

        #endregion

    }

}
