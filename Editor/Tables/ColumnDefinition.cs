using System.Collections.Generic;
using System.Reflection;

namespace SideXP.Database.EditorOnly
{

    /// <summary>
    /// Describes a column of a table: the source of its values, its header and its width.
    /// </summary>
    /// <remarks>
    /// The values of a column come from a serialized property (<see cref="PropertyPath"/>), or from the method of a computed column
    /// (<see cref="Computed"/>), which is read-only.
    /// </remarks>
    internal class ColumnDefinition
    {

        #region Fields

        /// <summary>
        /// The stable identifier of the column in its table, used to remember its settings. For a column that displays a serialized
        /// field, it's the property path of that field. For a computed column, see <see cref="ComputedColumns.GetId"/>.
        /// </summary>
        private string _id = null;

        /// <summary>
        /// The path of the serialized property that holds the values of this column, from the asset. Null for a computed column.
        /// </summary>
        private string _propertyPath = null;

        /// <summary>
        /// The method that computes the values of this column, or null if the column displays a serialized property.
        /// </summary>
        private ComputedMethod _computed = null;

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
        public ColumnDefinition(string id, string propertyPath, IReadOnlyList<FieldInfo> fields, string name, string tooltip, float width)
        {
            _id = id;
            _propertyPath = propertyPath;
            _fields = fields;
            _name = name;
            _tooltip = tooltip;
            _width = width;
        }

        /// <summary>
        /// Creates the definition of a computed column.
        /// </summary>
        /// <param name="id"><inheritdoc cref="_id" path="/summary"/></param>
        /// <param name="computed"><inheritdoc cref="_computed" path="/summary"/></param>
        /// <param name="name"><inheritdoc cref="_name" path="/summary"/></param>
        /// <param name="tooltip"><inheritdoc cref="_tooltip" path="/summary"/></param>
        /// <param name="width"><inheritdoc cref="_width" path="/summary"/></param>
        public ColumnDefinition(string id, ComputedMethod computed, string name, string tooltip, float width)
        {
            _id = id;
            _computed = computed;
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

        /// <inheritdoc cref="_computed"/>
        public ComputedMethod Computed => _computed;

        /// <summary>
        /// Checks if the values of the column are computed by a method, instead of read from a serialized property.
        /// </summary>
        public bool IsComputed => _computed != null;

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
