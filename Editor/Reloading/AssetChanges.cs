using System;
using System.Collections.Generic;

namespace SideXP.Database.EditorOnly
{

    /// <summary>
    /// The paths of the assets changed by an import, as reported by <see cref="DatabaseAssetPostprocessor"/>.
    /// </summary>
    internal class AssetChanges
    {

        #region Fields

        /// <summary>
        /// The paths of the imported assets, created or changed.
        /// </summary>
        private IReadOnlyList<string> _imported = null;

        /// <summary>
        /// The paths of the deleted assets.
        /// </summary>
        private IReadOnlyList<string> _deleted = null;

        /// <summary>
        /// The new paths of the moved or renamed assets.
        /// </summary>
        private IReadOnlyList<string> _moved = null;

        /// <summary>
        /// The previous paths of the moved or renamed assets, in the same order as <see cref="_moved"/>.
        /// </summary>
        private IReadOnlyList<string> _movedFrom = null;

        #endregion


        #region Lifecycle

        /// <inheritdoc cref="AssetChanges"/>
        /// <param name="imported"><inheritdoc cref="_imported" path="/summary"/></param>
        /// <param name="deleted"><inheritdoc cref="_deleted" path="/summary"/></param>
        /// <param name="moved"><inheritdoc cref="_moved" path="/summary"/></param>
        /// <param name="movedFrom"><inheritdoc cref="_movedFrom" path="/summary"/></param>
        public AssetChanges(IReadOnlyList<string> imported, IReadOnlyList<string> deleted, IReadOnlyList<string> moved,
            IReadOnlyList<string> movedFrom)
        {
            _imported = imported ?? Array.Empty<string>();
            _deleted = deleted ?? Array.Empty<string>();
            _moved = moved ?? Array.Empty<string>();
            _movedFrom = movedFrom ?? Array.Empty<string>();
        }

        #endregion


        #region Public API

        /// <inheritdoc cref="_imported"/>
        public IReadOnlyList<string> Imported => _imported;

        /// <inheritdoc cref="_deleted"/>
        public IReadOnlyList<string> Deleted => _deleted;

        /// <inheritdoc cref="_moved"/>
        public IReadOnlyList<string> Moved => _moved;

        /// <inheritdoc cref="_movedFrom"/>
        public IReadOnlyList<string> MovedFrom => _movedFrom;

        /// <summary>
        /// Checks if there is no change at all.
        /// </summary>
        public bool IsEmpty => _imported.Count == 0 && _deleted.Count == 0 && _moved.Count == 0 && _movedFrom.Count == 0;

        #endregion

    }

}
