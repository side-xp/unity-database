using System;

using UnityEngine;

namespace SideXP.Database.EditorOnly
{

    /// <summary>
    /// The personal overrides of a project view: the settings the user changed in it, as a layer above the settings of the project
    /// view (see <see cref="LayeredViewSettings"/>). Stored with the personal views (see <see cref="PersonalViews"/>).
    /// </summary>
    [Serializable]
    internal class ViewOverride
    {

        #region Fields

        /// <summary>
        /// The identifier of the overridden project view (see <see cref="SavedView.Id"/>).
        /// </summary>
        [SerializeField]
        private string _viewId = null;

        /// <summary>
        /// The settings the user changed in the project view. The other settings are unset.
        /// </summary>
        [SerializeField]
        private ViewSettings _settings = null;

        #endregion


        #region Lifecycle

        /// <inheritdoc cref="ViewOverride"/>
        /// <param name="viewId"><inheritdoc cref="_viewId" path="/summary"/></param>
        /// <param name="settings"><inheritdoc cref="_settings" path="/summary"/></param>
        /// <exception cref="ArgumentNullException">Thrown if the settings are null.</exception>
        public ViewOverride(string viewId, ViewSettings settings)
        {
            _viewId = viewId;
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        #endregion


        #region Public API

        /// <inheritdoc cref="_viewId"/>
        public string ViewId => _viewId;

        /// <inheritdoc cref="_settings"/>
        public ViewSettings Settings => _settings;

        #endregion

    }

}
