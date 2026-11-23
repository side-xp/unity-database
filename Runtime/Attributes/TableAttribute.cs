using System;
using System.Diagnostics;

namespace SideXP.Database
{

    /// <summary>
    /// Makes a <see cref="UnityEngine.ScriptableObject"/> type available as a table in the Database window. The table lists every
    /// asset of the type found in the project, including assets of derived types and sub-assets.
    /// </summary>
    /// <remarks>
    /// Abstract types can be tables, to list a family of types. Open generic types are never tables, since they can't have assets.
    /// This attribute is stripped from player builds.
    /// </remarks>
    /// <example>
    /// <code>
    /// [Table("Items/All", Inherited = true)]
    /// public class ItemSO : ScriptableObject { }
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    [Conditional("UNITY_EDITOR")]
    public class TableAttribute : Attribute
    {

        #region Lifecycle

        /// <summary>
        /// Makes the marked type a table named after the type.
        /// </summary>
        public TableAttribute() { }

        /// <summary>
        /// Makes the marked type a table with a given name.
        /// </summary>
        /// <param name="name"><inheritdoc cref="Name" path="/summary"/></param>
        public TableAttribute(string name)
        {
            Name = name;
        }

        #endregion


        #region Public API

        /// <summary>
        /// The name of the table. Can contain <c>/</c> to group tables in the table list (for example <c>"Items/Generic"</c>). If null
        /// or blank, the nicified type name is used.
        /// </summary>
        public string Name { get; set; } = null;

        /// <summary>
        /// Makes this type and all its derived types tables. A derived type can't opt out.
        /// </summary>
        /// <remarks>
        /// The nearest declaration wins: a type's own <see cref="TableAttribute"/> replaces its parent's, and inheritance stops at a
        /// declaration without <see cref="Inherited"/>. An inherited table is named after its type, in the same group as the parent's
        /// table, and copies the other options of the parent's declaration.
        /// </remarks>
        public bool Inherited { get; set; } = false;

        /// <summary>
        /// Hides the <i>Asset Name</i> built-in column by default.
        /// </summary>
        public bool HideAssetName { get; set; } = false;

        /// <summary>
        /// Makes the <i>Asset Name</i> built-in column editable, so assets can be renamed from the table.
        /// </summary>
        public bool AllowRenameAsset { get; set; } = false;

        #endregion

    }

}
