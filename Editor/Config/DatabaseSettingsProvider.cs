using UnityEditor;

using SideXP.Core.EditorOnly;

namespace SideXP.Database.EditorOnly
{

    /// <summary>
    /// Registers the page to edit the preferences of the Database package from the Preferences window.
    /// </summary>
    internal static class DatabaseSettingsProvider
    {

        /// <summary>
        /// The name of the page, under the pages of the Sideways Experiments packages.
        /// </summary>
        private const string PageName = "Database";

        /// <summary>
        /// Registers the page to edit <see cref="DatabasePreferences"/> from the Preferences window.
        /// </summary>
        /// <returns>Returns the created settings provider.</returns>
        [SettingsProvider]
        private static SettingsProvider RegisterPreferencesMenu()
        {
            return EditorConfigSettingsProvider.Create<DatabasePreferences>($"{EditorConstants.Preferences}/{PageName}",
                SettingsScope.User);
        }

    }

}
