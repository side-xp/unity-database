using System;

using UnityEngine;
using UnityEditor;

namespace SideXP.Database.EditorOnly
{

    /// <summary>
    /// A settings file of the package, loaded once per domain reload, whose saves are grouped (see <see cref="UserViews"/> and
    /// <see cref="ProjectViews"/>).
    /// </summary>
    /// <remarks>
    /// <see cref="ScheduleSave"/> writes the file once no change has been made for <see cref="SaveDelay"/>, so resizing a column
    /// doesn't write it on every frame. A scheduled save is written right away before a domain reload and when the editor quits,
    /// since the changes that are not saved would be lost. The file is written as text, so it can be read and merged like the other
    /// settings of the project.
    /// </remarks>
    /// <typeparam name="T">The type of the settings file, which declares its path with <see cref="FilePathAttribute"/>.</typeparam>
    internal abstract class SettingsFile<T> : ScriptableSingleton<T>
        where T : ScriptableObject
    {

        #region Fields

        /// <summary>
        /// The time to wait after the last change before writing the file, in seconds.
        /// </summary>
        private const double SaveDelay = 1;

        /// <summary>
        /// The time at which the scheduled save is written (see <see cref="EditorApplication.timeSinceStartup"/>), or a negative value
        /// if no save is scheduled.
        /// </summary>
        [NonSerialized]
        private double _saveTime = -1;

        #endregion


        #region Lifecycle

        /// <summary>
        /// Called when the file is loaded, including after a domain reload.
        /// </summary>
        protected virtual void OnEnable()
        {
            AssemblyReloadEvents.beforeAssemblyReload += SaveNow;
            EditorApplication.quitting += SaveNow;
        }

        /// <summary>
        /// Called when the file is unloaded, including before a domain reload.
        /// </summary>
        protected virtual void OnDisable()
        {
            AssemblyReloadEvents.beforeAssemblyReload -= SaveNow;
            EditorApplication.quitting -= SaveNow;
            EditorApplication.update -= SaveWhenDue;
        }

        #endregion


        #region Public API

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
