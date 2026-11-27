using System.Collections.Generic;

using Object = UnityEngine.Object;

namespace SideXP.Database.EditorOnly
{

    /// <summary>
    /// What an import does with a row of its file: the asset it matches, its status, and the changes it makes (see
    /// <see cref="ImportPlan"/>).
    /// </summary>
    internal class ImportRow
    {

        #region Subclasses

        /// <summary>
        /// A value that the import writes to a cell.
        /// </summary>
        public readonly struct CellChange
        {

            /// <summary>
            /// The index of the column of the cell in the table.
            /// </summary>
            public readonly int ColumnIndex;

            /// <summary>
            /// The current value of the cell as text, or null for an asset that the import creates.
            /// </summary>
            public readonly string OldText;

            /// <summary>
            /// The new value of the cell as text, as the cell will display it (a value out of the limits of its field is clamped, for
            /// example).
            /// </summary>
            public readonly string NewText;

            /// <summary>
            /// The new value of the cell, converted from the text of the file.
            /// </summary>
            public readonly object Value;

            /// <inheritdoc cref="CellChange"/>
            /// <param name="columnIndex"><inheritdoc cref="ColumnIndex" path="/summary"/></param>
            /// <param name="oldText"><inheritdoc cref="OldText" path="/summary"/></param>
            /// <param name="newText"><inheritdoc cref="NewText" path="/summary"/></param>
            /// <param name="value"><inheritdoc cref="Value" path="/summary"/></param>
            public CellChange(int columnIndex, string oldText, string newText, object value)
            {
                ColumnIndex = columnIndex;
                OldText = oldText;
                NewText = newText;
                Value = value;
            }

        }

        /// <summary>
        /// A text of the file that the handler of its column can't convert, so the import skips its cell.
        /// </summary>
        public readonly struct RejectedCell
        {

            /// <summary>
            /// The index of the column of the cell in the table.
            /// </summary>
            public readonly int ColumnIndex;

            /// <summary>
            /// The text of the file.
            /// </summary>
            public readonly string Text;

            /// <inheritdoc cref="RejectedCell"/>
            /// <param name="columnIndex"><inheritdoc cref="ColumnIndex" path="/summary"/></param>
            /// <param name="text"><inheritdoc cref="Text" path="/summary"/></param>
            public RejectedCell(int columnIndex, string text)
            {
                ColumnIndex = columnIndex;
                Text = text;
            }

        }

        #endregion


        #region Fields

        /// <summary>
        /// The index of the row among the data rows of the file, from 0.
        /// </summary>
        private int _rowIndex = 0;

        /// <summary>
        /// The number of the row in the file, as a spreadsheet app shows it (the header is the row 1).
        /// </summary>
        private int _rowNumber = 0;

        /// <summary>
        /// The asset that the row matches, or null if it matches none.
        /// </summary>
        private Object _asset = null;

        /// <summary>
        /// How the row has been matched with its asset.
        /// </summary>
        private ImportMatchKind _matchKind = ImportMatchKind.None;

        /// <summary>
        /// What the import does with the row.
        /// </summary>
        private ImportRowStatus _status = ImportRowStatus.Missing;

        /// <summary>
        /// The new name of the matched asset if the row renames it, the name of the asset that the import creates, or null.
        /// </summary>
        private string _newName = null;

        /// <summary>
        /// The values that the import writes to the cells of the row. For an asset that the import creates, all the values of the file
        /// that can be converted.
        /// </summary>
        private List<CellChange> _changes = new List<CellChange>();

        /// <summary>
        /// The texts of the row that can't be converted, so their cells are skipped.
        /// </summary>
        private List<RejectedCell> _rejectedCells = new List<RejectedCell>();

        #endregion


        #region Lifecycle

        /// <inheritdoc cref="ImportRow"/>
        /// <param name="rowIndex"><inheritdoc cref="_rowIndex" path="/summary"/></param>
        /// <param name="rowNumber"><inheritdoc cref="_rowNumber" path="/summary"/></param>
        /// <param name="asset"><inheritdoc cref="_asset" path="/summary"/></param>
        /// <param name="matchKind"><inheritdoc cref="_matchKind" path="/summary"/></param>
        public ImportRow(int rowIndex, int rowNumber, Object asset, ImportMatchKind matchKind)
        {
            _rowIndex = rowIndex;
            _rowNumber = rowNumber;
            _asset = asset;
            _matchKind = matchKind;
        }

        #endregion


        #region Public API

        /// <inheritdoc cref="_rowIndex"/>
        public int RowIndex => _rowIndex;

        /// <inheritdoc cref="_rowNumber"/>
        public int RowNumber => _rowNumber;

        /// <inheritdoc cref="_asset"/>
        public Object Asset => _asset;

        /// <inheritdoc cref="_matchKind"/>
        public ImportMatchKind MatchKind => _matchKind;

        /// <inheritdoc cref="_status"/>
        public ImportRowStatus Status
        {
            get => _status;
            set => _status = value;
        }

        /// <inheritdoc cref="_newName"/>
        public string NewName
        {
            get => _newName;
            set => _newName = value;
        }

        /// <inheritdoc cref="_changes"/>
        public List<CellChange> Changes => _changes;

        /// <inheritdoc cref="_rejectedCells"/>
        public List<RejectedCell> RejectedCells => _rejectedCells;

        /// <summary>
        /// Checks if the row renames the asset it matches.
        /// </summary>
        public bool IsRenamed => _status == ImportRowStatus.Changed && _newName != null;

        #endregion

    }

}
