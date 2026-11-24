namespace SideXP.Database.EditorOnly
{

    /// <summary>
    /// Miscellaneous constant values of the Database editor features.
    /// </summary>
    internal static class DatabaseConstants
    {

        /// <summary>
        /// The prefix of the messages logged by the package.
        /// </summary>
        public const string LogPrefix = "[Database] ";

        /// <summary>
        /// The name of the package, as declared in its manifest.
        /// </summary>
        public const string PackageName = "com.side-xp.database";

        /// <summary>
        /// The path of the package folder, from the project. Ends with "/".
        /// </summary>
        public const string PackagePath = "Packages/" + PackageName + "/";

        /// <summary>
        /// The path of the folder of the editor window files, from the project. Ends with "/".
        /// </summary>
        public const string WindowFolderPath = PackagePath + "Editor/Window/";

    }

}
