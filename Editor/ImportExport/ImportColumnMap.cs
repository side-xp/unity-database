using System;
using System.Collections.Generic;

namespace SideXP.Database.EditorOnly
{

    /// <summary>
    /// The columns of the table that receive the columns of a file to import.
    /// </summary>
    /// <remarks>
    /// Each column of the file is mapped automatically, from left to right, by its header: the first column named
    /// <see cref="TableExport.GuidColumnName"/> (ignoring case) is the identity column, which identifies the assets. The others are
    /// mapped to a column of the table by name (ignoring case), then by id, each column of the table receiving one column of the file
    /// at most, in the order of the table, so columns that share a name are mapped in the same order as the export writes them. The
    /// <i>Controls</i> column is never mapped.
    /// <para>
    /// A column of the file that matches no column of the table is unknown, and ignored until it's mapped by hand
    /// (<see cref="SetTarget(int, int)"/>). A column mapped to a column that can't be written (a computed column, for example) is
    /// read-only: its values are ignored.
    /// </para>
    /// </remarks>
    internal class ImportColumnMap
    {

        #region Fields

        /// <summary>
        /// The target of a column of the file whose values are ignored.
        /// </summary>
        public const int Ignored = -1;

        /// <summary>
        /// All the columns of the table. The index of a column is its index in the pipeline of the rows.
        /// </summary>
        private IReadOnlyList<TableViewColumn> _columns = null;

        /// <summary>
        /// The texts of the header row of the file.
        /// </summary>
        private IReadOnlyList<string> _header = null;

        /// <summary>
        /// The index of the identity column in the file, or -1 if the file has none.
        /// </summary>
        private int _identityColumn = -1;

        /// <summary>
        /// The index of the column of the table that receives each column of the file, or <see cref="Ignored"/>.
        /// </summary>
        private int[] _targets = null;

        /// <summary>
        /// Checks if each column of the file has matched no column of the table automatically.
        /// </summary>
        private bool[] _isUnknown = null;

        #endregion


        #region Lifecycle

        /// <summary>
        /// Maps the columns of a file automatically (see <see cref="ImportColumnMap"/>).
        /// </summary>
        /// <param name="header"><inheritdoc cref="_header" path="/summary"/></param>
        /// <param name="columns"><inheritdoc cref="_columns" path="/summary"/></param>
        public ImportColumnMap(IReadOnlyList<string> header, IReadOnlyList<TableViewColumn> columns)
        {
            _header = header;
            _columns = columns;
            _targets = new int[header.Count];
            _isUnknown = new bool[header.Count];
            // All at once, since the columns not mapped yet must not look mapped to the first column of the table
            for (int fileColumn = 0; fileColumn < header.Count; fileColumn++)
                _targets[fileColumn] = Ignored;

            for (int fileColumn = 0; fileColumn < header.Count; fileColumn++)
            {
                string name = (header[fileColumn] ?? string.Empty).Trim();
                if (_identityColumn < 0 && string.Equals(name, TableExport.GuidColumnName, StringComparison.OrdinalIgnoreCase))
                {
                    _identityColumn = fileColumn;
                    continue;
                }

                int target = FindColumn(name);
                _targets[fileColumn] = target;
                _isUnknown[fileColumn] = target == Ignored;
            }
        }

        #endregion


        #region Public API

        /// <summary>
        /// Gets the number of columns of the file.
        /// </summary>
        public int FileColumnCount => _header.Count;

        /// <inheritdoc cref="_identityColumn"/>
        public int IdentityColumn => _identityColumn;

        /// <summary>
        /// Gets the header of a column of the file.
        /// </summary>
        /// <param name="fileColumn">The index of the column in the file.</param>
        /// <returns>Returns the text of the header, trimmed.</returns>
        public string GetHeader(int fileColumn)
        {
            return (_header[fileColumn] ?? string.Empty).Trim();
        }

        /// <summary>
        /// Gets the column of the table that receives a column of the file.
        /// </summary>
        /// <param name="fileColumn">The index of the column in the file.</param>
        /// <returns>Returns the index of the column of the table, or <see cref="Ignored"/> (always for the identity column).</returns>
        public int GetTarget(int fileColumn)
        {
            return _targets[fileColumn];
        }

