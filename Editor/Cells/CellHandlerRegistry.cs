using System;
using System.Collections.Generic;
using System.Reflection;

using UnityEngine;
using UnityEditor;

namespace SideXP.Database.EditorOnly
{

    /// <summary>
    /// Finds the <see cref="CellHandler"/> of each column, among the handlers registered with <see cref="CustomCellHandlerAttribute"/>.
    /// </summary>
    internal class CellHandlerRegistry
    {

        #region Subclasses

        /// <summary>
        /// A handler registered for a target type.
        /// </summary>
        private class Registration
        {

            /// <summary>
            /// The shared instance of the handler.
            /// </summary>
            public CellHandler Handler;

            /// <inheritdoc cref="CustomCellHandlerAttribute.UseForDerivedTypes"/>
            public bool UseForDerivedTypes;

            /// <summary>
            /// Checks if the handler is one of the built-in handlers of the package.
            /// </summary>
            public bool IsBuiltIn;

        }

        #endregion


        #region Fields

        /// <summary>
        /// The registry of the handlers of the project, built the first time it's needed. Handlers only change when scripts are
        /// compiled, so it stays valid until the next domain reload.
        /// </summary>
        private static CellHandlerRegistry s_default = null;

        /// <summary>
        /// The handler used when no handler is registered for a column.
        /// </summary>
        private CellHandler _defaultHandler = new DefaultCellHandler();

        /// <summary>
        /// The registered handlers, mapped by their target type (attribute type or value type).
        /// </summary>
        private Dictionary<Type, Registration> _registrations = new Dictionary<Type, Registration>();

        #endregion


        #region Lifecycle

        /// <summary>
        /// Registers handlers.
        /// </summary>
        /// <param name="handlerTypes">The types of the handlers to register, marked with <see cref="CustomCellHandlerAttribute"/>.</param>
        /// <param name="warnings">The list to which the issues found in the declarations of the handlers are added.</param>
        public CellHandlerRegistry(IEnumerable<Type> handlerTypes, ICollection<string> warnings)
        {
            // Sort the types, so the result doesn't depend on the order of the type cache when two handlers have the same target
            List<Type> sortedTypes = new List<Type>(handlerTypes);
            sortedTypes.Sort((a, b) => string.CompareOrdinal(a.FullName, b.FullName));

            foreach (Type handlerType in sortedTypes)
            {
                if (!TryCreateHandler(handlerType, warnings, out CellHandler handler))
                    continue;

                bool isBuiltIn = handlerType.Assembly == typeof(CellHandler).Assembly;
                foreach (CustomCellHandlerAttribute attribute in handlerType.GetCustomAttributes<CustomCellHandlerAttribute>(false))
                    Register(handlerType, handler, attribute, isBuiltIn, warnings);
            }
        }

        #endregion


        #region Public API

        /// <summary>
        /// Gets the registry of the handlers of the project.
        /// </summary>
        public static CellHandlerRegistry Default
        {
            get
            {
                if (s_default != null)
                    return s_default;

                List<string> warnings = new List<string>();
                s_default = new CellHandlerRegistry(TypeCache.GetTypesWithAttribute<CustomCellHandlerAttribute>(), warnings);
                if (warnings.Count > 0)
                {
                    Debug.LogWarning($"{DatabaseConstants.LogPrefix}{warnings.Count} issue(s) found in the declarations of the cell " +
                        $"handlers:\n- {string.Join("\n- ", warnings)}");
                }

                return s_default;
            }
        }

        /// <inheritdoc cref="_defaultHandler"/>
        public CellHandler DefaultHandler => _defaultHandler;

        /// <summary>
        /// Gets the handler of a column: the handler of the first attribute of its field that has one, or the handler of the type of
        /// its values, or the default handler.
        /// </summary>
        /// <param name="field">The field displayed in the column, or null if the column doesn't display a field.</param>
        /// <param name="valueType">The type of the values of the column.</param>
        /// <returns>Returns the handler of the column.</returns>
        public CellHandler GetHandler(FieldInfo field, Type valueType)
        {
            CellHandler handler;
            if (field != null)
            {
                foreach (object attribute in field.GetCustomAttributes(true))
                {
                    if (TryGetRegisteredHandler(attribute.GetType(), out handler))
                        return handler;
                }
            }

            if (valueType != null && TryGetRegisteredHandler(valueType, out handler))
                return handler;

            return _defaultHandler;
        }

