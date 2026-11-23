using System.Collections.Generic;
using System.Reflection;

namespace SideXP.Database.EditorOnly
{

    /// <summary>
    /// Describes a column of a table: the source of its values, its header and its width.
    /// </summary>
    public class ColumnDefinition
    {

        #region Fields

        /// <summary>
        /// The stable identifier of the column in its table, used to remember its settings. For a column that displays a serialized
        /// field, it's the property path of that field.
        /// </summary>
        private string _id = null;

        /// <summary>
        /// The path of the serialized property that holds the values of this column, from the asset.
        /// </summary>
        private string _propertyPath = null;

        /// <summary>
        /// The chain of C# fields from the asset to the value, or null if the property path isn't made of C# fields (for example on
        /// Unity types implemented in native code).
        /// </summary>
        private IReadOnlyList<FieldInfo> _fields = null;

        /// <summary>
        /// The header text of the column.
        /// </summary>
        private string _name = null;

        /// <summary>
        /// The text shown when hovering the column header, or null if there is none.
        /// </summary>
        private string _tooltip = null;

        /// <summary>
        /// The default width of the column, in pixels. If 0 or negative, the column is flexible.
        /// </summary>
        private float _width = 0;

        #endregion


        #region Lifecycle

        /// <inheritdoc cref="ColumnDefinition"/>
        /// <param name="id"><inheritdoc cref="_id" path="/summary"/></param>
        /// <param name="propertyPath"><inheritdoc cref="_propertyPath" path="/summary"/></param>
        /// <param name="fields"><inheritdoc cref="_fields" path="/summary"/></param>
        /// <param name="name"><inheritdoc cref="_name" path="/summary"/></param>
        /// <param name="tooltip"><inheritdoc cref="_tooltip" path="/summary"/></param>
        /// <param name="width"><inheritdoc cref="_width" path="/summary"/></param>
        internal ColumnDefinition(string id, string propertyPath, IReadOnlyList<FieldInfo> fields, string name, string tooltip, float width)
        {
            _id = id;
            _propertyPath = propertyPath;
            _fields = fields;
            _name = name;
            _tooltip = tooltip;
            _width = width;
        }

        #endregion


        #region Public API

        /// <inheritdoc cref="_id"/>
        public string Id => _id;

        /// <inheritdoc cref="_propertyPath"/>
        public string PropertyPath => _propertyPath;

        /// <inheritdoc cref="_fields"/>
        public IReadOnlyList<FieldInfo> Fields => _fields;

        /// <inheritdoc cref="_name"/>
        public string Name => _name;

        /// <inheritdoc cref="_tooltip"/>
        public string Tooltip => _tooltip;

        /// <inheritdoc cref="_width"/>
        public float Width => _width;

        /// <summary>
        /// Checks if the column shares the width left by the columns that have a fixed width.
        /// </summary>
        public bool IsFlexible => _width <= 0;

        #endregion

    }

}
