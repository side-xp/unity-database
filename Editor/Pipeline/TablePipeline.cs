using System;
using System.Collections.Generic;

using Object = UnityEngine.Object;

namespace SideXP.Database.EditorOnly
{

    /// <summary>
    /// Computes the rows of a table through separate stages: source, value cache, filter and sort.
    /// </summary>
    /// <remarks>
    /// Each stage only reruns when its input changes: invalidating the source reloads the assets and reads all the values again,
    /// invalidating a row reads its values again, and both rerun the filter and the sort. Changing the filter reruns the filter and the
    /// sort, changing the sort only reruns the sort. Invalidations are deferred: call <see cref="Update"/> to apply them.
    /// </remarks>
    internal class TablePipeline
    {

        #region Fields

        /// <summary>
        /// Provides the assets of the table.
        /// </summary>
        private IAssetSource _source = null;

        /// <summary>
        /// The accessors of the columns. The index of an accessor is the index of its column in the pipeline.
        /// </summary>
        private IReadOnlyList<IValueAccessor> _accessors = null;

        /// <summary>
        /// All the rows of the table, in the order of the source.
        /// </summary>
        private List<TableRow> _rows = new List<TableRow>();

        /// <summary>
        /// All the rows of the table, mapped by their asset.
        /// </summary>
        private Dictionary<Object, TableRow> _rowsByAsset = new Dictionary<Object, TableRow>();

        /// <summary>
        /// The rows that pass the filter, in the order of the sort.
        /// </summary>
        private List<TableRow> _visibleRows = new List<TableRow>();

        /// <summary>
        /// The condition a row must meet to be visible, or null to show all the rows.
        /// </summary>
        private Predicate<TableRow> _filter = null;

        /// <summary>
        /// The columns used to sort the rows, by priority: the rows are sorted by the first column, then the rows of equal values by
        /// the second column, and so on. Empty to keep the order of the source.
        /// </summary>
        private List<SortKey> _sortKeys = new List<SortKey>();

        /// <summary>
        /// The number of times the source has been loaded, so the users of the pipeline can tell when its rows have been replaced.
        /// </summary>
        private int _sourceVersion = 0;

        /// <summary>
        /// Checks if the rows match the assets of the source.
        /// </summary>
        private bool _isSourceValid = false;

        /// <summary>
        /// Checks if at least one row has outdated values.
        /// </summary>
        private bool _hasInvalidRows = false;

        /// <summary>
        /// Checks if the visible rows match the filter.
        /// </summary>
        private bool _isFilterValid = false;

        /// <summary>
        /// Checks if the visible rows match the sort.
        /// </summary>
        private bool _isSortValid = false;

        #endregion


        #region Lifecycle

        /// <inheritdoc cref="TablePipeline"/>
        /// <param name="source"><inheritdoc cref="_source" path="/summary"/></param>
        /// <param name="accessors"><inheritdoc cref="_accessors" path="/summary"/></param>
        public TablePipeline(IAssetSource source, IReadOnlyList<IValueAccessor> accessors)
        {
            _source = source;
            _accessors = accessors;
        }

        #endregion


        #region Public API

        /// <summary>
        /// Gets the number of columns of the pipeline.
        /// </summary>
        public int ColumnCount => _accessors.Count;

        /// <inheritdoc cref="_rows"/>
        public IReadOnlyList<TableRow> Rows => _rows;

        /// <inheritdoc cref="_visibleRows"/>
        public IReadOnlyList<TableRow> VisibleRows => _visibleRows;

        /// <inheritdoc cref="_sourceVersion"/>
        public int SourceVersion => _sourceVersion;

        /// <inheritdoc cref="_sortKeys"/>
        public IReadOnlyList<SortKey> SortKeys => _sortKeys;

        /// <summary>
        /// The condition a row must meet to be visible, or null to show all the rows. Setting it invalidates the filter.
        /// </summary>
        public Predicate<TableRow> Filter
        {
            get => _filter;
            set
            {
                _filter = value;
                InvalidateFilter();
            }
        }

        /// <summary>
        /// Sorts the rows by a single column. Rows with equal values keep the order of the source, in both directions.
        /// </summary>
        /// <param name="columnIndex">The index of the column used to sort the rows.</param>
        /// <param name="isDescending">If enabled, the rows are sorted in descending order.</param>
        /// <param name="comparison">Compares two values of the column.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if the column index is out of range.</exception>
        /// <exception cref="ArgumentNullException">Thrown if the comparison is null.</exception>
        public void SetSort(int columnIndex, bool isDescending, Comparison<object> comparison)
        {
            SetSort(new[] { new SortKey(columnIndex, isDescending, comparison) });
        }

        /// <summary>
        /// Sorts the rows by several columns, by priority: the rows are sorted by the first key, then the rows of equal values by the
        /// second key, and so on. Each key has its own direction. Rows equal for all the keys keep the order of the source.
        /// </summary>
        /// <param name="keys">The columns used to sort the rows, by priority. If empty, the rows are in the order of the source.</param>
        /// <exception cref="ArgumentNullException">Thrown if the list of keys, or the comparison of a key, is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if the column index of a key is out of range.</exception>
        /// <exception cref="ArgumentException">Thrown if several keys sort the same column.</exception>
        public void SetSort(IReadOnlyList<SortKey> keys)
        {
            if (keys == null)
                throw new ArgumentNullException(nameof(keys));

            HashSet<int> columnIndices = new HashSet<int>();
            foreach (SortKey key in keys)
            {
                if (key.ColumnIndex < 0 || key.ColumnIndex >= _accessors.Count)
                    throw new ArgumentOutOfRangeException(nameof(keys), $"The column index {key.ColumnIndex} is out of range.");

                if (key.Comparison == null)
                    throw new ArgumentNullException(nameof(keys), $"The key of the column {key.ColumnIndex} has no comparison.");

                if (!columnIndices.Add(key.ColumnIndex))
                    throw new ArgumentException($"The column {key.ColumnIndex} is sorted by several keys.", nameof(keys));
            }

            _sortKeys = new List<SortKey>(keys);
            _isSortValid = false;
        }

