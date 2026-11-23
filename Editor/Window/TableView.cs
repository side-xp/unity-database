using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UIElements;

using Object = UnityEngine.Object;

namespace SideXP.Database.EditorOnly
{

    /// <summary>
    /// Displays the rows of a table: header, rows and status line.
    /// </summary>
    /// <remarks>
    /// The view builds the pipeline of the table when the table is set: it loads the assets, validates the columns against the first
    /// asset, and creates an accessor per column. Rows are virtualized: only the visible ones have elements, which show the cached
    /// values of the pipeline.
    /// </remarks>
    internal class TableView : VisualElement
    {

        #region Fields

        private const string ViewClass = "database-table-view";
        private const string ListClass = "database-table-view__list";
        private const string HeaderClass = "database-table-view__header";
        private const string CellClass = "database-table-view__cell";
        private const string StatusBarClass = "database-status-bar";
        private const string StatusLabelClass = "database-status-bar__label";
        private const string MessageClass = "database-message";

        private const string NoAssetMessage = "There is no asset of this table in the project.";

        /// <summary>
        /// The height of the rows, in pixels. Smaller than the default height of the list views of the editor, so more rows fit, like
        /// in a spreadsheet.
        /// </summary>
        private const float RowHeight = 20;

        /// <summary>
        /// The initial width of the flexible columns, in pixels. They grow to fill the width left by the other columns.
        /// </summary>
        private const float FlexibleColumnWidth = 120;

        /// <summary>
        /// The minimum width of all the columns when they are resized, in pixels.
        /// </summary>
        private const float MinColumnWidth = 40;

        /// <summary>
        /// The displayed table, or null if none has been set.
        /// </summary>
        private TableDefinition _table = null;

        /// <summary>
        /// Computes the rows of the table.
        /// </summary>
        private TablePipeline _pipeline = null;

        /// <summary>
        /// The displayed columns. The index of a column is its index in the pipeline.
        /// </summary>
        private List<TableViewColumn> _columns = new List<TableViewColumn>();

        /// <summary>
        /// The visible rows of the pipeline, copied after each update, used as the source of the list view.
        /// </summary>
        private List<TableRow> _rows = new List<TableRow>();

        /// <summary>
        /// The list that displays the rows. Rebuilt for each table, since its columns change.
        /// </summary>
        private MultiColumnListView _listView = null;

        /// <summary>
        /// The message shown instead of the rows when the table has no asset.
        /// </summary>
        private Label _messageLabel = null;

        /// <summary>
        /// The label of the status line.
        /// </summary>
        private Label _statusLabel = null;

        #endregion


        #region Lifecycle

        /// <inheritdoc cref="TableView"/>
        public TableView()
        {
            AddToClassList(ViewClass);

            _messageLabel = new Label(NoAssetMessage);
            _messageLabel.AddToClassList(MessageClass);
            _messageLabel.SetDisplayed(false);
            Add(_messageLabel);

            VisualElement statusBar = new VisualElement();
            statusBar.AddToClassList(StatusBarClass);
            _statusLabel = new Label();
            _statusLabel.AddToClassList(StatusLabelClass);
            statusBar.Add(_statusLabel);
            Add(statusBar);
        }

        #endregion


        #region Public API

        /// <inheritdoc cref="_table"/>
        public TableDefinition Table => _table;

        /// <summary>
        /// Displays a table: loads its assets, and builds its columns and its pipeline.
        /// </summary>
        /// <param name="table">The table to display.</param>
        public void SetTable(TableDefinition table)
        {
            _table = table;

            ProjectAssetSource source = new ProjectAssetSource(table.Type);
            IReadOnlyList<Object> assets = source.LoadAssets();
            _columns = CreateColumns(table, assets.Count > 0 ? assets[0] : null);

            IValueAccessor[] accessors = new IValueAccessor[_columns.Count];
            for (int i = 0; i < _columns.Count; i++)
                accessors[i] = _columns[i].Accessor;

            _pipeline = new TablePipeline(new PreloadedAssetSource(assets, source), accessors);

            RebuildListView();
            Refresh();
        }

        /// <summary>
        /// Updates the pipeline, and the display if the rows have changed.
        /// </summary>
        public void Refresh()
        {
            if (_pipeline == null)
                return;

            if (_pipeline.Update())
            {
                _rows.Clear();
                _rows.AddRange(_pipeline.VisibleRows);
                _listView.RefreshItems();
            }

            bool hasAssets = _pipeline.Rows.Count > 0;
            _listView.SetDisplayed(hasAssets);
            _messageLabel.SetDisplayed(!hasAssets);
            _statusLabel.text = _rows.Count == 1 ? "1 row" : $"{_rows.Count} rows";
        }

        #endregion


        #region UI

