using System;
using System.Reflection;

using UnityEngine;

namespace SideXP.Database.EditorOnly
{

    /// <summary>
    /// A column displayed in a table view: a column of the table definition, or a built-in column.
    /// </summary>
    internal class TableViewColumn
    {

        #region Fields

        /// <summary>
        /// The identifier of the <i>Asset Name</i> built-in column. The <c>$</c> prefix can't appear in a property path, so it never
        /// conflicts with the identifier of a field column.
        /// </summary>
        public const string AssetNameId = "$AssetName";

        private const string AssetNameTitle = "Asset Name";
        private const string AssetNameTooltip = "The name of the asset.";

        /// <summary>
        /// The default width of the <i>Asset Name</i> column, in pixels. Fixed, so the name doesn't take the space of the other columns
        /// in a wide window.
        /// </summary>
        private const float AssetNameWidth = 180;

        /// <summary>
        /// The stable identifier of the column in its table.
        /// </summary>
        private string _id = null;

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

        /// <summary>
        /// Reads the values of the column.
        /// </summary>
        private IValueAccessor _accessor = null;

        /// <summary>
        /// The field displayed in the column, or null if the column doesn't display a field.
        /// </summary>
        private FieldInfo _field = null;

        /// <summary>
        /// The path of the serialized property that holds the values of the column, from the asset, or null if the values are not
        /// stored in a serialized property (for example a built-in column).
        /// </summary>
        private string _propertyPath = null;

        /// <summary>
        /// Displays the values of the column, and converts them to text.
        /// </summary>
        private CellHandler _handler = null;

        /// <summary>
        /// Checks if the column can be sorted, as decided by its handler when the column is created.
        /// </summary>
        private bool _isSortable = false;

        /// <summary>
        /// Checks if an error of the handler has already been logged for this column, so it's logged once instead of once per cell.
        /// </summary>
        private bool _hasLoggedError = false;

        #endregion


        #region Lifecycle

        /// <summary>
        /// Creates a column, selects its handler, and asks the handler if the column can be sorted.
        /// </summary>
        /// <param name="id"><inheritdoc cref="_id" path="/summary"/></param>
        /// <param name="name"><inheritdoc cref="_name" path="/summary"/></param>
        /// <param name="tooltip"><inheritdoc cref="_tooltip" path="/summary"/></param>
        /// <param name="width"><inheritdoc cref="_width" path="/summary"/></param>
        /// <param name="accessor"><inheritdoc cref="_accessor" path="/summary"/></param>
        /// <param name="field"><inheritdoc cref="_field" path="/summary"/></param>
        /// <param name="propertyPath"><inheritdoc cref="_propertyPath" path="/summary"/></param>
        public TableViewColumn(string id, string name, string tooltip, float width, IValueAccessor accessor, FieldInfo field,
            string propertyPath)
        {
            _id = id;
            _name = name;
            _tooltip = tooltip;
            _width = width;
            _accessor = accessor;
            _field = field;
            _propertyPath = propertyPath;
            _handler = CellHandlerRegistry.Default.GetHandler(field, accessor.ValueType);

            try
            {
                _isSortable = _handler.CanCompare(accessor.ValueType);
            }
            catch (Exception exception)
            {
                // A broken custom handler makes the column unsortable, instead of preventing the table from opening
                Debug.LogError($"{DatabaseConstants.LogPrefix}The cell handler {_handler.GetType()} failed to tell if the column " +
                    $"\"{name}\" can be sorted. The column can't be sorted.");
                Debug.LogException(exception);
            }
        }

        #endregion


        #region Public API

        /// <inheritdoc cref="_id"/>
        public string Id => _id;

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

        /// <inheritdoc cref="_accessor"/>
        public IValueAccessor Accessor => _accessor;

        /// <inheritdoc cref="_field"/>
        public FieldInfo Field => _field;

        /// <inheritdoc cref="_propertyPath"/>
        public string PropertyPath => _propertyPath;

        /// <summary>
        /// Checks if the values of the column are stored in a serialized property, which edits can write to.
        /// </summary>
        public bool HasProperty => !string.IsNullOrEmpty(_propertyPath);

        /// <inheritdoc cref="_handler"/>
        public CellHandler Handler => _handler;

        /// <inheritdoc cref="_isSortable"/>
        public bool IsSortable => _isSortable;

        /// <summary>
        /// Checks if the column is the <i>Asset Name</i> built-in column, whose edit renames the asset of the cell if its type allows
        /// it (see <see cref="CodeTables.IsRenameAllowed(Type)"/>).
        /// </summary>
        public bool IsAssetName => _id == AssetNameId;

        /// <inheritdoc cref="_hasLoggedError"/>
        public bool HasLoggedError
        {
            get => _hasLoggedError;
            set => _hasLoggedError = value;
        }

        /// <summary>
        /// Creates the column that displays a column of a table definition.
        /// </summary>
        /// <param name="column">The column of the table definition.</param>
        /// <returns>Returns the created column.</returns>
        public static TableViewColumn FromDefinition(ColumnDefinition column)
        {
            // The attributes that select the handler are on the field that holds the value, the last one of the path
            FieldInfo field = column.Fields != null && column.Fields.Count > 0
                ? column.Fields[column.Fields.Count - 1]
                : null;

            return new TableViewColumn(column.Id, column.Name, column.Tooltip, column.Width, ValueAccessors.Create(column), field,
                column.PropertyPath);
        }

        /// <summary>
        /// Creates the <i>Asset Name</i> built-in column.
        /// </summary>
        /// <returns>Returns the created column.</returns>
        public static TableViewColumn CreateAssetNameColumn()
        {
            // Not a serialized property: the name of a main asset is the name of its file, so it's edited by renaming the asset
            return new TableViewColumn(AssetNameId, AssetNameTitle, AssetNameTooltip, AssetNameWidth, AssetNameAccessor.Instance, null,
                null);
        }

        #endregion

    }

}
