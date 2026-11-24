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
    /// Until views are implemented, it holds the default view of each table: the settings the tool remembers for the user (see
    /// <see cref="ViewSettings"/>).
    /// <para>
    /// Saves are grouped: <see cref="ScheduleSave"/> writes the file once no change has been made for <see cref="SaveDelay"/>, so
    /// resizing a column doesn't write it on every frame. A scheduled save is written right away before a domain reload and when the
    /// editor quits, since the changes that are not saved would be lost.
    /// </para>
    /// </remarks>
    [FilePath(UserViews.RelativeFilePath, FilePathAttribute.Location.ProjectFolder)]
    internal class UserViews : ScriptableSingleton<UserViews>
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

        /// <summary>
        /// The default view of each table that has been opened.
        /// </summary>
        /// <remarks>
        /// A list, since Unity can't serialize dictionaries. The number of tables of a project is small enough for a linear search.
        /// </remarks>
        [SerializeField]
        private List<ViewSettings> _defaultViews = new List<ViewSettings>();

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
        /// Gets the default view of a table, and creates it if the table has none yet.
        /// </summary>
        /// <remarks>
        /// The view is modified directly: call <see cref="ScheduleSave"/> after changing it.
        /// </remarks>
        /// <param name="tableId">The stable identifier of the table (see <see cref="TableDefinition.Id"/>).</param>
        /// <returns>Returns the default view of the table.</returns>
        /// <exception cref="ArgumentException">Thrown if the identifier is null or empty.</exception>
        public ViewSettings GetDefaultView(string tableId)
        {
            if (string.IsNullOrEmpty(tableId))
                throw new ArgumentException("The identifier of the table can't be null or empty.", nameof(tableId));

            ViewSettings view = _defaultViews.Find(item => item != null && item.TableId == tableId);
            if (view == null)
            {
                view = new ViewSettings(tableId);
                _defaultViews.Add(view);
            }

            return view;
        }

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
