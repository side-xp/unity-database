using System;
using System.Collections.Generic;
using System.IO;

using UnityEditor;

using Object = UnityEngine.Object;

namespace SideXP.Database.EditorOnly
{

    /// <summary>
    /// The changes that the import of a file makes to a table, computed before they are applied, so the user can review them.
    /// </summary>
    /// <remarks>
    /// Each data row of the file is matched with an asset of the table (see <see cref="ImportRowMatcher"/>), then each of its values
    /// mapped to a column (see <see cref="ImportColumnMap"/>) is converted by the handler of the column. A value that differs from the
    /// value of the cell is a change, compared as the cells display them. A different <i>Asset Name</i> renames the asset. The assets
    /// of the table that no row matches are never changed.
    /// <list type="bullet">
    /// <item>A text that can't be converted is a rejected cell, skipped. A blank text that can't be converted (a number left empty in a
    /// spreadsheet) leaves its cell as it is, without being reported.</item>
    /// <item>A row that matches no asset creates one if the options allow it (see <see cref="ImportOptions.CanCreateAssets"/>), named
    /// after its <i>Asset Name</i>, or after the type, with a number when the name is taken in the folder (<c>Weapon SO 2</c>).</item>
    /// <item>A row whose path or name is shared by several assets, and a row that matches the asset of an earlier row, are skipped.
    /// </item>
    /// </list>
    /// <para>
    /// Applying the plan writes the values and renames the sub-assets as a single Undo step. Creating an asset and renaming a main asset
    /// can't be undone, like in the Project window: the values of a created asset are written without Undo, since undoing them would
    /// leave the asset with the default values.
    /// </para>
    /// </remarks>
    internal class ImportPlan
    {

        #region Fields

        private const string UndoNamePrefix = "Import";
        private const string DefaultUndoName = "Import Rows";

        /// <summary>
        /// Why the values are converted, at the end of the error message of a cell handler that fails.
        /// </summary>
        private const string ImportPurpose = "to import it";

        /// <summary>
        /// The number added to the name of the second created asset that would get a taken name (<c>Weapon SO 2</c>). The first one
        /// has no number.
        /// </summary>
        private const int FirstDuplicateNumber = 2;

        /// <summary>
        /// Replaces the characters that can't be in a file name in the names of the created assets.
        /// </summary>
        private const char InvalidFileNameReplacement = '-';

        /// <summary>
        /// The file to import.
        /// </summary>
        private ImportFile _file = null;

        /// <summary>
        /// The columns of the table that receive the columns of the file.
        /// </summary>
        private ImportColumnMap _columnMap = null;

        /// <summary>
        /// All the columns of the table. The index of a column is its index in the pipeline of the rows.
        /// </summary>
        private IReadOnlyList<TableViewColumn> _columns = null;

        /// <summary>
        /// The choices of the user for the missing assets.
        /// </summary>
        private ImportOptions _options = null;

        /// <summary>
        /// What the import does with each data row of the file, in the order of the file.
        /// </summary>
        private List<ImportRow> _rows = new List<ImportRow>();

        #endregion


        #region Lifecycle

        /// <inheritdoc cref="ImportPlan"/>
        /// <param name="file"><inheritdoc cref="_file" path="/summary"/></param>
        /// <param name="columnMap"><inheritdoc cref="_columnMap" path="/summary"/></param>
        /// <param name="columns"><inheritdoc cref="_columns" path="/summary"/></param>
        /// <param name="options"><inheritdoc cref="_options" path="/summary"/></param>
        private ImportPlan(ImportFile file, ImportColumnMap columnMap, IReadOnlyList<TableViewColumn> columns, ImportOptions options)
        {
            _file = file;
            _columnMap = columnMap;
            _columns = columns;
            _options = options;
        }

        #endregion


        #region Public API

        /// <inheritdoc cref="_file"/>
        public ImportFile File => _file;

        /// <inheritdoc cref="_columnMap"/>
        public ImportColumnMap ColumnMap => _columnMap;

        /// <inheritdoc cref="_options"/>
        public ImportOptions Options => _options;

        /// <inheritdoc cref="_rows"/>
        public IReadOnlyList<ImportRow> Rows => _rows;

