using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using UnityEditor;

namespace SideXP.Database.EditorOnly
{

    /// <summary>
    /// Finds the computed columns declared with <see cref="ComputedColumnAttribute"/>, and gives the ones that apply to each table.
    /// </summary>
    /// <remarks>
    /// The signature of a method is checked the first time a table it applies to is built, so a method is reported once, and the
    /// methods that apply to no table are never reported.
    /// </remarks>
    internal class ComputedColumns
    {

        #region Subclasses

        /// <summary>
        /// A method marked with <see cref="ComputedColumnAttribute"/>, and its column once its signature is checked.
        /// </summary>
        private class Declaration
        {

            /// <summary>
            /// The marked method.
            /// </summary>
            public MethodInfo Method;

            /// <summary>
            /// The attribute of the method.
            /// </summary>
            public ComputedColumnAttribute Attribute;

            /// <summary>
            /// The identifier of the column (see <see cref="GetId"/>).
            /// </summary>
            public string Id;

            /// <summary>
            /// The reason why the column is skipped whatever its signature, or null if there is none.
            /// </summary>
            public string SkipReason;

            /// <summary>
            /// Checks if the signature of the method has been checked.
            /// </summary>
            public bool IsChecked;

            /// <summary>
            /// The definition of the column, or null if the method is not checked yet, or invalid.
            /// </summary>
            public ColumnDefinition Column;

        }

        #endregion


        #region Fields

        /// <summary>
        /// The prefix of the identifiers of the computed columns. A field name can't start with it, so the identifier of a computed
        /// column never matches the identifier of a field column.
        /// </summary>
        public const string IdPrefix = "#";

        /// <summary>
        /// The methods marked with <see cref="ComputedColumnAttribute"/>, ordered by the full name of their declaring type, then in
        /// declaration order.
        /// </summary>
        private List<Declaration> _declarations = new List<Declaration>();

        #endregion


        #region Lifecycle

        /// <inheritdoc cref="ComputedColumns"/>
        /// <param name="methods">The methods marked with <see cref="ComputedColumnAttribute"/>. Methods without the attribute are
        /// ignored.</param>
        public ComputedColumns(IEnumerable<MethodInfo> methods)
        {
            // The order of the methods found by the TypeCache isn't guaranteed: the metadata tokens follow the declaration order
            IEnumerable<MethodInfo> sortedMethods = methods
                .Where(i => i != null && i.DeclaringType != null)
                .OrderBy(i => i.DeclaringType.FullName, StringComparer.Ordinal)
                .ThenBy(i => i.MetadataToken);

            Dictionary<string, MethodInfo> methodsById = new Dictionary<string, MethodInfo>();
            foreach (MethodInfo method in sortedMethods)
            {
                ComputedColumnAttribute attribute = method.GetCustomAttribute<ComputedColumnAttribute>(false);
                if (attribute == null)
                    continue;

                Declaration declaration = new Declaration { Method = method, Attribute = attribute, Id = GetId(method) };

                // Overloads have the same identifier: only the first one is a column
                if (methodsById.ContainsKey(declaration.Id))
                    declaration.SkipReason = "an overload of the method is already a computed column";
                else
                    methodsById.Add(declaration.Id, method);

                _declarations.Add(declaration);
            }
        }

        #endregion


        #region Public API

        /// <summary>
        /// Finds the computed columns of the project, with the <see cref="TypeCache"/>.
        /// </summary>
        /// <returns>Returns the found computed columns.</returns>
        public static ComputedColumns FromProject()
        {
            return new ComputedColumns(TypeCache.GetMethodsWithAttribute<ComputedColumnAttribute>());
        }

        /// <summary>
        /// Gets the identifier of a computed column.
        /// </summary>
        /// <remarks>
        /// The identifier comes from the method, not from the name of the column, so renaming the column keeps its settings in the
        /// views, but renaming the method or its declaring type loses them, like renaming a field.
        /// </remarks>
        /// <param name="method">The method of the computed column.</param>
        /// <returns>Returns <see cref="IdPrefix"/>, the full name of the declaring type and the name of the method.</returns>
        public static string GetId(MethodInfo method)
        {
            return $"{IdPrefix}{method.DeclaringType.FullName}.{method.Name}";
        }

        /// <summary>
        /// Gets the computed columns that apply to the table of a type: the columns whose target type is the type or one of its base
        /// types (or interfaces).
        /// </summary>
        /// <param name="tableType">The type of the table.</param>
        /// <param name="warnings">The list to which the issues found in the declarations are added, the first time they are
        /// checked.</param>
        /// <returns>Returns the valid columns that apply to the table, ordered by the full name of the type that declares their method,
        /// then in declaration order.</returns>
        public List<ColumnDefinition> GetColumns(Type tableType, ICollection<string> warnings)
        {
            List<ColumnDefinition> columns = new List<ColumnDefinition>();
            foreach (Declaration declaration in _declarations)
            {
                Type targetType = declaration.Attribute.TargetType;
                if (targetType == null || !targetType.IsAssignableFrom(tableType))
                    continue;

                if (!declaration.IsChecked)
                    Check(declaration, warnings);

                if (declaration.Column != null)
                    columns.Add(declaration.Column);
            }
            return columns;
        }

        #endregion


        #region Private API

        /// <summary>
        /// Checks the signature of a method, and creates its column if it's valid.
        /// </summary>
        /// <param name="declaration">The declaration to check.</param>
        /// <param name="warnings">The list to which the issue is added if the declaration is invalid.</param>
        private static void Check(Declaration declaration, ICollection<string> warnings)
        {
            declaration.IsChecked = true;
            MethodInfo method = declaration.Method;
            ComputedColumnAttribute attribute = declaration.Attribute;

            string reason = declaration.SkipReason;
            ComputedMethod computedMethod = null;
            if (reason == null)
                ComputedMethod.TryCreate(method, attribute.TargetType, out computedMethod, out reason);

            if (reason != null)
            {
                warnings.Add($"The computed column {method.DeclaringType}.{method.Name}() is skipped: {reason}.");
                return;
            }

            string name = !string.IsNullOrWhiteSpace(attribute.Name)
                ? attribute.Name.Trim()
                : ObjectNames.NicifyVariableName(method.Name);

            string tooltip = !string.IsNullOrWhiteSpace(attribute.Tooltip) ? attribute.Tooltip : null;
            declaration.Column = new ColumnDefinition(declaration.Id, computedMethod, name, tooltip, attribute.Width);
        }

        #endregion

    }

}
