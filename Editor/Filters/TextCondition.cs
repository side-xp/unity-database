using System;

using UnityEngine;

namespace SideXP.Database.EditorOnly
{

    /// <summary>
    /// Keeps the rows whose cell contains a text, ignoring case.
    /// </summary>
    /// <remarks>
    /// Applies to the columns of any type: the cells are converted to text like the search does (names for enums, paths for
    /// references, invariant numbers), so this is also the condition of the types that have no condition of their own. Unlike the
    /// search, the text is not split into words. An empty text keeps all the rows.
    /// </remarks>
    [Serializable]
    internal class TextCondition : FilterCondition
    {

        #region Fields

        /// <summary>
        /// The text the cells must contain.
        /// </summary>
        [SerializeField]
        private string _text = string.Empty;

        #endregion


        #region Lifecycle

        /// <inheritdoc cref="TextCondition"/>
        public TextCondition()
        {
        }

        /// <inheritdoc cref="TextCondition"/>
        /// <param name="columnId"><inheritdoc cref="FilterCondition.ColumnId" path="/summary"/></param>
        /// <param name="text"><inheritdoc cref="_text" path="/summary"/></param>
        public TextCondition(string columnId, string text)
            : base(columnId)
        {
            _text = text ?? string.Empty;
        }

        #endregion


        #region Public API

        /// <inheritdoc cref="_text"/>
        public string Text
        {
            get => _text;
            set => _text = value ?? string.Empty;
        }

        /// <inheritdoc/>
        public override bool CanApplyTo(Type valueType)
        {
            return valueType != null;
        }

        /// <inheritdoc/>
        public override Predicate<object> CreateMatcher(FilterColumn column)
        {
            string text = _text ?? string.Empty;
            if (text.Length == 0)
                return value => true;

            Func<object, string> toText = column.ToText;
            return value =>
            {
                // A failed conversion is an empty text, like in the search
                string cellText = toText != null ? toText(value) : null;
                return cellText != null && cellText.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0;
            };
        }

        #endregion

    }

}