        /// <summary>
        /// Removes the sort, so the rows are in the order of the source.
        /// </summary>
        public void ClearSort()
        {
            _sortKeys.Clear();
            _isSortValid = false;
        }

        /// <summary>
        /// Marks the source as outdated, so the assets are loaded again on the next update.
        /// </summary>
        public void InvalidateSource()
        {
            _isSourceValid = false;
        }

        /// <summary>
        /// Marks the values of an asset's row as outdated, so they are read again on the next update.
        /// </summary>
        /// <param name="asset">The asset of which to invalidate the row.</param>
        /// <returns>Returns true if the asset has a row in the table.</returns>
        public bool InvalidateRow(Object asset)
        {
            if (asset == null || !_rowsByAsset.TryGetValue(asset, out TableRow row))
                return false;

            row.Invalidate();
            _hasInvalidRows = true;
            return true;
        }

        /// <summary>
        /// Marks the filter as outdated, so it reruns on the next update.
        /// </summary>
        public void InvalidateFilter()
        {
            _isFilterValid = false;
        }

        /// <summary>
        /// Gets the row of an asset.
        /// </summary>
        /// <param name="asset">The asset of which to get the row.</param>
        /// <param name="row">Outputs the found row.</param>
        /// <returns>Returns true if the asset has a row in the table.</returns>
        public bool TryGetRow(Object asset, out TableRow row)
        {
            if (asset != null)
                return _rowsByAsset.TryGetValue(asset, out row);

            row = null;
            return false;
        }

        /// <summary>
        /// Reruns the stages that are outdated, in order.
        /// </summary>
        /// <returns>Returns true if at least one stage has rerun, so the visible rows may have changed.</returns>
        public bool Update()
        {
            bool hasChanged = false;

            if (!_isSourceValid)
            {
                ReloadSource();
                hasChanged = true;
            }

            if (_hasInvalidRows)
            {
                ReadInvalidRows();
                hasChanged = true;
            }

            if (!_isFilterValid)
            {
                ApplyFilter();
                hasChanged = true;
            }

            if (!_isSortValid)
            {
                ApplySort();
                hasChanged = true;
            }

            return hasChanged;
        }

        #endregion


        #region Private API

        /// <summary>
        /// Loads the assets of the source, and updates the rows. The rows of the assets still in the table are kept, and all the rows
        /// are invalidated, since the assets may have changed.
        /// </summary>
        private void ReloadSource()
        {
            IReadOnlyList<Object> assets = _source.LoadAssets();
            Dictionary<Object, TableRow> previousRows = _rowsByAsset;

            _rows = new List<TableRow>(assets.Count);
            _rowsByAsset = new Dictionary<Object, TableRow>(assets.Count);
            foreach (Object asset in assets)
            {
                // Skip null and duplicate assets, which would break the mapping of the rows
                if (asset == null || _rowsByAsset.ContainsKey(asset))
                    continue;

                if (!previousRows.TryGetValue(asset, out TableRow row))
                    row = new TableRow(asset, _accessors.Count);

                row.SourceIndex = _rows.Count;
                row.Invalidate();
                _rows.Add(row);
                _rowsByAsset.Add(asset, row);
            }

            _sourceVersion++;
            _isSourceValid = true;
            _hasInvalidRows = true;
        }

        /// <summary>
        /// Reads the values of the rows that are outdated.
        /// </summary>
        private void ReadInvalidRows()
        {
            foreach (TableRow row in _rows)
            {
                if (!row.IsValid)
                    row.Read(_accessors);
            }

            _hasInvalidRows = false;
            _isFilterValid = false;
        }

        /// <summary>
        /// Selects the rows that pass the filter, in the order of the source.
        /// </summary>
        private void ApplyFilter()
        {
            _visibleRows.Clear();
            foreach (TableRow row in _rows)
            {
                if (_filter == null || _filter(row))
                    _visibleRows.Add(row);
            }

            _isFilterValid = true;
            _isSortValid = false;
        }

        /// <summary>
        /// Sorts the visible rows.
        /// </summary>
        /// <remarks>
        /// <see cref="List{T}.Sort(Comparison{T})"/> is not stable, so ties are broken by the position of the rows in the source. This
        /// makes the result independent of the current order of the visible rows.
        /// </remarks>
        private void ApplySort()
        {
            // Copied, so the comparison of the rows doesn't go through the list
            SortKey[] keys = _sortKeys.ToArray();
            _visibleRows.Sort((a, b) =>
            {
                foreach (SortKey key in keys)
                {
                    // The sign is used instead of the raw result, since negating int.MinValue overflows
                    int result = Math.Sign(key.Comparison(a.GetValue(key.ColumnIndex), b.GetValue(key.ColumnIndex)));
                    if (result != 0)
                        return key.IsDescending ? -result : result;
                }

                return a.SourceIndex.CompareTo(b.SourceIndex);
            });

            _isSortValid = true;
        }

        #endregion

    }

}
