using System;

using UnityEngine;

namespace SideXP.Database.EditorOnly
{

    /// <summary>
    /// Keeps the rows whose checkbox is checked, or the rows whose checkbox is unchecked.
    /// </summary>
    [Serializable]
    internal class BoolCondition : FilterCondition
    {

        #region Fields

        /// <summary>
        /// Checks if the condition keeps the checked rows. Otherwise, it keeps the unchecked rows.
        /// </summary>
        [SerializeField]
        private bool _isChecked = true;

        #endregion


        #region Lifecycle

        /// <inheritdoc cref="BoolCondition"/>
        public BoolCondition()
        {
        }

        /// <inheritdoc cref="BoolCondition"/>
        /// <param name="columnId"><inheritdoc cref="FilterCondition.ColumnId" path="/summary"/></param>
        /// <param name="isChecked"><inheritdoc cref="_isChecked" path="/summary"/></param>
        public BoolCondition(string columnId, bool isChecked)
            : base(columnId)
        {
            _isChecked = isChecked;
        }

        #endregion


        #region Public API

        /// <inheritdoc cref="_isChecked"/>
        public bool IsChecked
        {
            get => _isChecked;
            set => _isChecked = value;
        }

        /// <inheritdoc/>
        public override bool CanApplyTo(Type valueType)
        {
            return valueType == typeof(bool);
        }

        /// <inheritdoc/>
        public override Predicate<object> CreateMatcher(FilterColumn column)
        {
            bool isChecked = _isChecked;
            return value => value is bool isValueChecked && isValueChecked == isChecked;
        }

        #endregion

    }

}
