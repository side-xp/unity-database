using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

using Object = UnityEngine.Object;

namespace SideXP.Database.Tests
{

    /// <summary>
    /// Valid computed columns of <see cref="PipelineTestSO"/>, which is not a table, so they never appear in the window. Counts the
    /// computations, so the tests can check when the values are computed again.
    /// </summary>
    public static class ComputedColumnTestMethods
    {

        #region Fields

        /// <summary>
        /// The number of values computed by <see cref="GetDouble"/>.
        /// </summary>
        public static int DoubleCount = 0;

        /// <summary>
        /// The number of times the ranks of <see cref="GetRank"/> have been built.
        /// </summary>
        public static int RanksBuildCount = 0;

        /// <summary>
        /// The number of the assets for which <see cref="Throw"/> throws an exception.
        /// </summary>
        public const int ThrowingNumber = 1;

        #endregion


        #region Computed columns

        /// <summary>
        /// Computes twice the number of an asset.
        /// </summary>
        /// <param name="asset">The asset of the row.</param>
        /// <returns>Returns twice the number of the asset.</returns>
        [ComputedColumn(typeof(PipelineTestSO), "Double")]
        private static int GetDouble(PipelineTestSO asset)
        {
            DoubleCount++;
            return asset.number * 2;
        }

        /// <summary>
        /// Computes the rank of the number of an asset among the visible assets, the greatest first.
        /// </summary>
        /// <param name="asset">The asset of the row.</param>
        /// <param name="context">The context of the table.</param>
        /// <returns>Returns the rank, from 1.</returns>
        [ComputedColumn(typeof(PipelineTestSO), Tooltip = "The rank of the number.", Width = 50)]
        private static int GetRank(PipelineTestSO asset, ITableContext context)
        {
            return context.GetShared(BuildRanks)[asset];
        }

        /// <summary>
        /// Computes a value from the number of assets of the context, and takes a base type of the target type.
        /// </summary>
        /// <param name="asset">The asset of the row.</param>
        /// <param name="context">The context of the table.</param>
        /// <returns>Returns the number of visible assets times 100, plus the number of assets.</returns>
        [ComputedColumn(typeof(PipelineTestSO))]
        private static int GetTotal(ScriptableObject asset, ITableContext context)
        {
            return context.VisibleAssets.Count * 100 + context.AllAssets.Count;
        }

        /// <summary>
        /// Throws an exception for the assets whose number is <see cref="ThrowingNumber"/>.
        /// </summary>
        /// <param name="asset">The asset of the row.</param>
        /// <returns>Returns the number of the asset.</returns>
        /// <exception cref="InvalidOperationException">Thrown for the assets whose number is <see cref="ThrowingNumber"/>.</exception>
        [ComputedColumn(typeof(PipelineTestSO), "Throws")]
        private static int Throw(PipelineTestSO asset)
        {
            if (asset.number == ThrowingNumber)
                throw new InvalidOperationException("Test exception of a computed column.");

            return asset.number;
        }

        #endregion


        #region Private API

        /// <summary>
        /// Builds the ranks of the visible assets, by number, the greatest first, in the order of the source for equal numbers.
        /// </summary>
        /// <param name="context">The context of the table.</param>
        /// <returns>Returns the rank of each visible asset, from 1.</returns>
        private static Dictionary<Object, int> BuildRanks(ITableContext context)
        {
            RanksBuildCount++;
            List<PipelineTestSO> assets = context.VisibleAssets.Cast<PipelineTestSO>().OrderByDescending(i => i.number).ToList();

            Dictionary<Object, int> ranks = new Dictionary<Object, int>();
            for (int i = 0; i < assets.Count; i++)
                ranks[assets[i]] = i + 1;

            return ranks;
        }

        #endregion

    }

    /// <summary>
    /// Invalid computed columns of <see cref="PipelineTestSO"/>, which is not a table, so they are never reported in the console. Only
    /// the first <see cref="Overload(PipelineTestSO)"/> is valid.
    /// </summary>
    public class InvalidComputedColumnTestMethods
    {

        /// <summary>
        /// Invalid: not static.
        /// </summary>
        /// <param name="asset">The asset of the row.</param>
        /// <returns>Returns 0.</returns>
        [ComputedColumn(typeof(PipelineTestSO))]
        private int NotStatic(PipelineTestSO asset) => 0;

        /// <summary>
        /// Invalid: returns no value.
        /// </summary>
        /// <param name="asset">The asset of the row.</param>
        [ComputedColumn(typeof(PipelineTestSO))]
        private static void ReturnsVoid(PipelineTestSO asset) { }

        /// <summary>
        /// Invalid: takes no asset.
        /// </summary>
        /// <returns>Returns 0.</returns>
        [ComputedColumn(typeof(PipelineTestSO))]
        private static int NoParameter() => 0;

        /// <summary>
        /// Invalid: takes an asset of a type that isn't a base type of the target type.
        /// </summary>
        /// <param name="asset">The asset of the row.</param>
        /// <returns>Returns 0.</returns>
        [ComputedColumn(typeof(PipelineTestSO))]
        private static int WrongAssetType(ColumnsSO asset) => 0;

        /// <summary>
        /// Invalid: the second parameter is not the context.
        /// </summary>
        /// <param name="asset">The asset of the row.</param>
        /// <param name="text">Not a context.</param>
        /// <returns>Returns 0.</returns>
        [ComputedColumn(typeof(PipelineTestSO))]
        private static int WrongContextType(PipelineTestSO asset, string text) => 0;

        /// <summary>
        /// Invalid: too many parameters.
        /// </summary>
        /// <param name="asset">The asset of the row.</param>
        /// <param name="context">The context of the table.</param>
        /// <param name="extra">An extra parameter.</param>
        /// <returns>Returns 0.</returns>
        [ComputedColumn(typeof(PipelineTestSO))]
        private static int TooManyParameters(PipelineTestSO asset, ITableContext context, int extra) => 0;

        /// <summary>
        /// Invalid: generic.
        /// </summary>
        /// <typeparam name="T">Any type.</typeparam>
        /// <param name="asset">The asset of the row.</param>
        /// <returns>Returns 0.</returns>
        [ComputedColumn(typeof(PipelineTestSO))]
        private static int Generic<T>(PipelineTestSO asset) => 0;

        /// <summary>
        /// Invalid: takes the asset by reference.
        /// </summary>
        /// <param name="asset">The asset of the row.</param>
        /// <returns>Returns 0.</returns>
        [ComputedColumn(typeof(PipelineTestSO))]
        private static int ByReference(ref PipelineTestSO asset) => 0;

        /// <summary>
        /// Valid: the first overload of the name.
        /// </summary>
        /// <param name="asset">The asset of the row.</param>
        /// <returns>Returns 0.</returns>
        [ComputedColumn(typeof(PipelineTestSO))]
        private static int Overload(PipelineTestSO asset) => 0;

        /// <summary>
        /// Invalid: an overload of a computed column, which would have the same identifier.
        /// </summary>
        /// <param name="asset">The asset of the row.</param>
        /// <param name="context">The context of the table.</param>
        /// <returns>Returns 0.</returns>
        [ComputedColumn(typeof(PipelineTestSO))]
        private static int Overload(PipelineTestSO asset, ITableContext context) => 0;

    }

}
