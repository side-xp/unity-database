using System;
using System.Diagnostics;

namespace SideXP.Database
{

    /// <summary>
    /// Displays the value returned by a static method as a read-only column of the tables of a type (see <see cref="TableAttribute"/>).
    /// </summary>
    /// <remarks>
    /// The method can be declared in any class: an editor class (for columns that are irrelevant in a build, or columns for types you
    /// can't modify), or a runtime class inside <c>#if UNITY_EDITOR</c>. It can be private. Its signature must be one of:
    /// <list type="bullet">
    /// <item><c>static TValue Method(TAsset asset)</c></item>
    /// <item><c>static TValue Method(TAsset asset, ITableContext context)</c></item>
    /// </list>
    /// <c>TAsset</c> must be <see cref="TargetType"/> or one of its base types, and <c>TValue</c> any type but <c>void</c>. The column
    /// is added to the table of <see cref="TargetType"/>, and to the tables of its derived types, after the columns of the fields. A
    /// method with another signature is ignored, with a warning.
    /// <para>
    /// The values are computed again when the data of the table changes (an edit, a reload) and when the filters change, never on
    /// each repaint, and only for the rows that pass the filters. The column can be sorted, filtered and searched like the others,
    /// but not edited. Use the <see cref="ITableContext"/> to compute values from the other assets of the table (totals, rankings),
    /// and <see cref="ITableContext.GetShared{T}(Func{ITableContext, T})"/> to build the data shared by all the rows once. If the
    /// method throws an exception, the rows of the column not computed yet get no value, and the exception is logged.
    /// </para>
    /// <para>
    /// The column is identified by its method (its declaring type and its name): changing <see cref="Name"/> keeps its settings in
    /// the views, renaming the method loses them. Overloads would share that identifier, so only the first one is a column. This
    /// attribute is stripped from player builds.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// public static class EquipmentColumns
    /// {
    ///     // The rank of the price of each equipment among the visible ones, the most expensive first
    ///     [ComputedColumn(typeof(EquipmentSO), "Price Rank", Width = 80)]
    ///     private static int GetPriceRank(EquipmentSO item, ITableContext context)
    ///     {
    ///         return context.GetShared(BuildPriceRanks).TryGetValue(item, out int rank) ? rank : 0;
    ///     }
    ///
    ///     private static Dictionary&lt;Object, int&gt; BuildPriceRanks(ITableContext context)
    ///     {
    ///         List&lt;EquipmentSO&gt; items = context.VisibleAssets
    ///             .OfType&lt;EquipmentSO&gt;()
    ///             .OrderByDescending(i =&gt; i.BasePrice)
    ///             .ToList();
    ///
    ///         Dictionary&lt;Object, int&gt; ranks = new Dictionary&lt;Object, int&gt;();
    ///         for (int i = 0; i &lt; items.Count; i++)
    ///             ranks[items[i]] = i + 1;
    ///         return ranks;
    ///     }
    /// }
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    [Conditional("UNITY_EDITOR")]
    public class ComputedColumnAttribute : Attribute
    {

        #region Lifecycle

        /// <summary>
        /// Displays the value returned by the marked method as a column of the tables of a type, named after the method.
        /// </summary>
        /// <param name="targetType"><inheritdoc cref="TargetType" path="/summary"/></param>
        public ComputedColumnAttribute(Type targetType)
        {
            TargetType = targetType;
        }

        /// <summary>
        /// Displays the value returned by the marked method as a column of the tables of a type, with a given name.
        /// </summary>
        /// <param name="targetType"><inheritdoc cref="TargetType" path="/summary"/></param>
        /// <param name="name"><inheritdoc cref="Name" path="/summary"/></param>
        public ComputedColumnAttribute(Type targetType, string name)
        {
            TargetType = targetType;
            Name = name;
        }

        #endregion


        #region Public API

        /// <summary>
        /// The type of the assets the column applies to: the type of a table, or a base type of tables. The column is added to the
        /// tables whose type is this type or derives from it. It can be an interface, to add the column to unrelated tables.
        /// </summary>
        public Type TargetType { get; private set; } = null;

        /// <summary>
        /// The column header text. If null or blank, the nicified method name is used (<c>GetPriceRank</c> becomes
        /// <i>Get Price Rank</i>).
        /// </summary>
        public string Name { get; set; } = null;

        /// <summary>
        /// The text shown when hovering the column header, or null if there is none.
        /// </summary>
        public string Tooltip { get; set; } = null;

        /// <summary>
        /// The default width of the column, in pixels. If 0 or negative, the column is flexible.
        /// </summary>
        public float Width { get; set; } = 0;

        #endregion

    }

}
