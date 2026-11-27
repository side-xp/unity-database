using System;
using System.Collections.Generic;

using Object = UnityEngine.Object;

namespace SideXP.Database.EditorOnly
{

    /// <summary>
    /// The context given to the methods of the computed columns, for a single computation of their values.
    /// </summary>
    internal class TableContext : ITableContext
    {

        #region Fields

        /// <inheritdoc cref="ITableContext.AllAssets"/>
        private List<Object> _allAssets = null;

        /// <inheritdoc cref="ITableContext.VisibleAssets"/>
        private List<Object> _visibleAssets = null;

        /// <summary>
        /// The data built by <see cref="GetShared{T}(Func{ITableContext, T})"/>, by build method. Delegates of the same method and
        /// target are equal, so a method group passed for each row finds the data built for the first row.
        /// </summary>
        private Dictionary<Delegate, object> _sharedData = new Dictionary<Delegate, object>();

        #endregion


        #region Lifecycle

        /// <inheritdoc cref="TableContext"/>
        /// <param name="allAssets"><inheritdoc cref="ITableContext.AllAssets" path="/summary"/></param>
        /// <param name="visibleAssets"><inheritdoc cref="ITableContext.VisibleAssets" path="/summary"/></param>
        public TableContext(List<Object> allAssets, List<Object> visibleAssets)
        {
            _allAssets = allAssets;
            _visibleAssets = visibleAssets;
        }

        #endregion


        #region Public API

        /// <inheritdoc/>
        public IReadOnlyList<Object> AllAssets => _allAssets;

        /// <inheritdoc/>
        public IReadOnlyList<Object> VisibleAssets => _visibleAssets;

        /// <inheritdoc/>
        public T GetShared<T>(Func<ITableContext, T> build)
        {
            if (build == null)
                throw new ArgumentNullException(nameof(build));

            if (_sharedData.TryGetValue(build, out object data))
                return (T)data;

            T builtData = build(this);
            _sharedData[build] = builtData;
            return builtData;
        }

        /// <summary>
        /// Ends the computation: clears the lists and the shared data, so a context kept by a method doesn't keep the assets alive,
        /// nor gives outdated data.
        /// </summary>
        public void End()
        {
            _allAssets.Clear();
            _visibleAssets.Clear();
            _sharedData.Clear();
        }

        #endregion

    }

}