        /// <summary>
        /// Creates the list view with the current columns, replacing the previous one.
        /// </summary>
        private void RebuildListView()
        {
            if (_listView != null)
                _listView.RemoveFromHierarchy();

            _listView = new MultiColumnListView
            {
                itemsSource = _rows,
                fixedItemHeight = RowHeight,
                selectionType = SelectionType.None,
                sortingMode = ColumnSortingMode.None,
                horizontalScrollingEnabled = true,
                showAlternatingRowBackgrounds = AlternatingRowBackground.ContentOnly,
            };
            _listView.AddToClassList(ListClass);
            _listView.columns.stretchMode = Columns.StretchMode.GrowAndFill;

            for (int i = 0; i < _columns.Count; i++)
                _listView.columns.Add(CreateListColumn(_columns[i], i));

            // Keep the list above the status line
            Insert(IndexOf(_messageLabel), _listView);
        }

        /// <summary>
        /// Creates the column of the list view that displays a column of the table.
        /// </summary>
        /// <param name="column">The column of the table.</param>
        /// <param name="columnIndex">The index of the column in the pipeline.</param>
        /// <returns>Returns the created column.</returns>
        private Column CreateListColumn(TableViewColumn column, int columnIndex)
        {
            return new Column
            {
                name = column.Id,
                title = column.Name,
                width = column.IsFlexible ? FlexibleColumnWidth : column.Width,
                minWidth = MinColumnWidth,
                stretchable = column.IsFlexible,
                resizable = true,
                sortable = false,
                makeHeader = () => MakeHeader(column),
                makeCell = () => MakeCell(column),
                bindCell = (element, rowIndex) => BindCell(element, column, columnIndex, _rows[rowIndex]),
            };
        }

        /// <summary>
        /// Creates the header of a column, with its tooltip.
        /// </summary>
        /// <param name="column">The column of which to create the header.</param>
        /// <returns>Returns the created header.</returns>
        private static VisualElement MakeHeader(TableViewColumn column)
        {
            Label header = new Label(column.Name) { tooltip = column.Tooltip };
            header.AddToClassList(HeaderClass);
            return header;
        }

        /// <summary>
        /// Creates the element of a cell: a container, which holds the display created by the handler of the column.
        /// </summary>
        /// <param name="column">The column of the cell.</param>
        /// <returns>Returns the created element.</returns>
        private static VisualElement MakeCell(TableViewColumn column)
        {
            VisualElement cell = new VisualElement();
            cell.AddToClassList(CellClass);
            cell.Add(column.Handler.CreateDisplay());
            return cell;
        }

        /// <summary>
        /// Shows the cached value of a row in a cell, through the handler of the column.
        /// </summary>
        /// <remarks>
        /// An error of the handler is logged once per column, and doesn't prevent the other cells from being displayed.
        /// </remarks>
        /// <param name="cell">The element created by <see cref="MakeCell(TableViewColumn)"/>.</param>
        /// <param name="column">The column of the cell.</param>
        /// <param name="columnIndex">The index of the column in the pipeline.</param>
        /// <param name="row">The row of the cell.</param>
        private static void BindCell(VisualElement cell, TableViewColumn column, int columnIndex, TableRow row)
        {
            try
            {
                CellContext context = new CellContext(row.Asset, column.Field, column.Accessor.ValueType);
                column.Handler.BindDisplay(cell[0], row.GetValue(columnIndex), context);
            }
            catch (Exception exception)
            {
                if (column.HasLoggedError)
                    return;

                column.HasLoggedError = true;
                Debug.LogError($"{DatabaseConstants.LogPrefix}The cell handler {column.Handler.GetType()} failed to display a value of " +
                    $"the column \"{column.Name}\". The next errors of this column are not logged.");
                Debug.LogException(exception);
            }
        }

        #endregion


        #region Private API

        /// <summary>
        /// Creates the columns displayed for a table: the <i>Asset Name</i> column (unless the table hides it), then the columns of the
        /// definition that are valid.
        /// </summary>
        /// <param name="table">The table of which to create the columns.</param>
        /// <param name="firstAsset">An asset of the table, used to validate the columns, or null if the table has no asset. In that case,
        /// all the columns of the definition are kept.</param>
        /// <returns>Returns the created columns.</returns>
        private static List<TableViewColumn> CreateColumns(TableDefinition table, Object firstAsset)
        {
            IReadOnlyList<ColumnDefinition> definitionColumns = table.Columns;
            if (firstAsset != null)
            {
                List<string> warnings = new List<string>();
                definitionColumns = ColumnValidation.GetValidColumns(table.Columns, firstAsset, warnings);
                if (warnings.Count > 0)
                {
                    Debug.LogWarning($"{DatabaseConstants.LogPrefix}{warnings.Count} issue(s) found in the columns of the table " +
                        $"\"{table.Name}\":\n- {string.Join("\n- ", warnings)}");
                }
            }

            List<TableViewColumn> columns = new List<TableViewColumn>(definitionColumns.Count + 1);
            if (!table.HideAssetName)
                columns.Add(TableViewColumn.CreateAssetNameColumn());

            foreach (ColumnDefinition column in definitionColumns)
                columns.Add(TableViewColumn.FromDefinition(column));

            return columns;
        }

        #endregion

    }

}
