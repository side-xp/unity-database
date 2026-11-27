using System;

using UnityEngine;

namespace SideXP.Database.EditorOnly
{

    /// <summary>
    /// A column the rows of a view are sorted by, identified by its id, with its direction (see <see cref="ViewSettings"/>).
    /// </summary>
    [Serializable]
    internal struct ColumnSort : IEquatable<ColumnSort>
    {

        #region Fields

        /// <summary>
        /// The identifier of the sorted column in its table (see <see cref="TableViewColumn.Id"/>).
        /// </summary>
        [SerializeField]
        private string _columnId;

        /// <summary>
        /// Checks if the rows are sorted in descending order of this column.
        /// </summary>
        [SerializeField]
        private bool _isDescending;

        #endregion


        #region Lifecycle

        /// <inheritdoc cref="ColumnSort"/>
        /// <param name="columnId"><inheritdoc cref="_columnId" path="/summary"/></param>
        /// <param name="isDescending"><inheritdoc cref="_isDescending" path="/summary"/></param>
        public ColumnSort(string columnId, bool isDescending)
        {
            _columnId = columnId;
            _isDescending = isDescending;
        }

        #endregion


        #region Public API

        /// <inheritdoc cref="_columnId"/>
        public string ColumnId => _columnId;

        /// <inheritdoc cref="_isDescending"/>
        public bool IsDescending => _isDescending;

        /// <summary>
        /// Gets the sort of the same column in the other direction.
        /// </summary>
        /// <returns>Returns the reversed sort.</returns>
        public ColumnSort Reversed()
        {
            return new ColumnSort(_columnId, !_isDescending);
        }

        /// <inheritdoc/>
        public bool Equals(ColumnSort other)
        {
            return _columnId == other._columnId && _isDescending == other._isDescending;
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return obj is ColumnSort other && Equals(other);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return HashCode.Combine(_columnId, _isDescending);
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"{_columnId} ({(_isDescending ? "descending" : "ascending")})";
        }

        #endregion

    }

}
