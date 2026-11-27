using System;
using System.Reflection;

using UnityEngine;

using Object = UnityEngine.Object;

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

        /// <summary>
        /// The identifier of the <i>Controls</i> built-in column (see <see cref="AssetNameId"/> for the <c>$</c> prefix).
        /// </summary>
        public const string ControlsId = "$Controls";

        private const string AssetNameTitle = "Asset Name";
        private const string AssetNameTooltip = "The name of the asset.";
        private const string ControlsTitle = "Controls";
        private const string ControlsTooltip = "Select the asset in the Project window, duplicate it or delete it.";
        private const string ComputedTooltip = "Computed column: its values are computed from the assets, and can't be edited.";

        /// <summary>
        /// The default width of the <i>Asset Name</i> column, in pixels. Fixed, so the name doesn't take the space of the other columns
        /// in a wide window.
        /// </summary>
        private const float AssetNameWidth = 180;

        /// <summary>
        /// The default width of the <i>Controls</i> column, in pixels: enough for its three icon buttons, with the padding of the cell.
        /// </summary>
        private const float ControlsWidth = 80;

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

        /// <summary>
        /// Checks if the values of the column are computed by a method (see <see cref="ComputedColumnAttribute"/>). Its cells are
        /// read-only, and its values are computed between the two passes of the filters, so only the second pass and the search can
        /// use them.
        /// </summary>
        public bool IsComputed => _accessor is ComputedAccessor;

        /// <inheritdoc cref="_handler"/>
        public CellHandler Handler => _handler;

        /// <inheritdoc cref="_isSortable"/>
        public bool IsSortable => _isSortable;

        /// <summary>
        /// Checks if the column is the <i>Asset Name</i> built-in column, whose edit renames the asset of the cell if its type allows
        /// it (see <see cref="CodeTables.IsRenameAllowed(Type)"/>).
        /// </summary>
        public bool IsAssetName => _id == AssetNameId;

        /// <summary>
        /// Checks if the column is the <i>Controls</i> built-in column, whose cells display the buttons of the asset operations
        /// instead of a value.
        /// </summary>
        public bool IsControls => _id == ControlsId;

        /// <inheritdoc cref="_hasLoggedError"/>
        public bool HasLoggedError
        {
            get => _hasLoggedError;
            set => _hasLoggedError = value;
        }

        /// <summary>
        /// Converts a value of the column to text through its handler, for the copy, the search, the filters and the export.
        /// </summary>
        /// <remarks>
        /// An error of the handler is logged once per column (see <see cref="HasLoggedError"/>), so a broken custom handler doesn't
        /// break the table.
        /// </remarks>
        /// <param name="value">The cached value of a cell of the column.</param>
        /// <param name="purpose">Why the value is converted, at the end of the error message (for example "for the search").</param>
        /// <returns>Returns the text of the value, or null if the handler failed.</returns>
        public string ToText(object value, string purpose)
        {
            try
            {
                return _handler.ToText(value);
            }
            catch (Exception exception)
            {
                if (!_hasLoggedError)
                {
                    _hasLoggedError = true;
                    Debug.LogError($"{DatabaseConstants.LogPrefix}The cell handler {_handler.GetType()} failed to convert a value of " +
                        $"the column \"{_name}\" to text {purpose}. The next errors of this column are not logged.");
                    Debug.LogException(exception);
                }
                return null;
            }
        }

        /// <summary>
        /// Converts a text to a value of the column through its handler, for the paste and the import.
        /// </summary>
        /// <remarks>
        /// An error of the handler is logged once per column (see <see cref="HasLoggedError"/>), and the text is then not converted.
        /// </remarks>
        /// <param name="text">The text to convert.</param>
        /// <param name="asset">The asset of the cell, or null for an asset that an import will create.</param>
        /// <param name="purpose">Why the text is converted, at the end of the error message (for example "to paste it").</param>
        /// <param name="value">Outputs the converted value, or null if the text can't be converted.</param>
        /// <returns>Returns true if the text has been converted.</returns>
        public bool TryParse(string text, Object asset, string purpose, out object value)
        {
            try
            {
                CellContext context = new CellContext(asset, _field, _accessor.ValueType);
                return _handler.TryParse(text, context, out value);
            }
            catch (Exception exception)
            {
                if (!_hasLoggedError)
                {
                    _hasLoggedError = true;
                    Debug.LogError($"{DatabaseConstants.LogPrefix}The cell handler {_handler.GetType()} failed to convert a text " +
                        $"{purpose} in the column \"{_name}\". The next errors of this column are not logged.");
                    Debug.LogException(exception);
                }

                value = null;
                return false;
            }
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

            // The header of a computed column says that it's read-only, since its cells look like the others
            string tooltip = column.Tooltip;
            if (column.IsComputed)
                tooltip = string.IsNullOrEmpty(tooltip) ? ComputedTooltip : $"{tooltip}\n\n{ComputedTooltip}";

            return new TableViewColumn(column.Id, column.Name, tooltip, column.Width, ValueAccessors.Create(column), field,
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

        /// <summary>
        /// Creates the <i>Controls</i> built-in column.
        /// </summary>
        /// <returns>Returns the created column.</returns>
        public static TableViewColumn CreateControlsColumn()
        {
            TableViewColumn column = new TableViewColumn(ControlsId, ControlsTitle, ControlsTooltip, ControlsWidth,
                EmptyAccessor.Instance, null, null);
            // It has no value to sort, whatever handler a project registers for the type of its empty values
            column._isSortable = false;
            return column;
        }

        #endregion

    }

}
