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
    /// The sub-assets hidden from the Project window are not listed. The tables of types marked with <see cref="ObsoleteAttribute"/>
    /// have a mark in the window.
    /// <para>
    /// The window remembers the layout of each table, identified by the GUID of the script file of its type: renaming the type keeps
    /// the layout, as long as its script file is renamed from Unity. A type without a script file of its own (a nested type, a type
    /// compiled in a DLL) is identified by its full name instead. This attribute is stripped from player builds.
    /// </para>
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
        /// Hides the <i>Asset Name</i> built-in column by default. The user can still show it from the context menu of the column
        /// headers, and the asset names are still searched.
        /// </summary>
        public bool HideAssetName { get; set; } = false;

        /// <summary>
        /// Allows the assets of this type to be renamed from the <i>Asset Name</i> built-in column, in every table that lists them.
        /// Enabled by default.
        /// </summary>
        /// <remarks>
        /// The option belongs to the type, not to the table: the nearest declaration of an asset's type or its base types decides, even
        /// if it's not <see cref="Inherited"/>. For example, disabling it on the declaration of a <c>ShieldSO</c> type prevents renaming
        /// the shields in its own table, and also in the table of its base type <c>EquipmentSO</c>, where the other equipment can still
        /// be renamed.
        /// </remarks>
        public bool AllowRenameAsset { get; set; } = true;

        #endregion

    }

}
