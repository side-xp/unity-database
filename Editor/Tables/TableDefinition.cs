using System;
using System.Collections.Generic;

namespace SideXP.Database.EditorOnly
{

    /// <summary>
    /// Describes a table: the type of asset it lists, its name, its options and its columns. Tables declared in code with
    /// <see cref="TableAttribute"/> and tables defined in the editor produce the same definition.
    /// </summary>
    public class TableDefinition
    {

        #region Fields

        /// <summary>
        /// The stable identifier of the table, used to remember its settings when the code changes.
        /// </summary>
        /// <remarks>
        /// For a table declared in code, it's the GUID of the type's script file, or the type name (with its assembly name) when the type
        /// has no script file of its own (nested types, types declared in a file named after another type, types compiled in a DLL).
        /// </remarks>
        private string _id = null;

        /// <summary>
        /// The type of the assets listed in the table. The table also lists the assets of its derived types.
        /// </summary>
        private Type _type = null;

        /// <summary>
        /// The name of the table. Can contain <c>/</c> to group tables in the table list.
        /// </summary>
        private string _name = null;

        /// <summary>
        /// Checks if the type of the table is marked with <see cref="ObsoleteAttribute"/>.
        /// </summary>
        private bool _isObsolete = false;

        /// <inheritdoc cref="TableAttribute.HideAssetName"/>
        private bool _hideAssetName = false;

        /// <inheritdoc cref="TableAttribute.AllowRenameAsset"/>
        private bool _allowRenameAsset = false;

        /// <summary>
        /// The columns of the table, in their default order.
        /// </summary>
        private IReadOnlyList<ColumnDefinition> _columns = null;

        #endregion


        #region Lifecycle

        /// <inheritdoc cref="TableDefinition"/>
        /// <param name="id"><inheritdoc cref="_id" path="/summary"/></param>
        /// <param name="type"><inheritdoc cref="_type" path="/summary"/></param>
        /// <param name="name"><inheritdoc cref="_name" path="/summary"/></param>
        /// <param name="isObsolete"><inheritdoc cref="_isObsolete" path="/summary"/></param>
        /// <param name="hideAssetName"><inheritdoc cref="TableAttribute.HideAssetName" path="/summary"/></param>
        /// <param name="allowRenameAsset"><inheritdoc cref="TableAttribute.AllowRenameAsset" path="/summary"/></param>
        /// <param name="columns"><inheritdoc cref="_columns" path="/summary"/></param>
        internal TableDefinition(
            string id,
            Type type,
            string name,
            bool isObsolete,
            bool hideAssetName,
            bool allowRenameAsset,
            IReadOnlyList<ColumnDefinition> columns)
        {
            _id = id;
            _type = type;
            _name = name;
            _isObsolete = isObsolete;
            _hideAssetName = hideAssetName;
            _allowRenameAsset = allowRenameAsset;
            _columns = columns;
        }

        #endregion


        #region Public API

        /// <inheritdoc cref="_id"/>
        public string Id => _id;

        /// <inheritdoc cref="_type"/>
        public Type Type => _type;

        /// <inheritdoc cref="_name"/>
        public string Name => _name;

        /// <inheritdoc cref="_isObsolete"/>
        public bool IsObsolete => _isObsolete;

        /// <inheritdoc cref="TableAttribute.HideAssetName"/>
        public bool HideAssetName => _hideAssetName;

        /// <inheritdoc cref="TableAttribute.AllowRenameAsset"/>
        public bool AllowRenameAsset => _allowRenameAsset;

        /// <inheritdoc cref="_columns"/>
        public IReadOnlyList<ColumnDefinition> Columns => _columns;

        #endregion

    }

}
