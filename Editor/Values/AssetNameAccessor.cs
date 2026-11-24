using System;

namespace SideXP.Database.EditorOnly
{

    /// <summary>
    /// Reads the name of the assets, for the <i>Asset Name</i> built-in column.
    /// </summary>
    internal class AssetNameAccessor : IValueAccessor
    {

        #region Fields

        /// <summary>
        /// The shared instance of this accessor, which has no state.
        /// </summary>
        public static readonly AssetNameAccessor Instance = new AssetNameAccessor();

        #endregion


        #region Lifecycle

        /// <summary>
        /// Prevents the creation of other instances: use <see cref="Instance"/> instead.
        /// </summary>
        private AssetNameAccessor() { }

        #endregion


        #region Public API

        /// <inheritdoc/>
        public Type ValueType => typeof(string);

        /// <inheritdoc/>
        public object Read(ValueReadContext context)
        {
            return context.Asset != null ? context.Asset.name : null;
        }

        #endregion

    }

}