        /// <summary>
        /// Checks if a column of the file has matched no column of the table automatically. It stays unknown when it's mapped by hand,
        /// so the preview can still offer the choice.
        /// </summary>
        /// <param name="fileColumn">The index of the column in the file.</param>
        /// <returns>Returns true if the column is unknown.</returns>
        public bool IsUnknown(int fileColumn)
        {
            return _isUnknown[fileColumn];
        }

        /// <summary>
        /// Checks if a column of the file is mapped to a column of the table that can't be written, so its values are ignored.
        /// </summary>
        /// <param name="fileColumn">The index of the column in the file.</param>
        /// <returns>Returns true if the column is mapped to a read-only column.</returns>
        public bool IsReadOnly(int fileColumn)
        {
            int target = _targets[fileColumn];
            return target != Ignored && !CanWrite(_columns[target]);
        }

        /// <summary>
        /// Gets the column of the file mapped to a column of the table.
        /// </summary>
        /// <param name="tableColumn">The index of the column of the table.</param>
        /// <returns>Returns the index of the column in the file, or -1 if no column of the file is mapped to it.</returns>
        public int FindFileColumn(int tableColumn)
        {
            return Array.IndexOf(_targets, tableColumn);
        }

        /// <summary>
        /// Gets the columns of the table that a column of the file can be mapped to by hand: the columns that can be written (and
        /// <i>Asset Name</i>), that no other column of the file is mapped to.
        /// </summary>
        /// <param name="fileColumn">The index of the column in the file.</param>
        /// <returns>Returns the indices of the columns of the table, in the order of the table.</returns>
        public List<int> GetAvailableTargets(int fileColumn)
        {
            List<int> targets = new List<int>();
            for (int i = 0; i < _columns.Count; i++)
            {
                int mappedColumn = FindFileColumn(i);
                if (CanWrite(_columns[i]) && (mappedColumn < 0 || mappedColumn == fileColumn))
                    targets.Add(i);
            }

            return targets;
        }

        /// <summary>
        /// Maps a column of the file to a column of the table by hand, or ignores it.
        /// </summary>
        /// <param name="fileColumn">The index of the column in the file, other than the identity column.</param>
        /// <param name="tableColumn">The index of the column of the table, one of <see cref="GetAvailableTargets(int)"/>, or
        /// <see cref="Ignored"/>.</param>
        /// <returns>Returns true if the column has been mapped, false if the target is not available.</returns>
        public bool SetTarget(int fileColumn, int tableColumn)
        {
            if (fileColumn == _identityColumn)
                return false;

            if (tableColumn != Ignored && !GetAvailableTargets(fileColumn).Contains(tableColumn))
                return false;

            _targets[fileColumn] = tableColumn;
            return true;
        }

        /// <summary>
        /// Checks if the values of a column can be imported: the columns stored in a serialized property, and <i>Asset Name</i>, which
        /// renames the assets.
        /// </summary>
        /// <param name="column">The column to check.</param>
        /// <returns>Returns true if the import can write the values of the column.</returns>
        public static bool CanWrite(TableViewColumn column)
        {
            return column.HasProperty || column.IsAssetName;
        }

        #endregion


        #region Private API

        /// <summary>
        /// Finds the column of the table that a header names: by name ignoring case, then by id, among the columns not mapped yet.
        /// </summary>
        /// <param name="name">The trimmed header of the column.</param>
        /// <returns>Returns the index of the column of the table, or <see cref="Ignored"/> if none matches.</returns>
        private int FindColumn(string name)
        {
            if (name.Length == 0)
                return Ignored;

            int target = FindUnmappedColumn(column => string.Equals(column.Name, name, StringComparison.OrdinalIgnoreCase));
            return target != Ignored
                ? target
                : FindUnmappedColumn(column => column.Id == name);
        }

        /// <summary>
        /// Finds the first column of the table, other than <i>Controls</i>, that no column of the file is mapped to and that meets a
        /// condition.
        /// </summary>
        /// <param name="predicate">The condition the column must meet.</param>
        /// <returns>Returns the index of the column of the table, or <see cref="Ignored"/> if none is found.</returns>
        private int FindUnmappedColumn(Predicate<TableViewColumn> predicate)
        {
            for (int i = 0; i < _columns.Count; i++)
            {
                if (!_columns[i].IsControls && predicate(_columns[i]) && FindFileColumn(i) < 0)
                    return i;
            }

            return Ignored;
        }

        #endregion

    }

}
