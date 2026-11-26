using System;

using UnityEngine;

namespace SideXP.Database.EditorOnly
{

    /// <summary>
    /// A view of a table, saved in a file: a personal view (see <see cref="PersonalView"/>) or a project view (see
    /// <see cref="ProjectView"/>). It has an id, a name and the settings of the view (see <see cref="ViewSettings"/>).
    /// </summary>
    /// <remarks>
    /// Views are identified by an id generated when they are created, so they can be renamed. A personal view shared as a project
    /// view keeps its id. Create, rename and delete them through <see cref="ViewCatalog"/>, which keeps their names unique per table.
    /// </remarks>
    [Serializable]
    internal abstract class SavedView
    {

        #region Fields

        /// <summary>
        /// The identifier of the view, unique in the project.
        /// </summary>
        [SerializeField]
        private string _id = null;

        /// <summary>
        /// The name of the view, unique among the views of its table when it's created or renamed.
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

        /// <summary>
        /// Creates a view with a new id.
        /// </summary>
        /// <param name="name"><inheritdoc cref="_name" path="/summary"/></param>
        /// <param name="settings"><inheritdoc cref="_settings" path="/summary"/></param>
        /// <exception cref="ArgumentNullException">Thrown if the settings are null.</exception>
        protected SavedView(string name, ViewSettings settings)
            : this(Guid.NewGuid().ToString("N"), name, settings)
        {
        }

        /// <summary>
        /// Creates a view with a given id, for example the id of the personal view shared as a project view.
        /// </summary>
        /// <param name="id"><inheritdoc cref="_id" path="/summary"/></param>
        /// <param name="name"><inheritdoc cref="_name" path="/summary"/></param>
        /// <param name="settings"><inheritdoc cref="_settings" path="/summary"/></param>
        /// <exception cref="ArgumentNullException">Thrown if the settings are null.</exception>
        protected SavedView(string id, string name, ViewSettings settings)
        {
            _id = id;
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

        /// <summary>
        /// Checks if the view is a project view, shared with the team, instead of a personal view.
        /// </summary>
        public abstract bool IsProjectView { get; }

        /// <summary>
        /// Checks if the view can be used: it has an id and settings with a table. A hand-edited file can contain views that can't.
        /// </summary>
        public bool IsValid => !string.IsNullOrEmpty(_id) && !string.IsNullOrEmpty(TableId);

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"{_name} ({TableId})";
        }

        #endregion

    }

}
