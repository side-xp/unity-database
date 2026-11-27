using System;
using System.Collections.Generic;

namespace SideXP.Database.EditorOnly
{

    /// <summary>
    /// A node of a filter tree: a group of nodes (see <see cref="FilterGroup"/>), or a condition on a column (see
    /// <see cref="FilterCondition"/>).
    /// </summary>
    /// <remarks>
    /// The tree is serialized with <see cref="UnityEngine.SerializeReferenceAttribute"/>, so the settings of a view can store any kind
    /// of node. It refers to the columns by their id, and is compiled against the columns of the table before it filters the rows, so
    /// a filter survives the changes of the columns: the nodes that don't apply anymore are ignored.
    /// </remarks>
    [Serializable]
    internal abstract class FilterNode
    {

        #region Fields

        /// <summary>
        /// The condition of a node whose values are not known yet (see <see cref="FilterColumn.IsDeferred"/>): it counts as met. Unlike
        /// a node that is ignored (null), it makes an "or" group keep all the rows, since the row may meet it once its values are
        /// known. Compare the compiled condition with it to know if a filter has nothing to check yet.
        /// </summary>
        public static readonly Predicate<TableRow> Deferred = row => true;

        #endregion


        #region Public API

        /// <summary>
        /// Compiles the node into the condition a row must meet to be visible.
        /// </summary>
        /// <param name="columns">The columns of the table that can be filtered, mapped by their id.</param>
        /// <returns>
        /// Returns the condition of the node, or null if the node doesn't apply to the table and must be ignored: a condition on a
        /// column that doesn't exist, or whose values are of a type the condition doesn't support, or a group without any condition
        /// that applies. Returns <see cref="Deferred"/> if the node can't be checked yet, since it depends on values that are not
        /// known yet.
        /// </returns>
        public abstract Predicate<TableRow> Compile(IReadOnlyDictionary<string, FilterColumn> columns);

        /// <summary>
        /// Gets a copy of this node, its child nodes included, so the copy can be edited without changing this node (for example in a
        /// duplicated view).
        /// </summary>
        /// <remarks>
        /// The default copy is a shallow copy of the fields: the nodes that hold lists or child nodes must override it. The assets the
        /// conditions refer to are shared, not copied.
        /// </remarks>
        /// <returns>Returns the copied node.</returns>
        public virtual FilterNode Clone()
        {
            return (FilterNode)MemberwiseClone();
        }

        #endregion

    }

}
