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
    /// header. A field displays a single column for now: extra <see cref="ColumnAttribute"/> on the same field are ignored, with a
    /// warning. This attribute is stripped from player builds.
    /// </remarks>
    /// <example>
    /// <code>
    /// [SerializeField]
    /// [Column("Price", Width = 80)]
    /// private int _basePrice = 0;
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
        /// The column header text. If null or blank, the nicified field name is used (<c>_basePrice</c> becomes <i>Base Price</i>).
        /// </summary>
        public string Name { get; set; } = null;

        /// <summary>
        /// The default width of the column, in pixels. If 0 or negative, the column is flexible.
        /// </summary>
        public float Width { get; set; } = 0;

        #endregion

    }

}
