using System;
using System.Diagnostics;

namespace SideXP.Database
{

    /// <summary>
    /// Displays a field as a column of its type's table (see <see cref="TableAttribute"/>).
    /// </summary>
    /// <remarks>
    /// The field must be serialized by Unity: public, or marked with <see cref="UnityEngine.SerializeField"/>. It can be declared on
    /// the table type or any of its base types. Columns are ordered from the root base type down to the table type, then in declaration
    /// order within each type. If the field has a <see cref="UnityEngine.TooltipAttribute"/>, its text is shown when hovering the column
    /// header. The fields that Unity can't serialize are skipped when the table opens, with a warning.
    /// <para>
    /// A field can declare several columns, each displaying a field nested in it with <see cref="Path"/> (the fields of a serializable
    /// struct or class). The columns of a field are ordered like their attributes. Two columns that display the same value are not
    /// allowed: the second one is skipped, with a warning.
    /// </para>
    /// <para>
    /// The backing field of an auto-property can be a column too (<c>[field: SerializeField, Column]</c>): the column is named after
    /// the property. This attribute is stripped from player builds.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// [SerializeField]
    /// [Column("Price", Width = 80)]
    /// private int _basePrice = 0;
    ///
    /// // One column per stat, named "Attack" and "Defense"
    /// [SerializeField]
    /// [Column(Path = "attack")]
    /// [Column(Path = "defense")]
    /// private Stats _stats;
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    [Conditional("UNITY_EDITOR")]
    public class ColumnAttribute : Attribute
    {

        #region Lifecycle

        /// <summary>
        /// Displays the marked field as a column named after the field.
        /// </summary>
        public ColumnAttribute() { }

        /// <summary>
        /// Displays the marked field as a column with a given name.
        /// </summary>
        /// <param name="name"><inheritdoc cref="Name" path="/summary"/></param>
        public ColumnAttribute(string name)
        {
            Name = name;
        }

        #endregion


        #region Public API

        /// <summary>
        /// The column header text. If null or blank, the nicified field name is used (<c>_basePrice</c> becomes <i>Base Price</i>), or
        /// the nicified name of the last field of the <see cref="Path"/>.
        /// </summary>
        public string Name { get; set; } = null;

        /// <summary>
        /// The default width of the column, in pixels. If 0 or negative, the column is flexible.
        /// </summary>
        public float Width { get; set; } = 0;

        /// <summary>
        /// The path of a field nested in the marked field, to display instead of the marked field itself: the names of the fields,
        /// separated with dots (<c>"attack"</c>, or <c>"stats.attack"</c> for a field nested deeper). If null or blank, the column
        /// displays the marked field.
        /// </summary>
        /// <remarks>
        /// Each field of the path must be serialized by Unity, like the marked field, and can be declared in the type that contains it
        /// or in its base types. The name of an auto-property can be used for its backing field. A path can't go through a reference to
        /// an object (an asset), an array or a list, or a field marked with <see cref="UnityEngine.SerializeReference"/>. The columns of
        /// an invalid path are skipped, with a warning.
        /// </remarks>
        public string Path { get; set; } = null;

        #endregion

    }

}
