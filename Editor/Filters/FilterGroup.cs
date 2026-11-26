using System;
using System.Collections.Generic;

using UnityEngine;

namespace SideXP.Database.EditorOnly
{

    /// <summary>
    /// A group of filter nodes, combined with "and" or "or" (see <see cref="FilterOperator"/>). Groups can contain groups, to build
    /// filters like <c>Tier is Rare and (Price below 100 or Weight below 5)</c>.
    /// </summary>
    /// <remarks>
    /// The nodes that don't apply to the table are ignored (see <see cref="FilterNode.Compile"/>): they don't make an "or" group match
    /// every row, nor an "and" group match none. A group without any node that applies is ignored too, so an empty filter shows all
    /// the rows.
    /// </remarks>
    [Serializable]
    internal class FilterGroup : FilterNode
    {

        #region Fields

        /// <summary>
        /// How the nodes of the group are combined.
        /// </summary>
        [SerializeField]
        private FilterOperator _operator = FilterOperator.And;

        /// <summary>
        /// The nodes of the group, groups or conditions.
        /// </summary>
        [SerializeReference]
        private List<FilterNode> _nodes = new List<FilterNode>();

        #endregion


        #region Lifecycle

        /// <inheritdoc cref="FilterGroup"/>
        public FilterGroup()
        {
        }

        /// <inheritdoc cref="FilterGroup"/>
        /// <param name="filterOperator"><inheritdoc cref="_operator" path="/summary"/></param>
        /// <param name="nodes"><inheritdoc cref="_nodes" path="/summary"/></param>
        public FilterGroup(FilterOperator filterOperator, params FilterNode[] nodes)
        {
            _operator = filterOperator;
            _nodes.AddRange(nodes);
        }

        #endregion


        #region Public API

        /// <inheritdoc cref="_operator"/>
        public FilterOperator Operator
        {
            get => _operator;
            set => _operator = value;
        }

        /// <inheritdoc cref="_nodes"/>
        public List<FilterNode> Nodes => _nodes;

        /// <inheritdoc/>
        public override Predicate<TableRow> Compile(IReadOnlyDictionary<string, FilterColumn> columns)
        {
            List<Predicate<TableRow>> conditions = new List<Predicate<TableRow>>(_nodes.Count);
            foreach (FilterNode node in _nodes)
            {
                // A node of a hand-edited file, or of a type that doesn't exist anymore, can be null
                Predicate<TableRow> condition = node?.Compile(columns);
                if (condition != null)
                    conditions.Add(condition);
            }

            if (conditions.Count == 0)
                return null;

            if (conditions.Count == 1)
                return conditions[0];

            Predicate<TableRow>[] compiledConditions = conditions.ToArray();
            return _operator == FilterOperator.Or
                ? row => MatchesAny(row, compiledConditions)
                : row => MatchesAll(row, compiledConditions);
        }

        /// <inheritdoc/>
        public override FilterNode Clone()
        {
            FilterGroup copy = (FilterGroup)base.Clone();
            copy._nodes = _nodes.ConvertAll(node => node?.Clone());
            return copy;
        }

        #endregion


        #region Private API

        /// <summary>
        /// Checks if a row meets all the given conditions.
        /// </summary>
        /// <param name="row">The row to check.</param>
        /// <param name="conditions">The conditions the row must meet.</param>
        /// <returns>Returns true if the row meets all the conditions.</returns>
        private static bool MatchesAll(TableRow row, Predicate<TableRow>[] conditions)
        {
            foreach (Predicate<TableRow> condition in conditions)
            {
                if (!condition(row))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Checks if a row meets at least one of the given conditions.
        /// </summary>
        /// <param name="row">The row to check.</param>
        /// <param name="conditions">The conditions of which the row must meet one.</param>
        /// <returns>Returns true if the row meets at least one condition.</returns>
        private static bool MatchesAny(TableRow row, Predicate<TableRow>[] conditions)
        {
            foreach (Predicate<TableRow> condition in conditions)
            {
                if (condition(row))
                    return true;
            }

            return false;
        }

        #endregion

    }

}
