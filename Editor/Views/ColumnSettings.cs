using System;

using UnityEngine;

namespace SideXP.Database.EditorOnly
{

    /// <summary>
    /// The settings of a column in a layer of the settings of a view (see <see cref="ViewSettings"/>): its width and its visibility,
    /// each of them set or not.
    /// </summary>
    [Serializable]
    internal class ColumnSettings
    {

        #region Fields

        /// <summary>
        /// The identifier of the column in its table (see <see cref="TableViewColumn.Id"/>).
        /// </summary>
        [SerializeField]
        private string _id = null;

        /// <summary>
        /// The width of the column chosen by the user, in pixels, or 0 if the layer doesn't set it.
        /// </summary>
        [SerializeField]
        private float _width = 0;

        /// <summary>
        /// The visibility of the column, or <see cref="ColumnVisibility.Unset"/> if the layer doesn't set it.
        /// </summary>
        [SerializeField]
        private ColumnVisibility _visibility = ColumnVisibility.Unset;

        /// <summary>
        /// Checks if the column is hidden, in the settings saved before the views. Only read, to move it to <see cref="_visibility"/>
        /// (see <see cref="ReadVisibilityOfFirstPass"/>).
        /// </summary>
        [SerializeField]
        private bool _isHidden = false;

        #endregion


        #region Lifecycle

        /// <inheritdoc cref="ColumnSettings"/>
        /// <param name="id"><inheritdoc cref="_id" path="/summary"/></param>
        public ColumnSettings(string id)
        {
            _id = id;
        }

        #endregion


        #region Public API

        /// <inheritdoc cref="_id"/>
        public string Id => _id;

        /// <inheritdoc cref="_width"/>
        public float Width
        {
            get => _width;
            set => _width = value;
        }

        /// <inheritdoc cref="_visibility"/>
        public ColumnVisibility Visibility
        {
            get => _visibility;
            set => _visibility = value;
        }

        /// <summary>
        /// Checks if the settings set nothing: neither the width nor the visibility of the column.
        /// </summary>
        public bool IsEmpty => _width <= 0 && _visibility == ColumnVisibility.Unset;

        /// <summary>
        /// Gets a copy of these settings.
        /// </summary>
        /// <returns>Returns the copied settings.</returns>
        public ColumnSettings Clone()
        {
            return new ColumnSettings(_id) { _width = _width, _visibility = _visibility };
        }

        /// <summary>
        /// Reads the visibility saved before the views: every column of these settings had an explicit visibility, so it's kept as a
        /// set visibility.
        /// </summary>
        public void ReadVisibilityOfFirstPass()
        {
            _visibility = _isHidden ? ColumnVisibility.Hidden : ColumnVisibility.Visible;
            _isHidden = false;
        }

        #endregion

    }

}