        /// <summary>
        /// Gets the number of cells of existing assets that the import writes.
        /// </summary>
        public int ChangedCellCount
        {
            get
            {
                int count = 0;
                foreach (ImportRow row in _rows)
                {
                    if (row.Status == ImportRowStatus.Changed)
                        count += row.Changes.Count;
                }

                return count;
            }
        }

        /// <summary>
        /// Gets the number of cells whose text can't be converted, in the rows that the import applies.
        /// </summary>
        public int RejectedCellCount
        {
            get
            {
                int count = 0;
                foreach (ImportRow row in _rows)
                    count += row.RejectedCells.Count;

                return count;
            }
        }

        /// <summary>
        /// Checks if applying the plan changes something: at least one row changes an asset or creates one.
        /// </summary>
        public bool HasChanges => CountRows(ImportRowStatus.Changed) > 0 || CountRows(ImportRowStatus.Created) > 0;

        /// <summary>
        /// Counts the rows that have a status.
        /// </summary>
        /// <param name="status">The status of the rows to count.</param>
        /// <returns>Returns the number of rows.</returns>
        public int CountRows(ImportRowStatus status)
        {
            int count = 0;
            foreach (ImportRow row in _rows)
            {
                if (row.Status == status)
                    count++;
            }

            return count;
        }

        /// <summary>
        /// Counts the rows that rename their asset.
        /// </summary>
        /// <returns>Returns the number of rows.</returns>
        public int CountRenames()
        {
            int count = 0;
            foreach (ImportRow row in _rows)
            {
                if (row.IsRenamed)
                    count++;
            }

            return count;
        }

        /// <summary>
        /// Computes the changes that the import of a file makes to a table (see <see cref="ImportPlan"/>).
        /// </summary>
        /// <param name="file">The file to import.</param>
        /// <param name="columnMap">The columns of the table that receive the columns of the file.</param>
        /// <param name="columns">All the columns of the table.</param>
        /// <param name="tableRows">All the rows of the table, with their current values (see <see cref="TableExport.LoadAllRows"/>),
        /// whose assets the rows of the file are matched with.</param>
        /// <param name="options">The choices of the user for the missing assets.</param>
        /// <returns>Returns the computed plan.</returns>
        public static ImportPlan Build(ImportFile file, ImportColumnMap columnMap, IReadOnlyList<TableViewColumn> columns,
            IReadOnlyList<TableRow> tableRows, ImportOptions options)
        {
            ImportPlan plan = new ImportPlan(file, columnMap, columns, options);

            Dictionary<Object, TableRow> tableRowsByAsset = new Dictionary<Object, TableRow>(tableRows.Count);
            foreach (TableRow tableRow in tableRows)
            {
                if (tableRow.Asset != null)
                    tableRowsByAsset[tableRow.Asset] = tableRow;
            }

            ImportRowMatcher matcher = new ImportRowMatcher(tableRowsByAsset.Keys);
            int identityColumn = columnMap.IdentityColumn;
            int nameColumn = plan.FindAssetNameFileColumn();
            HashSet<Object> matchedAssets = new HashSet<Object>();
            HashSet<string> createdNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (int rowIndex = 0; rowIndex < file.RowCount; rowIndex++)
            {
                string identityText = identityColumn >= 0 ? file.GetText(rowIndex, identityColumn) : null;
                string nameText = nameColumn >= 0 ? file.GetText(rowIndex, nameColumn) : null;
                ImportMatchKind matchKind = matcher.Match(identityText, nameText, out Object asset, out bool isAmbiguous);
                ImportRow row = new ImportRow(rowIndex, file.GetRowNumber(rowIndex), asset, matchKind);
                plan._rows.Add(row);

                if (asset == null)
                {
                    if (isAmbiguous)
                    {
                        row.Status = ImportRowStatus.Ambiguous;
                    }
                    else if (options.CanCreateAssets)
                    {
                        row.Status = ImportRowStatus.Created;
                        row.NewName = plan.GetCreatedName(nameText, createdNames);
                        plan.ReadValues(row, null);
                    }
                    else
                    {
                        row.Status = ImportRowStatus.Missing;
                    }

                    continue;
                }

                if (!matchedAssets.Add(asset))
                {
                    row.Status = ImportRowStatus.Duplicate;
                    continue;
                }

                plan.ReadValues(row, tableRowsByAsset[asset]);
                row.Status = row.Changes.Count > 0 || row.NewName != null
                    ? ImportRowStatus.Changed
                    : ImportRowStatus.Unchanged;
            }

            return plan;
        }

