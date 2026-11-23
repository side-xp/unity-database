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
    /// <para>
    /// The first column is frozen: it's displayed by its own list view, on the left of the list view of the other columns, and stays
    /// visible when the table is scrolled horizontally (see <see cref="FrozenColumnLink"/>).
    /// </para>
    /// </remarks>
    internal class TableView : VisualElement
    {

        #region Fields

        private const string ViewClass = "database-table-view";
        private const string BodyClass = "database-table-view__body";
        private const string ListClass = "database-table-view__list";
        private const string FrozenListClass = "database-table-view__frozen-list";
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
        /// The element that holds the list views, side by side.
        /// </summary>
        private VisualElement _body = null;

        /// <summary>
        /// The list that displays the rows, with all the columns except the frozen one. Rebuilt for each table, since its columns change.
        /// </summary>
        private MultiColumnListView _listView = null;

        /// <summary>
        /// The list that displays the frozen column, or null if the table has a single column. Rebuilt for each table.
        /// </summary>
        private MultiColumnListView _frozenListView = null;

        /// <summary>
        /// Keeps the frozen list aligned with the main list, or null if there is no frozen list.
        /// </summary>
        private FrozenColumnLink _frozenColumnLink = null;

        /// <summary>
        /// Tracks the changes of the assets of the table, or null if no table is displayed. Listens only while the view is attached
        /// to a panel.
        /// </summary>
        private TableChangeTracker _changeTracker = null;

        /// <summary>
        /// Checks if a refresh is scheduled for the next update of the view, so several changes only cause one refresh.
        /// </summary>
        private bool _isRefreshScheduled = false;

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

            _body = new VisualElement();
            _body.AddToClassList(BodyClass);
            Add(_body);

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

            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
        }

        /// <summary>
        /// Called when the view is added to a panel, for example when its window is opened or docked elsewhere.
        /// </summary>
        /// <param name="evt">The emitted event.</param>
        private void OnAttachToPanel(AttachToPanelEvent evt)
        {
            if (_changeTracker == null)
                return;

            // Changes may have been missed while the view was detached
            _changeTracker.Start();
            _pipeline.InvalidateSource();
            ScheduleRefresh();
        }

        /// <summary>
        /// Called when the view is removed from its panel, for example when its window is closed.
        /// </summary>
        /// <param name="evt">The emitted event.</param>
        private void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            _changeTracker?.Stop();
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
            _changeTracker?.Stop();
            _table = table;

            ProjectAssetSource source = new ProjectAssetSource(table.Type);
            IReadOnlyList<Object> assets = source.LoadAssets();
            _columns = CreateColumns(table, assets.Count > 0 ? assets[0] : null);

            IValueAccessor[] accessors = new IValueAccessor[_columns.Count];
            for (int i = 0; i < _columns.Count; i++)
                accessors[i] = _columns[i].Accessor;

            _pipeline = new TablePipeline(new PreloadedAssetSource(assets, source), accessors);

            _changeTracker = new TableChangeTracker(table.Type, _pipeline);
            _changeTracker.Changed += ScheduleRefresh;
            if (panel != null)
                _changeTracker.Start();

            RebuildListViews();
            Refresh();
        }

        /// <summary>
        /// Removes the displayed table, and stops tracking its changes.
        /// </summary>
        public void ClearTable()
        {
            _changeTracker?.Stop();
            _changeTracker = null;
            _table = null;
            _pipeline = null;
            _columns = new List<TableViewColumn>();
            _rows.Clear();

            _body.Clear();
            _listView = null;
            _frozenListView = null;
            _frozenColumnLink = null;
        }

        /// <summary>
        /// Loads the assets of the table again, and reads all their values. Keeps the sort, the filter and the scroll position.
        /// </summary>
        /// <remarks>
        /// Changes are usually detected automatically. This covers the changes the view can't detect, such as a script that writes a
        /// field without notifying Unity.
        /// </remarks>
        public void Reload()
        {
            if (_pipeline == null)
                return;

            _pipeline.InvalidateSource();
            Refresh();
        }

        /// <summary>
        /// Updates the pipeline, and the display if the rows have changed.
        /// </summary>
        /// <remarks>
        /// The list views are refreshed, not rebuilt, so they keep their scroll position.
        /// </remarks>
        public void Refresh()
        {
            if (_pipeline == null)
                return;

            if (_pipeline.Update())
            {
                _changeTracker.UpdateIndex();
                _rows.Clear();
                _rows.AddRange(_pipeline.VisibleRows);
                _listView.RefreshItems();
                _frozenListView?.RefreshItems();
            }

            bool hasAssets = _pipeline.Rows.Count > 0;
            _body.SetDisplayed(hasAssets);
            _messageLabel.SetDisplayed(!hasAssets);
            _statusLabel.text = _rows.Count == 1 ? "1 row" : $"{_rows.Count} rows";
        }

        #endregion


        #region UI

        /// <summary>
        /// Creates the list views with the current columns, replacing the previous ones: the frozen list with the first column, and the
        /// main list with the other columns.
        /// </summary>
        private void RebuildListViews()
        {
            _body.Clear();
            _frozenListView = null;
            _frozenColumnLink = null;

            // The first column is frozen only if there are other columns to scroll beside it
            bool hasFrozenColumn = _columns.Count > 1;
            if (hasFrozenColumn)
            {
                _frozenListView = CreateListView();
                _frozenListView.AddToClassList(FrozenListClass);
                // The width of the frozen list follows its column, so the column never grows to fill the list
                _frozenListView.columns.stretchMode = Columns.StretchMode.Grow;
                _body.Add(_frozenListView);
            }

            _listView = CreateListView();
            _listView.AddToClassList(ListClass);
            _listView.columns.stretchMode = Columns.StretchMode.GrowAndFill;
            _body.Add(_listView);

            int firstMainColumn = 0;
            if (hasFrozenColumn)
            {
                // A flexible column has no width of its own, and can't share the width of the others in the frozen list
                TableViewColumn frozenColumn = _columns[0];
                float frozenColumnWidth = frozenColumn.IsFlexible ? FlexibleColumnWidth : frozenColumn.Width;

                _frozenColumnLink = new FrozenColumnLink(_frozenListView, _listView, frozenColumnWidth);
                _frozenListView.columns.Add(CreateListColumn(frozenColumn, 0, frozenColumnWidth, false, _frozenColumnLink));
                firstMainColumn = 1;
            }

            for (int i = firstMainColumn; i < _columns.Count; i++)
            {
                TableViewColumn column = _columns[i];
                float width = column.IsFlexible ? FlexibleColumnWidth : column.Width;
                _listView.columns.Add(CreateListColumn(column, i, width, column.IsFlexible, null));
            }
        }

        /// <summary>
        /// Creates a list view that displays the rows, without columns.
        /// </summary>
        /// <returns>Returns the created list view.</returns>
        private MultiColumnListView CreateListView()
        {
            return new MultiColumnListView
            {
                itemsSource = _rows,
                fixedItemHeight = RowHeight,
                selectionType = SelectionType.None,
                sortingMode = ColumnSortingMode.None,
                horizontalScrollingEnabled = true,
                showAlternatingRowBackgrounds = AlternatingRowBackground.ContentOnly,
            };
        }

        /// <summary>
        /// Creates the column of a list view that displays a column of the table.
        /// </summary>
        /// <param name="column">The column of the table.</param>
        /// <param name="columnIndex">The index of the column in the pipeline.</param>
        /// <param name="width">The initial width of the column, in pixels.</param>
        /// <param name="isStretchable">If enabled, the column grows to fill the width left by the other columns.</param>
        /// <param name="frozenColumnLink">The link that tracks the cells of the frozen column, or null if the column is not frozen.</param>
        /// <returns>Returns the created column.</returns>
        private Column CreateListColumn(TableViewColumn column, int columnIndex, float width, bool isStretchable,
            FrozenColumnLink frozenColumnLink)
        {
            return new Column
            {
                name = column.Id,
                title = column.Name,
                width = width,
                minWidth = MinColumnWidth,
                stretchable = isStretchable,
                resizable = true,
                sortable = false,
                makeHeader = () => MakeHeader(column),
                makeCell = () =>
                {
                    VisualElement cell = MakeCell(column);
                    frozenColumnLink?.TrackCell(cell);
                    return cell;
                },
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
        /// Refreshes the view on its next update. Several calls before that update only cause one refresh.
        /// </summary>
        /// <remarks>
        /// Changes are reported during the postprocess of the imports, where loading assets is fragile, so the refresh is deferred. The
        /// scheduler of the view only runs while it's attached to a panel.
        /// </remarks>
        private void ScheduleRefresh()
        {
            if (_isRefreshScheduled)
                return;

            _isRefreshScheduled = true;
            schedule.Execute(() =>
            {
                _isRefreshScheduled = false;
                Refresh();
            });
        }

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
