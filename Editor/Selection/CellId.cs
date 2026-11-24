using System;

using Object = UnityEngine.Object;

namespace SideXP.Database.EditorOnly
{

    /// <summary>
    /// Identifies a cell of a table by its asset and its column, instead of its position, so it survives sorting, filtering and
    /// reloading.
    /// </summary>
    internal readonly struct CellId : IEquatable<CellId>
    {

        #region Fields

        /// <summary>
        /// The asset of the row of the cell, or null for an empty identifier.
        /// </summary>
        private readonly Object _asset;

        /// <summary>
        /// The identifier of the column of the cell in its table.
        /// </summary>
        private readonly string _columnId;

        #endregion


        #region Lifecycle

        /// <inheritdoc cref="CellId"/>
        /// <param name="asset"><inheritdoc cref="_asset" path="/summary"/></param>
        /// <param name="columnId"><inheritdoc cref="_columnId" path="/summary"/></param>
        public CellId(Object asset, string columnId)
        {
            _asset = asset;
            _columnId = columnId;
        }

        #endregion


        #region Public API

        /// <summary>
        /// An identifier that designates no cell.
        /// </summary>
        public static CellId None => default;

        /// <inheritdoc cref="_asset"/>
        public Object Asset => _asset;

        /// <inheritdoc cref="_columnId"/>
        public string ColumnId => _columnId;

        /// <summary>
        /// Checks if this identifier designates no cell.
        /// </summary>
        /// <remarks>
        /// The asset is compared by reference: a destroyed asset still designates its former cell, so the table can tell that the cell
        /// is gone.
        /// </remarks>
        public bool IsNone => ReferenceEquals(_asset, null);

        /// <summary>
        /// Checks if this identifier designates a given cell.
        /// </summary>
        /// <param name="asset">The asset of the row of the cell.</param>
        /// <param name="columnId">The identifier of the column of the cell.</param>
        /// <returns>Returns true if the asset and the column match.</returns>
        public bool Is(Object asset, string columnId)
        {
            return ReferenceEquals(_asset, asset) && _columnId == columnId;
        }

        /// <inheritdoc/>
        public bool Equals(CellId other)
        {
            return Is(other._asset, other._columnId);
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return obj is CellId other && Equals(other);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            // The hash of a Unity object doesn't change when it's destroyed
            return HashCode.Combine(ReferenceEquals(_asset, null) ? 0 : _asset.GetHashCode(), _columnId);
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            if (IsNone)
                return "None";

            // The name of a destroyed asset can't be read
            string assetName = _asset != null ? _asset.name : "Missing";
            return $"{assetName} ({_columnId})";
        }

        #endregion

    }

}
