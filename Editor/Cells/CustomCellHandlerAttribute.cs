using System;

namespace SideXP.Database.EditorOnly
{

    /// <summary>
    /// Registers a <see cref="CellHandler"/> for the fields that have an attribute, or for the values of a type, like
    /// <see cref="UnityEditor.CustomPropertyDrawer"/> does for property drawers.
    /// </summary>
    /// <remarks>
    /// The handler of a column is selected by the attributes of its field first (in declaration order), then by the type of its
    /// values. Without a handler, the default one displays the values as text. A handler declared in a project replaces a built-in
    /// handler of the package for the same type.
    /// </remarks>
    /// <example>
    /// <code>
    /// // In a runtime assembly
    /// public class PercentAttribute : Attribute { }
    ///
    /// // In an editor assembly
    /// [CustomCellHandler(typeof(PercentAttribute))]
    /// public class PercentCellHandler : CellHandler { }
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public class CustomCellHandlerAttribute : Attribute
    {

        #region Lifecycle

        /// <inheritdoc cref="CustomCellHandlerAttribute"/>
        /// <param name="targetType"><inheritdoc cref="TargetType" path="/summary"/></param>
        public CustomCellHandlerAttribute(Type targetType)
        {
            TargetType = targetType;
        }

        #endregion


        #region Public API

        /// <summary>
        /// The attribute type, or the value type, handled by the marked handler.
        /// </summary>
        public Type TargetType { get; }

        /// <summary>
        /// Uses the marked handler for the types derived from <see cref="TargetType"/> too, unless they have their own handler.
        /// </summary>
        public bool UseForDerivedTypes { get; set; } = false;

        #endregion

    }

}
