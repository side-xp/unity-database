using UnityEngine;
using UnityEngine.UIElements;

namespace SideXP.Database.EditorOnly
{

    /// <summary>
    /// Keeps the frozen pane of a table view aligned with its main list. The frozen pane is a list view that only displays the first
    /// column, placed on the left of the main list, which displays the other columns.
    /// </summary>
    /// <remarks>
    /// The main list scrolls in both directions, and the frozen pane follows its vertical scroll. The frozen pane has no scrollbar: the
    /// mouse wheel over it scrolls the main list. Its width follows the width of its column when the column is resized, and its bottom
    /// is raised by the height of the horizontal scrollbar of the main list, so the rows of both lists stay aligned.
    /// </remarks>
    internal class FrozenColumnLink
    {

        #region Fields

        /// <summary>
        /// The list that displays the frozen column.
        /// </summary>
        private MultiColumnListView _frozenList = null;

        /// <summary>
        /// The scroll view of the frozen list.
        /// </summary>
        private ScrollView _frozenScrollView = null;

        /// <summary>
        /// The scroll view of the main list, which displays the other columns.
        /// </summary>
        private ScrollView _mainScrollView = null;

        /// <summary>
        /// The last width applied to the frozen list, borders included, in pixels.
        /// </summary>
        private float _paneWidth = -1;

        #endregion


        #region Lifecycle

        /// <summary>
        /// Links a frozen list to a main list.
        /// </summary>
        /// <param name="frozenList">The list that displays the frozen column.</param>
        /// <param name="mainList">The list that displays the other columns.</param>
        /// <param name="columnWidth">The initial width of the frozen column, in pixels.</param>
        public FrozenColumnLink(MultiColumnListView frozenList, MultiColumnListView mainList, float columnWidth)
        {
            _frozenList = frozenList;
            _frozenScrollView = frozenList.Q<ScrollView>();
            _mainScrollView = mainList.Q<ScrollView>();

            // The frozen list never scrolls on its own: it follows the main list
            _frozenScrollView.verticalScrollerVisibility = ScrollerVisibility.Hidden;
            _frozenScrollView.horizontalScrollerVisibility = ScrollerVisibility.Hidden;

            _mainScrollView.verticalScroller.valueChanged += OnMainScrolled;
            _mainScrollView.contentViewport.RegisterCallback<GeometryChangedEvent>(OnViewportGeometryChanged);
            _frozenScrollView.contentViewport.RegisterCallback<GeometryChangedEvent>(OnViewportGeometryChanged);
            _frozenList.RegisterCallback<WheelEvent>(OnFrozenListWheel, TrickleDown.TrickleDown);

            SetColumnWidth(columnWidth);
        }

        #endregion


        #region Public API

        /// <summary>
        /// Tracks a cell of the frozen column, so the width of the frozen list follows the width of the column when it's resized.
        /// </summary>
        /// <remarks>
        /// The header of a column is resized by the list view, which resizes its cells. Cells are tracked instead of the header, since
        /// the element that holds a cell is known to have the width of its column, while the layout of the header is internal.
        /// </remarks>
        /// <param name="cell">The element created for a cell of the frozen column.</param>
        public void TrackCell(VisualElement cell)
        {
            cell.RegisterCallback<GeometryChangedEvent>(OnCellGeometryChanged);
        }

        #endregion


        #region Private API

        /// <summary>
        /// Sets the width of the frozen list so it shows its whole column.
        /// </summary>
        /// <param name="columnWidth">The width of the frozen column, in pixels.</param>
        private void SetColumnWidth(float columnWidth)
        {
            // Unity's layout includes the borders in the width
            float paneWidth = columnWidth + _frozenList.resolvedStyle.borderLeftWidth + _frozenList.resolvedStyle.borderRightWidth;
            if (Mathf.Approximately(paneWidth, _paneWidth))
                return;

            _paneWidth = paneWidth;
            _frozenList.style.width = paneWidth;
        }

        /// <summary>
        /// Applies the vertical scroll of the main list to the frozen list.
        /// </summary>
        private void SyncScroll()
        {
            Vector2 offset = new Vector2(0, _mainScrollView.scrollOffset.y);
            if (_frozenScrollView.scrollOffset != offset)
                _frozenScrollView.scrollOffset = offset;
        }

        /// <summary>
        /// Called when the main list is scrolled vertically.
        /// </summary>
        /// <param name="value">The new vertical offset of the main list.</param>
        private void OnMainScrolled(float value)
        {
            SyncScroll();
        }

        /// <summary>
        /// Called when the viewport of a list changes size, for example when the horizontal scrollbar of the main list appears.
        /// </summary>
        /// <param name="evt">The emitted event.</param>
        private void OnViewportGeometryChanged(GeometryChangedEvent evt)
        {
            // The space under the viewport of the main list is taken by its horizontal scrollbar, when it's displayed. Measured instead
            // of read from the scrollbar, so it doesn't depend on how the scroll view hides it
            float scrollbarHeight = Mathf.Max(0, _mainScrollView.worldBound.yMax - _mainScrollView.contentViewport.worldBound.yMax);
            if (!Mathf.Approximately(_frozenList.resolvedStyle.marginBottom, scrollbarHeight))
                _frozenList.style.marginBottom = scrollbarHeight;

            // The frozen list may not have been able to reach the offset of the main list before its layout was ready
            SyncScroll();
        }

        /// <summary>
        /// Called when the mouse wheel is used over the frozen list. Scrolls the main list instead, which then scrolls the frozen list.
        /// </summary>
        /// <param name="evt">The emitted event.</param>
        private void OnFrozenListWheel(WheelEvent evt)
        {
            // Same computation as the scroll views of UI Toolkit. The values of the scrollers are clamped to their range
            float scrollSize = _mainScrollView.mouseWheelScrollSize;
            _mainScrollView.verticalScroller.value += evt.delta.y * scrollSize;
            _mainScrollView.horizontalScroller.value += evt.delta.x * scrollSize;
            evt.StopPropagation();
        }

        /// <summary>
        /// Called when a cell of the frozen column changes size, for example when the column is resized.
        /// </summary>
        /// <param name="evt">The emitted event.</param>
        private void OnCellGeometryChanged(GeometryChangedEvent evt)
        {
            if (evt.currentTarget is VisualElement cell && cell.parent != null)
                SetColumnWidth(cell.parent.layout.width);
        }

        #endregion

    }

}
