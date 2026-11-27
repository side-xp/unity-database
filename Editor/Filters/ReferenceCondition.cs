using System;

using UnityEngine;

using Object = UnityEngine.Object;

namespace SideXP.Database.EditorOnly
{

    /// <summary>
    /// Keeps the rows whose reference is a chosen asset (or <i>None</i>), or the rows whose reference is missing.
    /// </summary>
    /// <remarks>
    /// A missing reference points to an asset that has been deleted: it's not null for C#, but is null for Unity, like the
    /// <i>Missing</i> cells of <see cref="ObjectReferenceCellHandler"/>. A condition whose chosen asset has been deleted keeps no row,
    /// since the references to that asset are missing too, and not distinguishable from the other missing references.
    /// </remarks>
    [Serializable]
    internal class ReferenceCondition : FilterCondition
    {

        #region Subclasses

        /// <summary>
        /// What a <see cref="ReferenceCondition"/> checks.
        /// </summary>
        public enum ReferenceMatch
        {

            /// <summary>
            /// The reference is the chosen asset, or <i>None</i> if no asset is chosen.
            /// </summary>
            IsAsset,

            /// <summary>
            /// The reference is missing (it points to a deleted asset).
            /// </summary>
            IsMissing,

        }

        #endregion


        #region Fields

        /// <summary>
        /// What the condition checks.
        /// </summary>
        [SerializeField]
        private ReferenceMatch _match = ReferenceMatch.IsAsset;

        /// <summary>
        /// The chosen asset, or null to keep the empty references (<i>None</i>). Only used with <see cref="ReferenceMatch.IsAsset"/>.
        /// </summary>
        [SerializeField]
        private Object _asset = null;

        #endregion


        #region Lifecycle

        /// <inheritdoc cref="ReferenceCondition"/>
        public ReferenceCondition()
        {
        }

        /// <inheritdoc cref="ReferenceCondition"/>
        /// <param name="columnId"><inheritdoc cref="FilterCondition.ColumnId" path="/summary"/></param>
        /// <param name="match"><inheritdoc cref="_match" path="/summary"/></param>
        /// <param name="asset"><inheritdoc cref="_asset" path="/summary"/></param>
        public ReferenceCondition(string columnId, ReferenceMatch match, Object asset = null)
            : base(columnId)
        {
            _match = match;
            _asset = asset;
        }

        #endregion


        #region Public API

        /// <inheritdoc cref="_match"/>
        public ReferenceMatch Match
        {
            get => _match;
            set => _match = value;
        }

        /// <inheritdoc cref="_asset"/>
        public Object Asset
        {
            get => _asset;
            set => _asset = value;
        }

        /// <inheritdoc/>
        public override bool CanApplyTo(Type valueType)
        {
            return valueType != null && typeof(Object).IsAssignableFrom(valueType);
        }

        /// <inheritdoc/>
        public override Predicate<object> CreateMatcher(FilterColumn column)
        {
            if (_match == ReferenceMatch.IsMissing)
                return IsMissing;

            // Compared by reference: a reference not null for C# but null for Unity is missing
            if (ReferenceEquals(_asset, null))
                return value => value is not Object;

            if (_asset == null)
                return value => false;

            Object asset = _asset;
            return value => value is Object reference && reference == asset;
        }

        #endregion


        #region Private API

        /// <summary>
        /// Checks if a reference is missing: not null for C#, but null for Unity.
        /// </summary>
        /// <param name="value">The cached value of a cell.</param>
        /// <returns>Returns true if the reference is missing.</returns>
        private static bool IsMissing(object value)
        {
            return value is Object reference && reference == null;
        }

        #endregion

    }

}
