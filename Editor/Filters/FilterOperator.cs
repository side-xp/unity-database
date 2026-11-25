namespace SideXP.Database.EditorOnly
{

    /// <summary>
    /// How a <see cref="FilterGroup"/> combines its nodes.
    /// </summary>
    internal enum FilterOperator
    {

        /// <summary>
        /// A row must meet all the nodes of the group.
        /// </summary>
        And,

        /// <summary>
        /// A row must meet at least one node of the group.
        /// </summary>
        Or,

    }

}
