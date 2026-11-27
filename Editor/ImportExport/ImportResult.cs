using System.Collections.Generic;

using Object = UnityEngine.Object;

namespace SideXP.Database.EditorOnly
{

    /// <summary>
    /// What an import has done (see <see cref="ImportPlan.Apply"/>).
    /// </summary>
    internal class ImportResult
    {

        #region Fields

        /// <summary>
        /// The number of cells of existing assets that have been written.
        /// </summary>
        private int _changedCellCount = 0;

        /// <summary>
        /// The number of existing assets that have been renamed.
        /// </summary>
        private int _renamedCount = 0;

        /// <summary>
        /// The assets that have been created, in the order of the file.
        /// </summary>
        private List<Object> _createdAssets = new List<Object>();

        /// <summary>
        /// Why some changes have not been applied, one message per failed change.
        /// </summary>
        private List<string> _errors = new List<string>();

        #endregion


        #region Public API

        /// <inheritdoc cref="_changedCellCount"/>
        public int ChangedCellCount
        {
            get => _changedCellCount;
            set => _changedCellCount = value;
        }

        /// <inheritdoc cref="_renamedCount"/>
        public int RenamedCount
        {
            get => _renamedCount;
            set => _renamedCount = value;
        }

        /// <inheritdoc cref="_createdAssets"/>
        public List<Object> CreatedAssets => _createdAssets;

        /// <inheritdoc cref="_errors"/>
        public List<string> Errors => _errors;

        #endregion

    }

}
