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

        #endregion


        #region Lifecycle

        /// <inheritdoc cref="TableViewColumn"/>
        /// <param name="id"><inheritdoc cref="_id" path="/summary"/></param>
        /// <param name="name"><inheritdoc cref="_name" path="/summary"/></param>
        /// <param name="tooltip"><inheritdoc cref="_tooltip" path="/summary"/></param>
        /// <param name="width"><inheritdoc cref="_width" path="/summary"/></param>
        /// <param name="accessor"><inheritdoc cref="_accessor" path="/summary"/></param>
        public TableViewColumn(string id, string name, string tooltip, float width, IValueAccessor accessor)
        {
            _id = id;
            _name = name;
            _tooltip = tooltip;
            _width = width;
            _accessor = accessor;
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

        /// <summary>
        /// Creates the column that displays a column of a table definition.
        /// </summary>
        /// <param name="column">The column of the table definition.</param>
        /// <returns>Returns the created column.</returns>
        public static TableViewColumn FromDefinition(ColumnDefinition column)
        {
            return new TableViewColumn(column.Id, column.Name, column.Tooltip, column.Width, ValueAccessors.Create(column));
        }

        /// <summary>
        /// Creates the <i>Asset Name</i> built-in column.
        /// </summary>
        /// <returns>Returns the created column.</returns>
        public static TableViewColumn CreateAssetNameColumn()
        {
            return new TableViewColumn(AssetNameId, AssetNameTitle, AssetNameTooltip, AssetNameWidth, AssetNameAccessor.Instance);
        }

        #endregion

    }

}
