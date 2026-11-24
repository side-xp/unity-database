using System;

namespace SideXP.Database.EditorOnly
{

    /// <summary>
    /// Reads the values of a column from the assets of a table.
    /// </summary>
    /// <remarks>
    /// Reads must be fast enough to run on every row, since the value cache reads all the rows for sorting and search. Writes don't go
    /// through accessors: they always use serialized objects, which handle Undo, the dirty flag and validation.
    /// </remarks>
    internal interface IValueAccessor
    {

        /// <summary>
        /// The type of the values read by this accessor.
        /// </summary>
        Type ValueType { get; }

        /// <summary>
        /// Reads the value of the column for an asset.
        /// </summary>
        /// <param name="context">The asset to read, shared by the accessors of its row.</param>
        /// <returns>Returns the read value.</returns>
        object Read(ValueReadContext context);

    }

}
