using UnityEngine.UIElements;

namespace SideXP.Database.EditorOnly
{

    /// <summary>
    /// Sent by a live control of a cell display (a checkbox, a dropdown) when the user changes its value. Bubbles up to the cell,
    /// whose table writes the value to the asset of the row.
    /// </summary>
    /// <remarks>
    /// Cell handlers send it with <c>CellHandler.SubmitValue()</c>, since they don't know the asset of the cell when they create its
    /// display: elements are reused for other rows when the table scrolls.
    /// </remarks>
    internal class CellValueSubmitEvent : EventBase<CellValueSubmitEvent>
    {

        #region Lifecycle

        /// <inheritdoc cref="CellValueSubmitEvent"/>
        public CellValueSubmitEvent()
        {
            LocalInit();
        }

        /// <summary>
        /// Gets an event from the pool of events, and initializes it with the submitted value.
        /// </summary>
        /// <param name="value">The value submitted by the control, of the type of the column.</param>
        /// <returns>Returns the initialized event. Dispose it after sending it.</returns>
        public static CellValueSubmitEvent GetPooled(object value)
        {
            CellValueSubmitEvent evt = GetPooled();
            evt.Value = value;
            return evt;
        }

        /// <inheritdoc/>
        protected override void Init()
        {
            base.Init();
            LocalInit();
        }

        /// <summary>
        /// Sets the propagation of the event and clears its value, when it's created and when it's reused from the pool.
        /// </summary>
        private void LocalInit()
        {
            bubbles = true;
            tricklesDown = false;
            Value = null;
        }

        #endregion


        #region Public API

        /// <summary>
        /// The value submitted by the control, of the type of the column.
        /// </summary>
        public object Value { get; private set; }

        #endregion

    }

}
