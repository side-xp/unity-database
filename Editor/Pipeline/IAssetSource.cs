using System.Collections.Generic;

using Object = UnityEngine.Object;

namespace SideXP.Database.EditorOnly
{

    /// <summary>
    /// Provides the assets of a table, for the source stage of the <see cref="TablePipeline"/>.
    /// </summary>
    internal interface IAssetSource
    {

        /// <summary>
        /// Loads the assets of the table.
        /// </summary>
        /// <returns>Returns the assets of the table, in their default order (the order of the rows when the table is not sorted).</returns>
        IReadOnlyList<Object> LoadAssets();

    }

}
