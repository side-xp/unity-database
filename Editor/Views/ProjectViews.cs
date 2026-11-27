using UnityEngine;
using UnityEditor;

namespace SideXP.Database.EditorOnly
{

    /// <summary>
    /// The views of the tables shared with the team, stored in the <c>ProjectSettings/</c> folder of the project (versioned).
    /// </summary>
    /// <remarks>
    /// It holds the project views of the tables (see <see cref="ProjectViewList"/>). The file only changes when a view is shared,
    /// renamed or deleted: the changes a user makes to the settings of a project view are personal overrides, stored in
    /// <see cref="UserViews"/>.
    /// <para>
    /// The file is loaded once per domain reload: a file changed on disk (for example after pulling from version control) is read
    /// again on the next domain reload. Conflicts can be solved with Unity's Smart Merge tool (<c>UnityYAMLMerge</c>), since the file
    /// is written as text.
    /// </para>
    /// </remarks>
    [FilePath(ProjectViews.RelativeFilePath, FilePathAttribute.Location.ProjectFolder)]
    internal class ProjectViews : SettingsFile<ProjectViews>
    {

        #region Fields

        /// <summary>
        /// The path of the file, from the project folder. Also shown to the user, who must commit the file to share the views.
        /// </summary>
        public const string RelativeFilePath = "ProjectSettings/SideXP.Database.Views.asset";

        /// <inheritdoc cref="ProjectViewList"/>
        [SerializeField]
        private ProjectViewList _views = new ProjectViewList();

        #endregion


        #region Public API

        /// <summary>
        /// The project views of the tables.
        /// </summary>
        /// <remarks>
        /// The views are modified directly: call <see cref="SettingsFile{T}.ScheduleSave"/> after changing them, or use
        /// <see cref="ViewCatalog.Current"/>, which saves after each operation.
        /// </remarks>
        public ProjectViewList Views => _views;

        #endregion

    }

}
