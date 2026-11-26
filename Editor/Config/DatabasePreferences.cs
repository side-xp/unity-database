using System;

using UnityEngine;
using UnityEditor;

using SideXP.Core.EditorOnly;

namespace SideXP.Database.EditorOnly
{

    /// <summary>
    /// User preferences of the Database package: the pagination of the tables.
    /// </summary>
    /// <remarks>
    /// These preferences are stored in <see cref="EditorPrefs"/>, so they're shared by all the projects (and all the Unity versions)
    /// on the user's machine. They're edited from the <i>Sideways Experiments &gt; Database</i> page of the Preferences window (see
    /// <see cref="DatabaseSettingsProvider"/>), and the page size also from the status line of the tables.
    /// </remarks>
    [EditorConfig(EEditorConfigScope.Editor, Key = "SideXP.Database.EditorOnly.DatabasePreferences")]
    internal class DatabasePreferences : EditorConfig<DatabasePreferences>
    {

        #region Fields

        /// <summary>
        /// The smallest page size the user can choose. Smaller pages would only make the user browse more pages.
        /// </summary>
        public const int MinPageSize = 10;

        /// <summary>
        /// The page size until the user chooses one: a few screens of rows, so the list stays short to scroll.
        /// </summary>
        public const int DefaultPageSize = 100;

        /// <summary>
        /// The number of rows per page of the tables.
        /// </summary>
        [SerializeField]
        [Min(MinPageSize)]
        [Tooltip("The number of rows per page of the tables, in all the projects. Can also be typed in the status line of a table.")]
        private int _pageSize = DefaultPageSize;

        /// <summary>
        /// Called after the page size changed, from the status line of a table or from the Preferences window, so all the open tables
        /// apply it.
        /// </summary>
        public static event Action PageSizeChanged;

        #endregion


        #region Lifecycle

        /// <summary>
        /// Called when a value is changed from the Preferences window, and when the preferences are loaded. Keeps the page size in its
        /// range, and notifies the change on the next editor update, once the preferences are loaded and saved.
        /// </summary>
        private void OnValidate()
        {
            _pageSize = Mathf.Max(MinPageSize, _pageSize);
            EditorApplication.delayCall -= NotifyPageSizeChanged;
            EditorApplication.delayCall += NotifyPageSizeChanged;
        }

        #endregion


        #region Public API

        /// <inheritdoc cref="_pageSize"/>
        /// <remarks>
        /// A size under <see cref="MinPageSize"/> (from storage edited by hand) is read as the minimum.
        /// </remarks>
        public int PageSize => Mathf.Max(MinPageSize, _pageSize);

        /// <summary>
        /// Sets the number of rows per page of the tables, saves the preferences, and notifies <see cref="PageSizeChanged"/>.
        /// </summary>
        /// <param name="pageSize">The number of rows per page. A size under <see cref="MinPageSize"/> (including 0 and negative sizes)
        /// is raised to it.</param>
        /// <returns>Returns true if the page size has changed.</returns>
        public bool SetPageSize(int pageSize)
        {
            pageSize = Mathf.Max(MinPageSize, pageSize);
            if (pageSize == PageSize)
                return false;

            _pageSize = pageSize;
            Save();
            PageSizeChanged?.Invoke();
            return true;
        }

        #endregion


        #region Private API

        /// <summary>
        /// Notifies <see cref="PageSizeChanged"/>, after a change made from the Preferences window.
        /// </summary>
        private static void NotifyPageSizeChanged()
        {
            PageSizeChanged?.Invoke();
        }

        #endregion

    }

}
