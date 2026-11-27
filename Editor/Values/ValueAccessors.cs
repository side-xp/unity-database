using System;

namespace SideXP.Database.EditorOnly
{

    /// <summary>
    /// Chooses the accessor of each column.
    /// </summary>
    internal static class ValueAccessors
    {

        #region Public API

        /// <summary>
        /// Creates the accessor that reads the values of a column.
        /// </summary>
        /// <remarks>
        /// The accessor is chosen from the path of the value, and isn't configurable: reflection when every name of the path is a C#
        /// field serialized by Unity (<see cref="ColumnDefinition.Fields"/> is set), serialized properties otherwise. A computed column
        /// gets a <see cref="ComputedAccessor"/>, whose values are computed by the pipeline.
        /// </remarks>
        /// <param name="column">The column of which to create the accessor.</param>
        /// <returns>Returns the created accessor.</returns>
        /// <exception cref="NotSupportedException">Thrown if the column needs serialized properties, which come with the tables
        /// defined in the editor.</exception>
        public static IValueAccessor Create(ColumnDefinition column)
        {
            if (column.IsComputed)
                return new ComputedAccessor(column.Computed);

            if (column.Fields != null && column.Fields.Count > 0)
                return new ReflectionAccessor(column.Fields);

            throw new NotSupportedException($"The column \"{column.Id}\" can't be read with reflection, and the accessor for serialized " +
                "properties is not implemented yet.");
        }

        #endregion

    }

}
