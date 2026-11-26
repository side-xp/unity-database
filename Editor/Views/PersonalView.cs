using System;

using UnityEngine;

namespace SideXP.Database.EditorOnly
{

    /// <summary>
    /// A view of a table that only its user sees: a name, and the settings of the view (see <see cref="ViewSettings"/>).
    /// </summary>
    /// <remarks>
    /// Views are identified by an id generated when they are created, so they can be renamed. Create, rename and delete them through
    /// <see cref="PersonalViews"/>, which keeps their names unique per table.
    /// </remarks>
    [Serializable]
    internal class PersonalView
    {

        #region Fields

        /// <summary>
        /// The identifier of the view, unique in the project.
        /// </summary>
        [SerializeField]
        private string _id = null;

        /// <summary>
        /// The name of the view, unique among the views of its table.
        /// </summary>
        [SerializeField]
        private string _name = null;

        /// <summary>
        /// The settings of the view, which also refer to its table.
        /// </summary>
        [SerializeField]
        private ViewSettings _settings = null;

        #endregion


        #region Lifecycle

        /// <inheritdoc cref="PersonalView"/>
        /// <param name="name"><inheritdoc cref="_name" path="/summary"/></param>
        /// <param name="settings"><inheritdoc cref="_settings" path="/summary"/></param>
        /// <exception cref="ArgumentNullException">Thrown if the settings are null.</exception>
        public PersonalView(string name, ViewSettings settings)
        {
            _id = Guid.NewGuid().ToString("N");
            _name = name;
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        #endregion


        #region Public API

        /// <inheritdoc cref="_id"/>
        public string Id => _id;

        /// <inheritdoc cref="_name"/>
        public string Name
        {
            get => _name;
            internal set => _name = value;
        }

        /// <inheritdoc cref="_settings"/>
        public ViewSettings Settings => _settings;

        /// <summary>
        /// The stable identifier of the table of the view (see <see cref="TableDefinition.Id"/>).
        /// </summary>
        public string TableId => _settings?.TableId;

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"{_name} ({TableId})";
        }

        #endregion

    }

}