        /// <summary>
        /// Applies the plan: writes the values of the changed rows, creates the missing assets, then renames the renamed assets. Call it
        /// once, right after the plan is built, so the assets haven't changed in between.
        /// </summary>
        /// <remarks>
        /// The values and the renames of sub-assets are a single Undo step. The assets are created before the renames, since creating
        /// an asset replaces the file at its path: a rename can then only fail on the name of a created asset, never take it. A change
        /// that fails is reported in the result, and the others are applied.
        /// </remarks>
        /// <returns>Returns what the import has done.</returns>
        public ImportResult Apply()
        {
            ImportResult result = new ImportResult();
            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName(_file.Name != null ? $"{UndoNamePrefix} {_file.Name}" : DefaultUndoName);
            int undoGroup = Undo.GetCurrentGroup();

            try
            {
                foreach (ImportRow row in _rows)
                {
                    if (row.Status != ImportRowStatus.Changed || row.Changes.Count == 0)
                        continue;

                    // The asset may have been deleted since the plan was built
                    if (row.Asset == null)
                    {
                        result.Errors.Add($"Row {row.RowNumber}: the asset doesn't exist anymore.");
                        continue;
                    }

                    if (TryWriteValues(row, row.Asset, true, result))
                        result.ChangedCellCount += row.Changes.Count;
                }

                foreach (ImportRow row in _rows)
                {
                    if (row.Status == ImportRowStatus.Created)
                        CreateAsset(row, result);
                }

                foreach (ImportRow row in _rows)
                {
                    if (!row.IsRenamed || row.Asset == null)
                        continue;

                    string oldName = row.Asset.name;
                    if (AssetOperations.TryRename(row.Asset, row.NewName, undoGroup, out string error))
                        result.RenamedCount++;
                    else if (error != null)
                        result.Errors.Add($"Row {row.RowNumber}: {oldName} can't be renamed to {row.NewName}: {error}");
                }
            }
            finally
            {
                Undo.CollapseUndoOperations(undoGroup);
            }

            return result;
        }

        #endregion


        #region Private API

        /// <summary>
        /// Gets the column of the file mapped to the <i>Asset Name</i> column of the table.
        /// </summary>
        /// <returns>Returns the index of the column in the file, or -1 if none is mapped to it.</returns>
        private int FindAssetNameFileColumn()
        {
            for (int i = 0; i < _columns.Count; i++)
            {
                if (_columns[i].IsAssetName)
                    return _columnMap.FindFileColumn(i);
            }

            return -1;
        }

        /// <summary>
        /// Converts the values of a row, and lists the cells they change, its new name and its rejected cells.
        /// </summary>
        /// <param name="row">The row of the file.</param>
        /// <param name="tableRow">The row of the matched asset, with its current values, or null for an asset that the import creates:
        /// all its converted values are then changes.</param>
        private void ReadValues(ImportRow row, TableRow tableRow)
        {
            Object asset = tableRow?.Asset;
            for (int fileColumn = 0; fileColumn < _columnMap.FileColumnCount; fileColumn++)
            {
                int columnIndex = _columnMap.GetTarget(fileColumn);
                if (columnIndex == ImportColumnMap.Ignored || _columnMap.IsReadOnly(fileColumn))
                    continue;

                TableViewColumn column = _columns[columnIndex];
                string text = _file.GetText(row.RowIndex, fileColumn);
                if (column.IsAssetName)
                {
                    // The name of a created asset is chosen with the names of the other created assets
                    string name = text.Trim();
                    if (asset != null && name.Length > 0 && name != asset.name)
                        row.NewName = name;

                    continue;
                }

                if (!column.TryParse(text, asset, ImportPurpose, out object value))
                {
                    if (!string.IsNullOrWhiteSpace(text))
                        row.RejectedCells.Add(new ImportRow.RejectedCell(columnIndex, text));

                    continue;
                }

                // Compared as the cells display them, so a number written differently in the file (1.50 for 1.5) doesn't change it
                string newText = column.ToText(value, ImportPurpose) ?? string.Empty;
                if (tableRow == null)
                {
                    row.Changes.Add(new ImportRow.CellChange(columnIndex, null, newText, value));
                    continue;
                }

                string oldText = column.ToText(tableRow.GetValue(columnIndex), ImportPurpose) ?? string.Empty;
                if (oldText != newText)
                    row.Changes.Add(new ImportRow.CellChange(columnIndex, oldText, newText, value));
            }
        }

