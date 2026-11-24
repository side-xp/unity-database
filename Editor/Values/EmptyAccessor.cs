using System;

namespace SideXP.Database.EditorOnly
{

    /// <summary>
    /// Reads nothing, for the built-in columns that display controls instead of values, like the <i>Controls</i> column.
    /// </summary>
    /// <remarks>
    /// The columns of a table view are also the columns of its pipeline, so every column needs an accessor. This one costs nothing to
    /// read, and its null values are never sorted nor searched.
    /// </remarks>
    internal class EmptyAccessor : IValueAccessor
    {

        #region Fields

        /// <summary>
        /// The shared instance of this accessor, which has no state.
        /// </summary>
        public static readonly EmptyAccessor Instance = new EmptyAccessor();

        #endregion


        #region Lifecycle

        /// <summary>
        /// Prevents the creation of other instances: use <see cref="Instance"/> instead.
        /// </summary>
        private EmptyAccessor() { }

        #endregion


        #region Public API

        /// <inheritdoc/>
        public Type ValueType => typeof(object);

        /// <inheritdoc/>
        public object Read(ValueReadContext context)
        {
            return null;
        }

        #endregion

    }

}
