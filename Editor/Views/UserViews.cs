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
    /// It holds the personal views of the tables, and the view open for each table (see <see cref="PersonalViews"/>). Before the views,
    /// it held a single view per table (the settings the tool remembered for the user): these settings are moved to a personal view
    /// when the file is loaded (see <see cref="OnAfterDeserialize"/>).
    /// <para>
    /// Saves are grouped: <see cref="ScheduleSave"/> writes the file once no change has been made for <see cref="SaveDelay"/>, so
    /// resizing a column doesn't write it on every frame. A scheduled save is written right away before a domain reload and when the
    /// editor quits, since the changes that are not saved would be lost.
    /// </para>
    /// </remarks>
    [FilePath(UserViews.RelativeFilePath, FilePathAttribute.Location.ProjectFolder)]
    internal class UserViews : ScriptableSingleton<UserViews>, ISerializationCallbackReceiver
    {

        #region Fields

        /// <summary>
        /// The path of the file, from the project folder. <c>Location.ProjectFolder</c> is used instead of
        /// <c>Location.PreferencesFolder</c>, which is shared by all the projects of the user.
        /// </summary>
        private const string RelativeFilePath = "UserSettings/SideXP.Database.Views.asset";

        /// <summary>
        /// The time to wait after the last change before writing the file, in seconds.
        /// </summary>
        private const double SaveDelay = 1;

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

        /// <summary>
        /// The time at which the scheduled save is written (see <see cref="EditorApplication.timeSinceStartup"/>), or a negative value
        /// if no save is scheduled.
        /// </summary>
        [NonSerialized]
        private double _saveTime = -1;

        #endregion


        #region Lifecycle

        /// <summary>
        /// Called when the singleton is loaded, including after a domain reload.
        /// </summary>
        private void OnEnable()
        {
            AssemblyReloadEvents.beforeAssemblyReload += SaveNow;
            EditorApplication.quitting += SaveNow;

            // Saved in the new format, so the views moved from the settings of the first pass keep their ids
            if (_hasImportedDefaultViews)
            {
                _hasImportedDefaultViews = false;
                ScheduleSave();
            }
        }

        /// <summary>
        /// Called when the singleton is unloaded, including before a domain reload.
        /// </summary>
        private void OnDisable()
        {
            AssemblyReloadEvents.beforeAssemblyReload -= SaveNow;
            EditorApplication.quitting -= SaveNow;
            EditorApplication.update -= SaveWhenDue;
        }

        #endregion


        #region Public API

        /// <summary>
        /// The personal views of the tables, and the view open for each table.
        /// </summary>
        /// <remarks>
        /// The views are modified directly: call <see cref="ScheduleSave"/> after changing them.
        /// </remarks>
        public PersonalViews PersonalViews => _personalViews;

        /// <summary>
        /// Writes the file once no change has been made for <see cref="SaveDelay"/>. Each call postpones the save.
        /// </summary>
        public void ScheduleSave()
        {
            bool isScheduled = _saveTime >= 0;
            _saveTime = EditorApplication.timeSinceStartup + SaveDelay;
            if (!isScheduled)
                EditorApplication.update += SaveWhenDue;
        }

        /// <summary>
        /// Writes the file right away if a save is scheduled.
        /// </summary>
        public void SaveNow()
        {
            if (_saveTime < 0)
                return;

            EditorApplication.update -= SaveWhenDue;
            _saveTime = -1;
            // As text, so the file can be read and merged like the other settings of the project
            Save(true);
        }

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
        /// <see cref="PersonalViews.DefaultViewName"/>, open, so the settings of the first pass are not lost.
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


        #region Private API

        /// <summary>
        /// Called on each editor update while a save is scheduled. Writes the file when the delay is over.
        /// </summary>
        private void SaveWhenDue()
        {
            if (EditorApplication.timeSinceStartup >= _saveTime)
                SaveNow();
        }

        #endregion

    }

}
