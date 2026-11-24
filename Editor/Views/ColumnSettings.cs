using System;

using UnityEngine;

namespace SideXP.Database.EditorOnly
{

    /// <summary>
    /// The settings of a column in a view (see <see cref="ViewSettings"/>).
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
        /// The width of the column chosen by the user, in pixels, or 0 if the column has its default width.
        /// </summary>
        [SerializeField]
        private float _width = 0;

        /// <summary>
        /// Checks if the column is hidden.
        /// </summary>
        [SerializeField]
        private bool _isHidden = false;

        #endregion


        #region Lifecycle

        /// <inheritdoc cref="ColumnSettings"/>
        /// <param name="id"><inheritdoc cref="_id" path="/summary"/></param>
        /// <param name="isHidden"><inheritdoc cref="_isHidden" path="/summary"/></param>
        public ColumnSettings(string id, bool isHidden)
        {
            _id = id;
            _isHidden = isHidden;
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

        /// <inheritdoc cref="_isHidden"/>
        public bool IsHidden
        {
            get => _isHidden;
            set => _isHidden = value;
        }

        #endregion

    }

}
