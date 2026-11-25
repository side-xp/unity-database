using System;
using System.Collections.Generic;

namespace SideXP.Database.EditorOnly
{

    /// <summary>
    /// A node of a filter tree: a group of nodes (see <see cref="FilterGroup"/>), or a condition on a column (see
    /// <see cref="FilterCondition"/>).
    /// </summary>
    /// <remarks>
    /// The tree is serialized with <see cref="UnityEngine.SerializeReferenceAttribute"/>, so the settings of a view can store any kind of node.
    /// It refers to the columns by their id, and is compiled against the columns of the table before it filters the rows, so a filter
    /// survives the changes of the columns: the nodes that don't apply anymore are ignored.
    /// </remarks>
    [Serializable]
    internal abstract class FilterNode
    {

        #region Public API

        /// <summary>
        /// Compiles the node into the condition a row must meet to be visible.
        /// </summary>
        /// <param name="columns">The columns of the table that can be filtered, mapped by their id.</param>
        /// <returns>
        /// Returns the condition of the node, or null if the node doesn't apply to the table and must be ignored: a condition on a
        /// column that doesn't exist, or whose values are of a type the condition doesn't support, or a group without any condition
        /// that applies.
        /// </returns>
        public abstract Predicate<TableRow> Compile(IReadOnlyDictionary<string, FilterColumn> columns);

        #endregion

    }

}
