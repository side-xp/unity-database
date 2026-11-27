using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEditor;

namespace SideXP.Database.EditorOnly
{

    /// <summary>
    /// The views of the tables that only the user of this project sees, stored in the <c>UserSettings/</c> folder of the project (not
    /// versioned).
    /// </summary>
    /// <remarks>
    /// It holds the personal views of the tables, the view open for each table, and the personal overrides of the project views (see
    /// <see cref="PersonalViews"/>). Before the views, it held a single view per table (the settings the tool remembered for the
    /// user): these settings are moved to a personal view when the file is loaded (see <see cref="OnAfterDeserialize"/>).
    /// <para>
    /// The saves are grouped (see <see cref="SettingsFile{T}.ScheduleSave"/>).
    /// </para>
    /// </remarks>
    [FilePath(UserViews.RelativeFilePath, FilePathAttribute.Location.ProjectFolder)]
    internal class UserViews : SettingsFile<UserViews>, ISerializationCallbackReceiver
    {

        #region Fields

        /// <summary>
        /// The path of the file, from the project folder. <c>Location.ProjectFolder</c> is used instead of
        /// <c>Location.PreferencesFolder</c>, which is shared by all the projects of the user.
        /// </summary>
        private const string RelativeFilePath = "UserSettings/SideXP.Database.Views.asset";

        /// <inheritdoc cref="PersonalViews"/>
        [SerializeField]
        private PersonalViews _personalViews = new PersonalViews();

        /// <summary>
        /// The single view of each table, saved before the views. Only read, to move them to <see cref="_personalViews"/> (see
        /// <see cref="OnAfterDeserialize"/>).
        /// </summary>
        [SerializeField]
        private List<ViewSettings> _defaultViews = new List<ViewSettings>();

        /// <summary>
        /// Checks if the views saved before the views have been moved to the personal views when the file was loaded, so the file must
        /// be saved in the new format.
        /// </summary>
        [NonSerialized]
        private bool _hasImportedDefaultViews = false;

        #endregion


        #region Lifecycle

        /// <inheritdoc/>
        protected override void OnEnable()
        {
            base.OnEnable();

            // Saved in the new format, so the views moved from the settings of the first pass keep their ids
            if (_hasImportedDefaultViews)
            {
                _hasImportedDefaultViews = false;
                ScheduleSave();
            }
        }

        #endregion


        #region Public API

        /// <summary>
        /// The personal views of the tables, the view open for each table, and the personal overrides of the project views.
        /// </summary>
        /// <remarks>
        /// The views are modified directly: call <see cref="SettingsFile{T}.ScheduleSave"/> after changing them, or use
        /// <see cref="ViewCatalog.Current"/>, which saves after each operation.
        /// </remarks>
        public PersonalViews PersonalViews => _personalViews;

        #endregion


        #region Serialization

        /// <summary>
        /// Called before the views are saved. Nothing to prepare: the views saved before the views are moved when the file is loaded.
        /// </summary>
        public void OnBeforeSerialize()
        {
        }

        /// <summary>
        /// Called after the views are loaded. Moves the single view of each table saved before the views to a personal view named
        /// <see cref="ViewCatalog.DefaultViewName"/>, open, so the settings of the first pass are not lost.
        /// </summary>
        public void OnAfterDeserialize()
        {
            if (_defaultViews.Count == 0)
                return;

            _personalViews ??= new PersonalViews();
            _personalViews.ImportDefaultViews(_defaultViews);
            _defaultViews.Clear();
            _hasImportedDefaultViews = true;
        }

        #endregion

    }

}