        #endregion


        #region Private API

        /// <summary>
        /// Creates the shared instance of a handler.
        /// </summary>
        /// <param name="handlerType">The type of the handler.</param>
        /// <param name="warnings">The list to which the issues found are added.</param>
        /// <param name="handler">Outputs the created handler.</param>
        /// <returns>Returns true if the handler has been created.</returns>
        private static bool TryCreateHandler(Type handlerType, ICollection<string> warnings, out CellHandler handler)
        {
            handler = null;
            string label = $"[{nameof(CustomCellHandlerAttribute)}] is ignored on {handlerType}";
            if (!typeof(CellHandler).IsAssignableFrom(handlerType))
            {
                warnings.Add($"{label}: a cell handler must derive from {nameof(CellHandler)}.");
                return false;
            }

            if (handlerType.IsAbstract || handlerType.ContainsGenericParameters)
            {
                warnings.Add($"{label}: a cell handler can't be abstract or an open generic type.");
                return false;
            }

            if (handlerType.GetConstructor(Type.EmptyTypes) == null)
            {
                warnings.Add($"{label}: a cell handler needs a public constructor without parameters.");
                return false;
            }

            handler = (CellHandler)Activator.CreateInstance(handlerType);
            return true;
        }

        /// <summary>
        /// Registers a handler for the target type of one of its attributes.
        /// </summary>
        /// <param name="handlerType">The type of the handler.</param>
        /// <param name="handler">The shared instance of the handler.</param>
        /// <param name="attribute">The attribute that declares the target type.</param>
        /// <param name="isBuiltIn"><inheritdoc cref="Registration.IsBuiltIn" path="/summary"/></param>
        /// <param name="warnings">The list to which the issues found are added.</param>
        private void Register(
            Type handlerType,
            CellHandler handler,
            CustomCellHandlerAttribute attribute,
            bool isBuiltIn,
            ICollection<string> warnings)
        {
            if (attribute.TargetType == null)
            {
                warnings.Add($"A [{nameof(CustomCellHandlerAttribute)}] of {handlerType} is ignored: its target type is null.");
                return;
            }

            if (_registrations.TryGetValue(attribute.TargetType, out Registration existing))
            {
                // A handler of the project replaces a built-in one, but two handlers of the same kind are a conflict
                if (existing.IsBuiltIn == isBuiltIn)
                {
                    warnings.Add($"{handlerType} is ignored for {attribute.TargetType}: {existing.Handler.GetType()} is already " +
                        "registered for that type.");
                    return;
                }

                if (!existing.IsBuiltIn)
                    return;
            }

            _registrations[attribute.TargetType] = new Registration
            {
                Handler = handler,
                UseForDerivedTypes = attribute.UseForDerivedTypes,
                IsBuiltIn = isBuiltIn,
            };
        }

        /// <summary>
        /// Gets the handler registered for a type, or for its nearest base type registered with
        /// <see cref="CustomCellHandlerAttribute.UseForDerivedTypes"/>.
        /// </summary>
        /// <param name="type">The attribute type or value type of which to get the handler.</param>
        /// <param name="handler">Outputs the found handler.</param>
        /// <returns>Returns true if a handler has been found.</returns>
        private bool TryGetRegisteredHandler(Type type, out CellHandler handler)
        {
            if (_registrations.TryGetValue(type, out Registration registration))
            {
                handler = registration.Handler;
                return true;
            }

            for (Type baseType = type.BaseType; baseType != null; baseType = baseType.BaseType)
            {
                if (_registrations.TryGetValue(baseType, out registration) && registration.UseForDerivedTypes)
                {
                    handler = registration.Handler;
                    return true;
                }
            }

            handler = null;
            return false;
        }

        #endregion

    }

}
