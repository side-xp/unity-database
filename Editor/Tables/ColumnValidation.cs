using System.Collections.Generic;

using UnityEditor;

using Object = UnityEngine.Object;

namespace SideXP.Database.EditorOnly
{

    /// <summary>
    /// Validates the columns of a table against one of its assets.
    /// </summary>
    /// <remarks>
    /// The columns declared in code are already checked when their definition is built, but only from the modifiers and attributes of
    /// their fields. This check needs an asset, and catches what the first check can't, such as fields of a type Unity can't serialize.
    /// </remarks>
    internal static class ColumnValidation
    {

        #region Public API

        /// <summary>
        /// Gets the columns that have a serialized property on a given asset.
        /// </summary>
        /// <param name="columns">The columns to validate.</param>
        /// <param name="asset">An asset of the table. Any asset works, since the columns are declared on the table type or its base
        /// types.</param>
        /// <param name="warnings">The list to which the issues found are added.</param>
        /// <returns>Returns the valid columns, in their original order.</returns>
        public static List<ColumnDefinition> GetValidColumns(
            IReadOnlyList<ColumnDefinition> columns,
            Object asset,
            ICollection<string> warnings)
        {
            List<ColumnDefinition> validColumns = new List<ColumnDefinition>(columns.Count);
            using (SerializedObject serializedObject = new SerializedObject(asset))
            {
                foreach (ColumnDefinition column in columns)
                {
                    if (serializedObject.FindProperty(column.PropertyPath) != null)
                    {
                        validColumns.Add(column);
                    }
                    else
                    {
                        warnings.Add($"The column \"{column.Name}\" of {asset.GetType()} is skipped: Unity doesn't serialize " +
                            $"\"{column.PropertyPath}\", maybe because Unity can't serialize its type.");
                    }
                }
            }

            return validColumns;
        }

        #endregion

    }

}
