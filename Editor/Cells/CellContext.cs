using System;
using System.Reflection;

using Object = UnityEngine.Object;

namespace SideXP.Database.EditorOnly
{

    /// <summary>
    /// Informations about the cell displayed by a <see cref="CellHandler"/>.
    /// </summary>
    public readonly struct CellContext
    {

        #region Fields

        /// <summary>
        /// The asset of the cell's row.
        /// </summary>
        public readonly Object Asset;

        /// <summary>
        /// The field displayed in the cell's column, or null if the column doesn't display a field (for example a built-in column). Use
        /// it to read the attributes of the field.
        /// </summary>
        public readonly FieldInfo Field;

        /// <summary>
        /// The type of the values of the cell's column. The value of a cell can be of a derived type, or null.
        /// </summary>
        public readonly Type ValueType;

        #endregion


        #region Lifecycle

        /// <inheritdoc cref="CellContext"/>
        /// <param name="asset"><inheritdoc cref="Asset" path="/summary"/></param>
        /// <param name="field"><inheritdoc cref="Field" path="/summary"/></param>
        /// <param name="valueType"><inheritdoc cref="ValueType" path="/summary"/></param>
        public CellContext(Object asset, FieldInfo field, Type valueType)
        {
            Asset = asset;
            Field = field;
            ValueType = valueType;
        }

        #endregion

    }

}
