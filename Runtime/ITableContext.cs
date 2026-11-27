using System;
using System.Collections.Generic;

using Object = UnityEngine.Object;

namespace SideXP.Database
{

    /// <summary>
    /// The assets of a table, given to the methods of the computed columns (see <see cref="ComputedColumnAttribute"/>) to compute
    /// values from the other assets of the table.
    /// </summary>
    /// <remarks>
    /// A context is created each time the values of the computed columns are computed again, and shared by all the computed columns
    /// and all the rows of the table for that computation. Don't keep it: its lists and its shared data are only valid during the
    /// computation.
    /// </remarks>
    public interface ITableContext
    {

        /// <summary>
        /// All the assets of the table (in its folder scope), in the order of the rows before any sort.
        /// </summary>
        IReadOnlyList<Object> AllAssets { get; }

        /// <summary>
        /// The assets of the table that pass the filters, in the order of the rows before any sort. A value based on these assets
        /// changes when the filters change.
        /// </summary>
        /// <remarks>
        /// The values are computed before the search and before the conditions on computed columns, which use them: these assets are
        /// the ones that pass the conditions on the other columns (a condition on a computed column counts as met). So typing a search
        /// doesn't change the values, and a filter on a computed column (such as a rank) compares the values computed for the assets
        /// filtered by the other conditions.
        /// </remarks>
        IReadOnlyList<Object> VisibleAssets { get; }

        /// <summary>
        /// Gets data shared by all the rows of the computation, built the first time it's asked for. Use it to build once what each row
        /// needs (totals, rankings), instead of building it for each row.
        /// </summary>
        /// <remarks>
        /// The data is identified by the build method: pass a static method (or a method group), not a lambda that captures variables,
        /// whose delegate would be a new one for each row. The data is built again on the next computation.
        /// </remarks>
        /// <typeparam name="T">The type of the shared data.</typeparam>
        /// <param name="build">The method that builds the data from this context, called once per computation.</param>
        /// <returns>Returns the shared data.</returns>
        /// <exception cref="ArgumentNullException">Thrown if the build method is null.</exception>
        T GetShared<T>(Func<ITableContext, T> build);

    }

}