        /// <summary>
        /// Gets the name of an asset that the import creates: its <i>Asset Name</i> in the file, or the name of the created type, with a
        /// number if an asset of the folder, or another created asset, already has it.
        /// </summary>
        /// <param name="nameText">The text of the <i>Asset Name</i> column of the row, or null if the file has none.</param>
        /// <param name="takenNames">The names of the assets that the import already creates, to which the name is added.</param>
        /// <returns>Returns the name of the created asset, without extension.</returns>
        private string GetCreatedName(string nameText, HashSet<string> takenNames)
        {
            string baseName = (nameText ?? string.Empty).Trim();
            foreach (char character in Path.GetInvalidFileNameChars())
                baseName = baseName.Replace(character, InvalidFileNameReplacement);

            if (baseName.Length == 0)
                baseName = AssetOperations.GetDisplayName(_options.CreatedType);

            string name = baseName;
            int number = FirstDuplicateNumber;
            while (takenNames.Contains(name) || AssetExists(GetCreatedPath(name)))
            {
                name = $"{baseName} {number}";
                number++;
            }

            takenNames.Add(name);
            return name;
        }

        /// <summary>
        /// Gets the path of an asset that the import creates.
        /// </summary>
        /// <param name="name">The name of the asset.</param>
        /// <returns>Returns the path of the asset in the folder of the options, from the project folder.</returns>
        private string GetCreatedPath(string name)
        {
            return $"{_options.Folder}/{name}{AssetOperations.AssetExtension}";
        }

        /// <summary>
        /// Creates the asset of a row, in the folder of the options, and writes its values without Undo.
        /// </summary>
        /// <param name="row">The row of the file, whose status is <see cref="ImportRowStatus.Created"/>.</param>
        /// <param name="result">The result of the import, which gets the created asset or the error.</param>
        private void CreateAsset(ImportRow row, ImportResult result)
        {
            string path = GetCreatedPath(row.NewName);
            // Creating an asset replaces the file at its path: one may have been created since the plan was built
            if (AssetExists(path))
            {
                result.Errors.Add($"Row {row.RowNumber}: the asset can't be created, since {path} already exists.");
                return;
            }

            if (!AssetOperations.TryCreate(_options.CreatedType, path, out Object asset, out string error))
            {
                result.Errors.Add($"Row {row.RowNumber}: the asset can't be created at {path}: {error}");
                return;
            }

            // Without Undo: the creation can't be undone, so undoing the values would only leave the asset with the default values
            if (row.Changes.Count > 0)
                TryWriteValues(row, asset, false, result);

            AssetDatabase.SaveAssetIfDirty(asset);
            result.CreatedAssets.Add(asset);
        }

        /// <summary>
        /// Writes the changes of a row to an asset.
        /// </summary>
        /// <param name="row">The row of the file.</param>
        /// <param name="asset">The asset to write.</param>
        /// <param name="isUndoable">If enabled, the writes are recorded in the current Undo group.</param>
        /// <param name="result">The result of the import, which gets the error if the values can't be written.</param>
        /// <returns>Returns true if the values have been written.</returns>
        private bool TryWriteValues(ImportRow row, Object asset, bool isUndoable, ImportResult result)
        {
            List<KeyValuePair<string, object>> values = row.Changes.ConvertAll(change =>
                new KeyValuePair<string, object>(_columns[change.ColumnIndex].PropertyPath, change.Value));

            try
            {
                CellEditSession.WriteValues(asset, values, isUndoable);
                return true;
            }
            catch (Exception exception)
            {
                result.Errors.Add($"Row {row.RowNumber}: the values of {asset.name} can't be written: {exception.Message}");
                return false;
            }
        }

        /// <summary>
        /// Checks if an asset exists at a path.
        /// </summary>
        /// <param name="path">The path of the asset, from the project folder.</param>
        /// <returns>Returns true if the path is the one of an existing asset.</returns>
        private static bool AssetExists(string path)
        {
            return !string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(path, AssetPathToGUIDOptions.OnlyExistingAssets));
        }

        #endregion

    }

}
