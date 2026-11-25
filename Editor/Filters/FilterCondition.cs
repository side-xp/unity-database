using System;
using System.Collections.Generic;

using UnityEngine;

namespace SideXP.Database.EditorOnly
{

    /// <summary>
    /// A condition on the values of a column, identified by its id. Each kind of condition supports some types of values (see
    /// <see cref="CanApplyTo(Type)"/>).
    /// </summary>
    /// <remarks>
    /// A condition reads the cached values of the rows, never the assets. It's ignored when its column doesn't exist anymore, or when
    /// the type of its values changed to a type it doesn't support, for example after a field was changed in code.
    /// </remarks>
    [Serializable]
    internal abstract class FilterCondition : FilterNode
    {

        #region Fields

        /// <summary>
        /// The identifier of the filtered column in its table (see <see cref="TableViewColumn.Id"/>).
        /// </summary>
        [SerializeField]
        private string _columnId = null;

        #endregion


        #region Lifecycle

        /// <inheritdoc cref="FilterCondition"/>
        protected FilterCondition()
        {
        }

        /// <inheritdoc cref="FilterCondition"/>
        /// <param name="columnId"><inheritdoc cref="_columnId" path="/summary"/></param>
        protected FilterCondition(string columnId)
        {
            _columnId = columnId;
        }

        #endregion


        #region Public API

        /// <inheritdoc cref="_columnId"/>
        public string ColumnId
        {
            get => _columnId;
            set => _columnId = value;
        }

        /// <summary>
        /// Checks if the condition supports the values of a type.
        /// </summary>
        /// <param name="valueType">The type of the values of a column.</param>
        /// <returns>Returns true if the condition can filter a column whose values are of that type.</returns>
        public abstract bool CanApplyTo(Type valueType);

        /// <summary>
        /// Creates the check of the values of a column, once per compilation, so the condition can prepare what it compares the values
        /// with.
        /// </summary>
        /// <param name="column">The filtered column, whose values are of a type the condition supports.</param>
        /// <returns>Returns the check of a cached value of the column, true if the value meets the condition.</returns>
        public abstract Predicate<object> CreateMatcher(FilterColumn column);

        /// <inheritdoc/>
        public override Predicate<TableRow> Compile(IReadOnlyDictionary<string, FilterColumn> columns)
        {
            if (string.IsNullOrEmpty(_columnId) || !columns.TryGetValue(_columnId, out FilterColumn column))
                return null;

            if (column.ValueType == null || !CanApplyTo(column.ValueType))
                return null;

            int columnIndex = column.Index;
            Predicate<object> matcher = CreateMatcher(column);
            return row => matcher(row.GetValue(columnIndex));
        }

        #endregion

    }

}
