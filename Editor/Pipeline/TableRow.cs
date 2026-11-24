using System.Collections.Generic;

using Object = UnityEngine.Object;

namespace SideXP.Database.EditorOnly
{

    /// <summary>
    /// A row of a table: an asset, and the cached values of its columns.
    /// </summary>
    /// <remarks>
    /// The display, the filter and the sort only use the cached values, and never read the asset themselves. A row keeps the same
    /// instance across reloads of the source, as long as its asset is still in the table.
    /// </remarks>
    internal class TableRow
    {

        #region Fields

        /// <summary>
        /// The asset displayed in this row.
        /// </summary>
        private Object _asset = null;

        /// <summary>
        /// The cached values of the columns, in the order of the accessors of the pipeline.
        /// </summary>
        private object[] _values = null;

        /// <summary>
        /// Checks if the cached values are up to date.
        /// </summary>
        private bool _isValid = false;

        /// <summary>
        /// The position of the row in the source, used as the default order and to break ties when sorting.
        /// </summary>
        private int _sourceIndex = 0;

        /// <summary>
        /// The text of the row compared with the search, built from the cached values by the owner of the pipeline, or null if it must
        /// be built again. Cleared each time the values are read.
        /// </summary>
        private string _searchText = null;

        #endregion


        #region Lifecycle

        /// <inheritdoc cref="TableRow"/>
        /// <param name="asset"><inheritdoc cref="_asset" path="/summary"/></param>
        /// <param name="columnCount">The number of columns of the pipeline.</param>
        internal TableRow(Object asset, int columnCount)
        {
            _asset = asset;
            _values = new object[columnCount];
        }

        #endregion


        #region Public API

        /// <inheritdoc cref="_asset"/>
        public Object Asset => _asset;

        /// <inheritdoc cref="_isValid"/>
        public bool IsValid => _isValid;

        /// <inheritdoc cref="_sourceIndex"/>
        public int SourceIndex
        {
            get => _sourceIndex;
            internal set => _sourceIndex = value;
        }

        /// <inheritdoc cref="_searchText"/>
        public string SearchText
        {
            get => _searchText;
            set => _searchText = value;
        }

        /// <summary>
        /// Gets the cached value of a column.
        /// </summary>
        /// <param name="columnIndex">The index of the column, in the order of the accessors of the pipeline.</param>
        /// <returns>Returns the cached value.</returns>
        public object GetValue(int columnIndex)
        {
            return _values[columnIndex];
        }

        #endregion


        #region Internal API

        /// <summary>
        /// Marks the cached values as outdated, so they are read again on the next update of the pipeline.
        /// </summary>
        internal void Invalidate()
        {
            _isValid = false;
        }

        /// <summary>
        /// Reads the values of all the columns from the asset.
        /// </summary>
        /// <param name="accessors">The accessors of the columns.</param>
        internal void Read(IReadOnlyList<IValueAccessor> accessors)
        {
            using (ValueReadContext context = new ValueReadContext(_asset))
            {
                for (int i = 0; i < accessors.Count; i++)
                    _values[i] = accessors[i].Read(context);
            }

            // Built from the previous values
            _searchText = null;
            _isValid = true;
        }

        #endregion

    }

}
