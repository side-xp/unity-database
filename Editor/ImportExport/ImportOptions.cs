using System;

using UnityEditor;

namespace SideXP.Database.EditorOnly
{

    /// <summary>
    /// The choices of the user for an import: whether the rows that match no asset create one, of which type, and in which folder.
    /// </summary>
    internal class ImportOptions
    {

        #region Fields

        /// <summary>
        /// If enabled, the rows that match no asset create one. Disabled by default, so a file of another project doesn't fill the
        /// table with copies by mistake.
        /// </summary>
        private bool _shouldCreateMissingAssets = false;

        /// <summary>
        /// The type of the assets that the import creates, or null if the table has no type that can be created.
        /// </summary>
        private Type _createdType = null;

        /// <summary>
        /// The folder of the assets that the import creates, from the project folder (<c>Assets/Items</c>).
        /// </summary>
        private string _folder = null;

        #endregion


        #region Lifecycle

        /// <inheritdoc cref="ImportOptions"/>
        /// <param name="shouldCreateMissingAssets"><inheritdoc cref="_shouldCreateMissingAssets" path="/summary"/></param>
        /// <param name="createdType"><inheritdoc cref="_createdType" path="/summary"/></param>
        /// <param name="folder"><inheritdoc cref="_folder" path="/summary"/></param>
        public ImportOptions(bool shouldCreateMissingAssets, Type createdType, string folder)
        {
            _shouldCreateMissingAssets = shouldCreateMissingAssets;
            _createdType = createdType;
            _folder = folder;
        }

        #endregion


        #region Public API

        /// <inheritdoc cref="_shouldCreateMissingAssets"/>
        public bool ShouldCreateMissingAssets => _shouldCreateMissingAssets;

        /// <inheritdoc cref="_createdType"/>
        public Type CreatedType => _createdType;

        /// <inheritdoc cref="_folder"/>
        public string Folder => _folder;

        /// <summary>
        /// Checks if the import creates the missing assets: the option is enabled, the type can be created, and the folder exists.
        /// </summary>
        public bool CanCreateAssets => _shouldCreateMissingAssets
            && AssetOperations.IsCreatable(_createdType)
            && !string.IsNullOrEmpty(_folder)
            && AssetDatabase.IsValidFolder(_folder);

        #endregion

    }

}
