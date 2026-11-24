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
    /// <para>
    /// The order of the columns, their widths and the sort are remembered for each table (see <see cref="ViewSettings"/>).
    /// </para>
    /// </remarks>
    internal partial class TableView : VisualElement
    {

        #region Subclasses

        /// <summary>
        /// What a cell element displays: its column, and its current row. Stored in the <see cref="VisualElement.userData"/> of the
        /// element, since elements are reused for other rows when the table scrolls.
        /// </summary>
        private class CellBinding
        {

            /// <summary>
            /// The column of the cell, which never changes for an element.
            /// </summary>
            public readonly TableViewColumn Column;

            /// <summary>
            /// Checks if the element is a cell of the frozen list.
            /// </summary>
            public readonly bool IsFrozen;

            /// <summary>
            /// The row currently displayed by the element, or null if the element is not bound.
            /// </summary>
            public TableRow Row = null;

            /// <inheritdoc cref="CellBinding"/>
            /// <param name="column"><inheritdoc cref="Column" path="/summary"/></param>
            /// <param name="isFrozen"><inheritdoc cref="IsFrozen" path="/summary"/></param>
            public CellBinding(TableViewColumn column, bool isFrozen)
            {
                Column = column;
                IsFrozen = isFrozen;
            }

        }

        #endregion


        #region Fields

        private const string ViewClass = "database-table-view";
        private const string BodyClass = "database-table-view__body";
        private const string ListClass = "database-table-view__list";
        private const string FrozenListClass = "database-table-view__frozen-list";
        private const string HeaderClass = "database-table-view__header";
        private const string CellClass = "database-table-view__cell";
        private const string ActiveCellClass = "database-table-view__cell--active";
        private const string HoverMirrorRowClass = "database-table-view__row--hover-mirror";
        private const string StatusBarClass = "database-status-bar";
        private const string StatusLabelClass = "database-status-bar__label";
        private const string MessageClass = "database-message";
        private const string EmptyClass = "database-table-view__empty";

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
        /// Checks if the view is changing the sort descriptions of the headers itself, so it ignores the events this raises.
        /// </summary>
        private bool _isUpdatingSort = false;

        /// <summary>
        /// The active cell, or <see cref="CellId.None"/> if there is none.
        /// </summary>
        private CellId _activeCell = CellId.None;

        /// <summary>
        /// The element that currently displays the active cell, or null if the active cell is not visible.
        /// </summary>
        /// <remarks>
        /// Cell elements are reused for other rows when the table scrolls, so the element is updated each time a cell is bound.
        /// </remarks>
        private VisualElement _activeCellElement = null;

        /// <summary>
        /// The row under the pointer, or null if the pointer is not over a cell.
        /// </summary>
        private TableRow _hoveredRow = null;

        /// <summary>
        /// Checks if the pointer is over the frozen list, instead of the main list.
        /// </summary>
        private bool _isHoverInFrozenList = false;

        /// <summary>
        /// The message shown instead of the rows when the table has no asset, with the button that creates one.
        /// </summary>
        private VisualElement _emptyMessage = null;

        /// <summary>
        /// The button of <see cref="_emptyMessage"/> that creates an asset, hidden if the table has no type that can be created.
        /// </summary>
        private Button _emptyCreateButton = null;

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

            _emptyMessage = new VisualElement();
            _emptyMessage.AddToClassList(EmptyClass);
            _emptyMessage.SetDisplayed(false);
            Label messageLabel = new Label(NoAssetMessage);
            messageLabel.AddToClassList(MessageClass);
            _emptyMessage.Add(messageLabel);
            _emptyCreateButton = CreateEmptyCreateButton();
            _emptyMessage.Add(_emptyCreateButton);
            Add(_emptyMessage);

            VisualElement statusBar = new VisualElement();
            statusBar.AddToClassList(StatusBarClass);
            _statusLabel = new Label();
            _statusLabel.AddToClassList(StatusLabelClass);
            statusBar.Add(_statusLabel);
            Add(statusBar);

            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);

            // The view gets the focus when a cell is clicked, so it receives the keys that start an edit
            focusable = true;
            RegisterCallback<KeyDownEvent>(OnKeyDown, TrickleDown.TrickleDown);
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
            EndEdit(true, false);
            SaveColumnOrder();
            _changeTracker?.Stop();
        }

        #endregion


        #region Public API

        /// <inheritdoc cref="_table"/>
        public TableDefinition Table => _table;

        /// <summary>
        /// Displays a table: loads its assets, and builds its columns and its pipeline, with the remembered order, widths, hidden
        /// columns and sort of the table.
        /// </summary>
        /// <param name="table">The table to display.</param>
        /// <param name="search">The search that filters the rows (see <see cref="SetSearch(string)"/>), or null to show all the rows.
        /// </param>
        public void SetTable(TableDefinition table, string search = null)
        {
            // Leaving a table confirms its edit, like clicking outside of the edited cell
            EndEdit(true, false);
            SaveColumnOrder();
            _changeTracker?.Stop();
            _table = table;
            _activeCell = CellId.None;
            _activeCellElement = null;
            _creatableTypes = AssetOperations.GetCreatableTypes(table.Type);
            UpdateCreateButton(_emptyCreateButton, CreateAssetText);

            ProjectAssetSource source = new ProjectAssetSource(table.Type);
            IReadOnlyList<Object> assets = source.LoadAssets();
            _columns = CreateColumns(table, assets.Count > 0 ? assets[0] : null);
            _viewSettings = UserViews.instance.GetDefaultView(table.Id);

            IValueAccessor[] accessors = new IValueAccessor[_columns.Count];
            for (int i = 0; i < _columns.Count; i++)
                accessors[i] = _columns[i].Accessor;

            _pipeline = new TablePipeline(new PreloadedAssetSource(assets, source), accessors);
            // Before the first update, so the rows are filtered and sorted once, on the displayed columns
            UpdateDisplayedColumns();
            StoreSearch(search);
            ApplySearchFilter();
            ApplyRememberedSort();

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
            EndEdit(true, false);
            SaveColumnOrder();
            _changeTracker?.Stop();
            _changeTracker = null;
            _table = null;
            _viewSettings = null;
            _pipeline = null;
            _columns = new List<TableViewColumn>();
            _displayedColumns = new List<int>();
            _isColumnDisplayed = Array.Empty<bool>();
            _rows.Clear();
            _activeCell = CellId.None;
            _activeCellElement = null;
            _hoveredRow = null;
            _creatableTypes = new List<Type>();

            _body.Clear();
            _listView = null;
            _frozenListView = null;
            _frozenColumnLink = null;
        }

        /// <inheritdoc cref="_activeCell"/>
        public CellId ActiveCell => _activeCell;

        /// <summary>
        /// Sets the active cell, and highlights it.
        /// </summary>
        /// <param name="cell">The cell to activate, or <see cref="CellId.None"/> to have no active cell.</param>
        public void SetActiveCell(CellId cell)
        {
            if (cell.Equals(_activeCell))
                return;

            _activeCell = cell;
            _activeCellElement?.RemoveFromClassList(ActiveCellClass);
            _activeCellElement = null;

            // Find the element of the new active cell among the visible cells. Elements that are not visible get the highlight when
            // they are bound
            if (!cell.IsNone)
                _body.Query(className: CellClass).ForEach(UpdateActiveCellHighlight);
        }

        /// <summary>
        /// Shows the row of an asset: clears the search if it hides the row, makes the asset the active cell (see
        /// <see cref="GetShownAssetColumnId"/>), scrolls to its row, and focuses the view.
        /// </summary>
        /// <remarks>
        /// The asset must already be in the table: call <see cref="Reload"/> first if it has just been created. Works on a table that
        /// has just been set, whose list views are not laid out yet.
        /// </remarks>
        /// <param name="asset">The asset to show.</param>
        /// <returns>Returns true if the asset has a row in the table, false if it doesn't (or if no table is displayed).</returns>
        public bool ShowAsset(Object asset)
        {
            if (asset == null || _pipeline == null || !_pipeline.TryGetRow(asset, out _))
                return false;

            // The user just asked for that asset: it must not stay hidden by the search
            ResetSearchIfHidden(asset);

            SetActiveCell(new CellId(asset, GetShownAssetColumnId()));
            ScrollToAsset(asset);
            Focus();
            return true;
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

                // The asset of the active cell may have been deleted, may not be in the table anymore, or may be hidden by the search
                if (!_activeCell.IsNone && !IsAssetVisible(_activeCell.Asset))
                {
                    // A deleted asset has nothing to write
                    EndEdit(_activeCell.Asset != null, false);
                    SetActiveCell(CellId.None);
                }

                _listView.RefreshItems();
                _frozenListView?.RefreshItems();
            }

            bool hasAssets = _pipeline.Rows.Count > 0;
            _body.SetDisplayed(hasAssets);
            _emptyMessage.SetDisplayed(!hasAssets);
            _statusLabel.text = GetStatusText();
        }

        #endregion


        #region UI

        /// <summary>
        /// Creates the list views with the displayed columns (see <see cref="UpdateDisplayedColumns"/>), replacing the previous ones:
        /// the frozen list with the first column, and the main list with the other columns. The sort of the pipeline is shown in the
        /// headers.
        /// </summary>
        /// <param name="shouldKeepScroll">If enabled, the new main list is scrolled like the previous one, for example when a column
        /// is hidden. Otherwise, it starts at the top.</param>
        private void RebuildListViews(bool shouldKeepScroll = false)
        {
            Vector2 scrollOffset = shouldKeepScroll && _listView != null ? _listView.Q<ScrollView>().scrollOffset : Vector2.zero;
            _body.Clear();
            _frozenListView = null;
            _frozenColumnLink = null;

            List<int> displayedColumns = _displayedColumns;
            // The first column is frozen only if there are other columns to scroll beside it
            bool hasFrozenColumn = displayedColumns.Count > 1;
            if (hasFrozenColumn)
            {
                _frozenListView = CreateListView();
                _frozenListView.AddToClassList(FrozenListClass);
                // The width of the frozen list follows its column, so the column never grows to fill the list
                _frozenListView.columns.stretchMode = Columns.StretchMode.Grow;
                // The message of the main list is enough, the narrow frozen pane would cut it
                _frozenListView.makeNoneElement = () => new VisualElement();
                _body.Add(_frozenListView);
            }

            _listView = CreateListView();
            _listView.AddToClassList(ListClass);
            _listView.columns.stretchMode = Columns.StretchMode.GrowAndFill;
            _listView.makeNoneElement = MakeNoMatchElement;
            // Dragging a header to reorder the columns ends with a release
            _listView.RegisterCallback<PointerUpEvent>(OnMainListPointerUp, TrickleDown.TrickleDown);
            _body.Add(_listView);

            // Scrolling moves the rows under the pointer: the mirrored hover is shown again when the pointer enters another cell
            _hoveredRow = null;
            _listView.Q<ScrollView>().verticalScroller.valueChanged += value => SetHoveredRow(null, false);

            int firstMainColumn = 0;
            if (hasFrozenColumn)
            {
                // A flexible column has no width of its own, and can't share the width of the others in the frozen list
                int frozenColumnIndex = displayedColumns[0];
                TableViewColumn frozenColumn = _columns[frozenColumnIndex];
                float frozenColumnWidth = GetInitialWidth(frozenColumn);

                _frozenColumnLink = new FrozenColumnLink(_frozenListView, _listView, frozenColumnWidth);
                _frozenListView.columns.Add(CreateListColumn(frozenColumn, frozenColumnIndex, frozenColumnWidth, false, _frozenColumnLink));
                firstMainColumn = 1;
            }

            for (int i = firstMainColumn; i < displayedColumns.Count; i++)
            {
                int columnIndex = displayedColumns[i];
                TableViewColumn column = _columns[columnIndex];
                _listView.columns.Add(CreateListColumn(column, columnIndex, GetInitialWidth(column), column.IsFlexible, null));
            }

            ShowSortInHeader();
            if (scrollOffset != Vector2.zero)
                RestoreScrollOffset(_listView, scrollOffset);
        }

        /// <summary>
        /// Scrolls a new list view to an offset, once it has laid out its content.
        /// </summary>
        /// <remarks>
        /// The offset is clamped by the scroll view if the content is smaller, for example when a hidden column makes the table
        /// narrower. Only the main list is scrolled: the frozen list follows it (see <see cref="FrozenColumnLink"/>).
        /// </remarks>
        /// <param name="listView">The new list view.</param>
        /// <param name="scrollOffset">The offset to restore.</param>
        private static void RestoreScrollOffset(MultiColumnListView listView, Vector2 scrollOffset)
        {
            ScrollView scrollView = listView.Q<ScrollView>();
            // The content has no size before its first layout, so an offset set earlier would be clamped to the top
            EventCallback<GeometryChangedEvent> callback = null;
            callback = evt =>
            {
                scrollView.contentContainer.UnregisterCallback(callback);
                scrollView.scrollOffset = scrollOffset;
            };
            scrollView.contentContainer.RegisterCallback(callback);
        }

        /// <summary>
        /// Creates a list view that displays the rows, without columns.
        /// </summary>
        /// <returns>Returns the created list view.</returns>
        private MultiColumnListView CreateListView()
        {
            MultiColumnListView listView = new MultiColumnListView
            {
                itemsSource = _rows,
                fixedItemHeight = RowHeight,
                selectionType = SelectionType.None,
                // The header only reports the sort: the pipeline sorts the rows
                sortingMode = ColumnSortingMode.Custom,
                horizontalScrollingEnabled = true,
                showAlternatingRowBackgrounds = AlternatingRowBackground.ContentOnly,
            };
            listView.columnSortingChanged += () => OnColumnSortingChanged(listView);
            listView.headerContextMenuPopulateEvent += OnHeaderContextMenuPopulate;
            return listView;
        }

        /// <summary>
        /// Creates the column of a list view that displays a column of the table. Its width is remembered when the user resizes it.
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
            Column listColumn = new Column
            {
                name = column.Id,
                title = column.Name,
                width = width,
                minWidth = MinColumnWidth,
                stretchable = isStretchable,
                resizable = true,
                sortable = column.IsSortable,
                makeHeader = () => MakeHeader(column),
                makeCell = () =>
                {
                    VisualElement cell = MakeCell(column, frozenColumnLink != null);
                    frozenColumnLink?.TrackCell(cell);
                    return cell;
                },
                bindCell = (element, rowIndex) => BindCell(element, column, columnIndex, _rows[rowIndex]),
                unbindCell = (element, rowIndex) => UnbindCell(element),
            };

            // After the initial width is set, so only the widths chosen by the user are reported
            TrackColumnWidth(column, listColumn);
            return listColumn;
        }

        /// <summary>
        /// Creates the header of a column, with its tooltip.
        /// </summary>
        /// <remarks>
        /// The header holds its column, so the order of the columns can be read from the headers (see <see cref="SaveColumnOrder"/>).
        /// </remarks>
        /// <param name="column">The column of which to create the header.</param>
        /// <returns>Returns the created header.</returns>
        private static VisualElement MakeHeader(TableViewColumn column)
        {
            Label header = new Label(column.Name) { tooltip = column.Tooltip, userData = column };
            header.AddToClassList(HeaderClass);
            return header;
        }

        /// <summary>
        /// Creates the element of a cell: a container, which holds the display created by the handler of the column, or the buttons of
        /// the <i>Controls</i> column.
        /// </summary>
        /// <remarks>
        /// The container knows its column and its current row (see <see cref="CellBinding"/>), and activates its cell when it's
        /// clicked. The display is a child of the container, so a live control that handles the click itself can stop it before it
        /// reaches the container.
        /// </remarks>
        /// <param name="column">The column of the cell.</param>
        /// <param name="isFrozen">Checks if the cell is in the frozen list.</param>
        /// <returns>Returns the created element.</returns>
        private VisualElement MakeCell(TableViewColumn column, bool isFrozen)
        {
            VisualElement cell = new VisualElement { userData = new CellBinding(column, isFrozen) };
            cell.AddToClassList(CellClass);
            cell.Add(column.IsControls ? CreateControls(cell) : column.Handler.CreateDisplay());
            cell.RegisterCallback<PointerDownEvent>(OnCellPointerDown);
            cell.RegisterCallback<PointerEnterEvent>(OnCellPointerEnter);
            cell.RegisterCallback<PointerLeaveEvent>(OnCellPointerLeave);
            cell.RegisterCallback<CellValueSubmitEvent>(OnCellValueSubmitted);
            return cell;
        }

        /// <summary>
        /// Shows the cached value of a row in a cell, through the handler of the column, and highlights the cell if it's active.
        /// </summary>
        /// <remarks>
        /// An error of the handler is logged once per column, and doesn't prevent the other cells from being displayed.
        /// </remarks>
        /// <param name="cell">The element created by <see cref="MakeCell(TableViewColumn, bool)"/>.</param>
        /// <param name="column">The column of the cell.</param>
        /// <param name="columnIndex">The index of the column in the pipeline.</param>
        /// <param name="row">The row of the cell.</param>
        private void BindCell(VisualElement cell, TableViewColumn column, int columnIndex, TableRow row)
        {
            ((CellBinding)cell.userData).Row = row;
            UpdateActiveCellHighlight(cell);
            UpdateHoverMirror(cell);
            EndEditIfRebound(cell, row);

            // The buttons of the controls read the row of their cell when they are clicked: there is no value to display
            if (column.IsControls)
                return;

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

        /// <summary>
        /// Detaches a cell from its row, when the list view releases its element.
        /// </summary>
        /// <param name="cell">The element created by <see cref="MakeCell(TableViewColumn, bool)"/>.</param>
        private void UnbindCell(VisualElement cell)
        {
            // A released element may stay in the hierarchy, hidden, so it must not keep the highlight of its last row
            ((CellBinding)cell.userData).Row = null;
            UpdateActiveCellHighlight(cell);
            UpdateHoverMirror(cell);
            EndEditIfRebound(cell, null);
        }

        #endregion


        #region Hover

        /// <summary>
        /// Called when the pointer enters a cell. Shows the hover of its row in the other list.
        /// </summary>
        /// <param name="evt">The emitted event.</param>
        private void OnCellPointerEnter(PointerEnterEvent evt)
        {
            if (evt.currentTarget is VisualElement cell && cell.userData is CellBinding binding)
                SetHoveredRow(binding.Row, binding.IsFrozen);
        }

        /// <summary>
        /// Called when the pointer leaves a cell. Removes the hover of its row from the other list, until the pointer enters another
        /// cell.
        /// </summary>
        /// <param name="evt">The emitted event.</param>
        private void OnCellPointerLeave(PointerLeaveEvent evt)
        {
            SetHoveredRow(null, false);
        }

        /// <summary>
        /// Sets the row under the pointer, and updates the hover shown in the other list.
        /// </summary>
        /// <remarks>
        /// The frozen list and the main list are separate list views, so the hover style of Unity only shows on the row of the list under
        /// the pointer. The row of the other list gets the same style through a class.
        /// </remarks>
        /// <param name="row">The row under the pointer, or null if the pointer is not over a cell.</param>
        /// <param name="isInFrozenList">Checks if the pointer is over the frozen list.</param>
        private void SetHoveredRow(TableRow row, bool isInFrozenList)
        {
            if (row == _hoveredRow && isInFrozenList == _isHoverInFrozenList)
                return;

            _hoveredRow = row;
            _isHoverInFrozenList = isInFrozenList;
            _body.Query(className: CellClass).ForEach(UpdateHoverMirror);
        }

        /// <summary>
        /// Shows the hover style on the row of a cell if the pointer is over the same row in the other list, and removes it otherwise.
        /// </summary>
        /// <param name="cell">The element created by <see cref="MakeCell(TableViewColumn, bool)"/>.</param>
        private void UpdateHoverMirror(VisualElement cell)
        {
            if (cell.userData is not CellBinding binding)
                return;

            VisualElement rowElement = GetRowElement(cell);
            if (rowElement == null)
                return;

            bool isMirrored = binding.Row != null && binding.Row == _hoveredRow && binding.IsFrozen != _isHoverInFrozenList;
            rowElement.EnableInClassList(HoverMirrorRowClass, isMirrored);
        }

        /// <summary>
        /// Gets the element of the list view that holds the row of a cell, which has the hover style of Unity.
        /// </summary>
        /// <param name="cell">The element created by <see cref="MakeCell(TableViewColumn, bool)"/>.</param>
        /// <returns>Returns the found element, or null if the cell is not in a list view.</returns>
        private static VisualElement GetRowElement(VisualElement cell)
        {
            VisualElement element = cell.parent;
            while (element != null && !element.ClassListContains(BaseVerticalCollectionView.itemUssClassName))
                element = element.parent;

            return element;
        }

        #endregion


        #region Active cell

        /// <summary>
        /// Called when a cell is clicked. Activates the cell, and edits it on a double-click.
        /// </summary>
        /// <param name="evt">The emitted event.</param>
        private void OnCellPointerDown(PointerDownEvent evt)
        {
            if (evt.button != (int)MouseButton.LeftMouse)
                return;

            if (evt.currentTarget is not VisualElement cell || cell.userData is not CellBinding binding || binding.Row == null)
                return;

            // Clicks inside the edit field are handled by the field
            if (_edit != null && _edit.CellElement == cell)
                return;

            // The view handles the focus itself. Otherwise, the focus controller gives it to the list view after this event, which
            // would end the edit started by a double-click
            cell.panel?.focusController?.IgnoreEvent(evt);

            // Read before the focus changes: ending an edit in progress refreshes the rows, and can rebind this element to another row
            CellId clickedCell = new CellId(binding.Row.Asset, binding.Column.Id);
            // Ended explicitly, since the field may not have the focus (for example a custom edit field without focusable element)
            EndEdit(true, false);
            Focus();
            SetActiveCell(clickedCell);

            if (evt.clickCount == 2 && _activeCellElement != null)
                BeginEdit(_activeCellElement, '\0');
        }

        /// <summary>
        /// Highlights a cell element if it displays the active cell, and removes its highlight otherwise.
        /// </summary>
        /// <param name="cell">The element created by <see cref="MakeCell(TableViewColumn, bool)"/>.</param>
        private void UpdateActiveCellHighlight(VisualElement cell)
        {
            if (cell.userData is not CellBinding binding)
                return;

            bool isActive = binding.Row != null && _activeCell.Is(binding.Row.Asset, binding.Column.Id);
            cell.EnableInClassList(ActiveCellClass, isActive);
            if (isActive)
                _activeCellElement = cell;
            else if (_activeCellElement == cell)
                _activeCellElement = null;
        }

        /// <summary>
        /// Gets the column of the active cell of an asset shown with <see cref="ShowAsset(Object)"/>: the <i>Asset Name</i> column if
        /// it's displayed, so the asset can be renamed right away, then the <i>Controls</i> column, then the first displayed column.
        /// </summary>
        /// <returns>Returns the identifier of the column.</returns>
        private string GetShownAssetColumnId()
        {
            if (IsColumnDisplayed(TableViewColumn.AssetNameId))
                return TableViewColumn.AssetNameId;

            if (IsColumnDisplayed(TableViewColumn.ControlsId))
                return TableViewColumn.ControlsId;

            return _displayedColumns.Count > 0 ? _columns[_displayedColumns[0]].Id : TableViewColumn.AssetNameId;
        }

        /// <summary>
        /// Scrolls the table so the row of an asset is visible. Does nothing if the asset has no visible row.
        /// </summary>
        /// <remarks>
        /// Only the main list is scrolled: the frozen list follows it (see <see cref="FrozenColumnLink"/>).
        /// </remarks>
        /// <param name="asset">The asset of the row to show.</param>
        private void ScrollToAsset(Object asset)
        {
            if (_pipeline == null || _listView == null || !_pipeline.TryGetRow(asset, out TableRow row))
                return;

            int index = _rows.IndexOf(row);
            if (index < 0)
                return;

            MultiColumnListView listView = _listView;
            Action scrollToRow = () =>
            {
                // The list may have been rebuilt, or the rows may have changed in the meantime
                if (_listView == listView && index < _rows.Count && _rows[index] == row)
                    _listView.ScrollToItem(index);
            };

            // A list view that has just been built has no size before its first layout, so it can't find the offset of the row: wait
            // for its layout, like when its scroll position is restored
            VisualElement content = listView.Q<ScrollView>().contentContainer;
            float contentHeight = content.layout.height;
            if (float.IsNaN(contentHeight) || contentHeight <= 0)
            {
                EventCallback<GeometryChangedEvent> callback = null;
                callback = evt =>
                {
                    content.UnregisterCallback(callback);
                    scrollToRow();
                };
                content.RegisterCallback(callback);
            }
            else
            {
                // Deferred, since the list view hasn't laid out its refreshed rows yet
                schedule.Execute(scrollToRow);
            }
        }

        #endregion


        #region Sorting

        /// <summary>
        /// Called when the user clicks the header of a sortable column, in the frozen list or in the main list. Sorts the rows by that
        /// column.
        /// </summary>
        /// <remarks>
        /// The table is sorted by a single column until multi-column sorting (Next milestone). The headers can describe several sorted
        /// columns (Shift-click), so only the last sorted column is kept, and the header of the other list is cleared.
        /// </remarks>
        /// <param name="listView">The list view whose header has been clicked.</param>
        private void OnColumnSortingChanged(MultiColumnListView listView)
        {
            // Changing the sort descriptions below raises this event again
            if (_isUpdatingSort || _pipeline == null)
                return;

            _isUpdatingSort = true;
            try
            {
                SortColumnDescriptions descriptions = listView.sortColumnDescriptions;
                SortColumnDescription description = descriptions.Count > 0 ? descriptions[descriptions.Count - 1] : null;
                if (descriptions.Count > 1)
                {
                    descriptions.Clear();
                    descriptions.Add(description);
                }

                MultiColumnListView otherListView = listView == _listView ? _frozenListView : _listView;
                if (description != null && otherListView != null && otherListView.sortColumnDescriptions.Count > 0)
                    otherListView.sortColumnDescriptions.Clear();

                int columnIndex = description != null ? GetColumnIndex(listView, description) : -1;
                if (columnIndex >= 0)
                    SetPipelineSort(columnIndex, description.direction == SortDirection.Descending);
                else
                    _pipeline.ClearSort();
            }
            finally
            {
                _isUpdatingSort = false;
            }

            SaveSort();
            Refresh();
        }

        /// <summary>
        /// Sorts the rows of the pipeline by a column, through its handler.
        /// </summary>
        /// <param name="columnIndex">The index of the column in the pipeline.</param>
        /// <param name="isDescending">If enabled, the rows are sorted in descending order.</param>
        private void SetPipelineSort(int columnIndex, bool isDescending)
        {
            TableViewColumn column = _columns[columnIndex];
            _pipeline.SetSort(columnIndex, isDescending, (a, b) => Compare(column, a, b));
        }

        /// <summary>
        /// Gets the index of the column of the table described by a sort description of a list view.
        /// </summary>
        /// <param name="listView">The list view that owns the description.</param>
        /// <param name="description">The sort description.</param>
        /// <returns>Returns the index of the column in the pipeline, or -1 if it's not found.</returns>
        private int GetColumnIndex(MultiColumnListView listView, SortColumnDescription description)
        {
            // The columns of the list views are named after the identifiers of the columns of the table
            string columnId = description.column?.name;
            if (string.IsNullOrEmpty(columnId))
                columnId = description.columnName;

            if (string.IsNullOrEmpty(columnId) && description.columnIndex >= 0 && description.columnIndex < listView.columns.Count)
                columnId = listView.columns[description.columnIndex].name;

            return _columns.FindIndex(column => column.Id == columnId);
        }

        /// <summary>
        /// Compares two values of a column through its handler.
        /// </summary>
        /// <remarks>
        /// An error of the handler is logged once per column, and the values are then considered equal, so a broken custom handler
        /// doesn't prevent the table from being displayed.
        /// </remarks>
        /// <param name="column">The column of the values.</param>
        /// <param name="a">The first value.</param>
        /// <param name="b">The second value.</param>
        /// <returns>Returns the result of the handler, or 0 if it fails.</returns>
        private static int Compare(TableViewColumn column, object a, object b)
        {
            try
            {
                return column.Handler.Compare(a, b);
            }
            catch (Exception exception)
            {
                if (!column.HasLoggedError)
                {
                    column.HasLoggedError = true;
                    Debug.LogError($"{DatabaseConstants.LogPrefix}The cell handler {column.Handler.GetType()} failed to compare " +
                        $"values of the column \"{column.Name}\". The next errors of this column are not logged.");
                    Debug.LogException(exception);
                }
                return 0;
            }
        }

        #endregion


        #region Private API

        /// <summary>
        /// Gets the text of the status line: the number of rows, and the number of visible rows when the search hides some.
        /// </summary>
        /// <returns>Returns the text of the status line.</returns>
        private string GetStatusText()
        {
            int rowCount = _pipeline.Rows.Count;
            string rowsText = rowCount == 1 ? "row" : "rows";
            return _rows.Count == rowCount
                ? $"{rowCount} {rowsText}"
                : $"{_rows.Count} of {rowCount} {rowsText}";
        }

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
                // Fields bound to their property write it while the user edits, which reports changes: the rows must not be sorted
                // again under the edited cell. The invalidations are applied when the edit ends, since it refreshes the view
                if (_edit == null)
                    Refresh();
            });
        }

        /// <summary>
        /// Creates the columns of a table: the <i>Asset Name</i> column, the columns of the definition that are valid, then the
        /// <i>Controls</i> column.
        /// </summary>
        /// <remarks>
        /// The <i>Asset Name</i> column is created even if the table hides it (<see cref="TableDefinition.HideAssetName"/>): it's only
        /// hidden by default, so the user can show it (see <see cref="IsHiddenByDefault(string)"/>).
        /// </remarks>
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

            List<TableViewColumn> columns = new List<TableViewColumn>(definitionColumns.Count + 2);
            columns.Add(TableViewColumn.CreateAssetNameColumn());
            foreach (ColumnDefinition column in definitionColumns)
                columns.Add(TableViewColumn.FromDefinition(column));

            // Last, since the first column is frozen and must show what identifies the rows
            columns.Add(TableViewColumn.CreateControlsColumn());
            return columns;
        }

        #endregion

    }

}
