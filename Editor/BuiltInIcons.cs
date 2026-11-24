using System.Collections.Generic;

using UnityEngine;
using UnityEditor;

namespace SideXP.Database.EditorOnly
{

    /// <summary>
    /// Declares the names of the built-in editor icons used by the package, and loads them.
    /// </summary>
    /// <remarks>
    /// The names of the built-in icons are not documented, and can change between Unity versions, so they are all declared here. The
    /// editor loads the dark version of an icon (<c>d_</c> prefix) when the dark theme is active. Most buttons use Tabler icons
    /// instead (see <see cref="DatabaseIcons"/>).
    /// </remarks>
    internal static class BuiltInIcons
    {

        #region Fields

        /// <summary>
        /// A plus sign, for the button that creates an asset when the table has a single type to create. The <c>+</c> button of
        /// Unity's reorderable lists.
        /// </summary>
        public const string Add = "Toolbar Plus";

        /// <summary>
        /// A plus sign with a dropdown arrow, for the button that creates an asset when the table has several types to choose from.
        /// The <c>+</c> button of Unity's reorderable lists that opens a menu.
        /// </summary>
        public const string AddDropdown = "Toolbar Plus More";

        /// <summary>
        /// A small warning sign, for the mark of obsolete tables.
        /// </summary>
        public const string Warning = "console.warnicon.sml";

        /// <summary>
        /// The loaded icons, mapped by their name.
        /// </summary>
        private static Dictionary<string, Texture> s_icons = new Dictionary<string, Texture>();

        #endregion


        #region Public API

        /// <summary>
        /// Loads a built-in icon.
        /// </summary>
        /// <param name="name">The name of the icon, one of the constants of this class.</param>
        /// <returns>Returns the loaded icon, or null if it doesn't exist in this version of Unity.</returns>
        public static Texture Get(string name)
        {
            if (!s_icons.TryGetValue(name, out Texture icon) || icon == null)
            {
                icon = EditorGUIUtility.IconContent(name).image;
                s_icons[name] = icon;
            }
            return icon;
        }

        #endregion

    }

}
