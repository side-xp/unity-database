using System;
using System.Collections.Generic;

using UnityEngine;

namespace SideXP.Database.EditorOnly
{

    /// <summary>
    /// Holds the definitions of all the tables of the project.
    /// </summary>
    /// <remarks>
    /// The definitions are built the first time they are needed, and stay valid until the next domain reload, since tables declared in
    /// code only change when scripts are compiled.
    /// </remarks>
    internal static class TableRegistry
    {

        #region Fields

        /// <summary>
        /// The definitions of all the tables, sorted by name. Null until they are built.
        /// </summary>
        private static List<TableDefinition> s_tables = null;

        /// <summary>
        /// The definitions of all the tables, mapped by their identifier.
        /// </summary>
        private static Dictionary<string, TableDefinition> s_tablesById = null;

        /// <summary>
        /// The definitions of all the tables, mapped by their type.
        /// </summary>
        private static Dictionary<Type, TableDefinition> s_tablesByType = null;

        #endregion


        #region Public API

        /// <summary>
        /// Gets the definitions of all the tables, sorted by name.
        /// </summary>
        public static IReadOnlyList<TableDefinition> Tables
        {
            get
            {
                EnsureLoaded();
                return s_tables;
            }
        }

        /// <summary>
        /// Gets the table that has a given identifier.
        /// </summary>
        /// <param name="id">The identifier of the table (see <see cref="TableDefinition.Id"/>).</param>
        /// <param name="table">Outputs the found table.</param>
        /// <returns>Returns true if a table has been found.</returns>
        public static bool TryGet(string id, out TableDefinition table)
        {
            EnsureLoaded();
            if (id != null)
                return s_tablesById.TryGetValue(id, out table);

            table = null;
            return false;
        }

        /// <summary>
        /// Gets the table of a given type.
        /// </summary>
        /// <param name="type">The type of the table. The table of a base type doesn't count.</param>
        /// <param name="table">Outputs the found table.</param>
        /// <returns>Returns true if a table has been found.</returns>
        public static bool TryGet(Type type, out TableDefinition table)
        {
            EnsureLoaded();
            if (type != null)
                return s_tablesByType.TryGetValue(type, out table);

            table = null;
            return false;
        }

        #endregion


        #region Private API

        /// <summary>
        /// Builds the definitions of all the tables if they haven't been built yet, and logs the issues found in the declarations.
        /// </summary>
        private static void EnsureLoaded()
        {
            if (s_tables != null)
                return;

            List<string> warnings = new List<string>();
            s_tables = new List<TableDefinition>();
            foreach (Type type in CodeTables.GetCandidateTypes())
            {
                TableDefinition table = CodeTables.Build(type, warnings);
                if (table != null)
                    s_tables.Add(table);
            }

            s_tables.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));

            s_tablesById = new Dictionary<string, TableDefinition>(s_tables.Count);
            s_tablesByType = new Dictionary<Type, TableDefinition>(s_tables.Count);
            foreach (TableDefinition table in s_tables)
            {
                s_tablesById[table.Id] = table;
                s_tablesByType[table.Type] = table;
            }

            // Log a single message, so the issues don't flood the console on every domain reload
            if (warnings.Count > 0)
            {
                Debug.LogWarning($"{DatabaseConstants.LogPrefix}{warnings.Count} issue(s) found in the declarations of the tables:\n- " +
                    string.Join("\n- ", warnings));
            }
        }

        #endregion

    }

}
