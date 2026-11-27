using System;
using System.Collections.Generic;
using System.IO;

using UnityEngine;
using UnityEditor;

namespace SideXP.Database.EditorOnly
{

    /// <summary>
    /// The export of the table view to CSV and TSV files (see <see cref="TableExport"/>).
    /// </summary>
    /// <remarks>
    /// The view is exported as it's displayed: its visible columns in their display order, and its visible rows in their order, of all
    /// the pages (the filters, the folder scope and the search apply). The whole table is exported without the settings of the view:
    /// all the columns, hidden and computed ones included, in their default order, and all the assets of the table in the project, by
    /// path.
    /// </remarks>
    internal partial class TableView
    {

        #region Fields

        private const string RowText = "row";
        private const string RowsText = "rows";

        /// <summary>
        /// The text that separates the name of the table and the name of the view in the suggested name of the exported file of a
        /// view.
        /// </summary>
        private const string ExportFileNameSeparator = " - ";

        /// <summary>
        /// Replaces the characters that can't be in a file name in the suggested name of an exported file, such as the slashes of the
        /// groups in the name of a table.
        /// </summary>
        private const char InvalidFileNameReplacement = '-';

        /// <summary>
        /// The folder of the last exported file, suggested by the next save dialog. Static, so it's shared by the windows. Forgotten
        /// after a domain reload, when the dialog starts in the folder of the project again.
        /// </summary>
        private static string s_lastExportFolder = null;

        #endregion


        #region Public API

        /// <summary>
        /// Exports the rows of the table to a file chosen in a save dialog. Shows a message in the window and in the console once the
        /// file is written, or why it can't be written.
        /// </summary>
        /// <param name="isWholeTable">If enabled, the whole table is exported, ignoring the settings of the view: all the columns and all
        /// the assets of the table in the project. Otherwise, the view is exported as it's displayed: the displayed columns, and the
        /// visible rows of all the pages.</param>
        /// <param name="format">The format of the file.</param>
        /// <returns>Returns true if the file has been written, false if the dialog has been cancelled, if the file can't be written, or
        /// if no table is displayed.</returns>
        public bool Export(bool isWholeTable, DelimitedFormat format)
        {
            if (_pipeline == null)
                return false;

            // The value being edited is part of what the user sees
            EndEdit(true, false);
            Refresh();

            string formatName = TableExport.GetFormatName(format);
            string path = EditorUtility.SaveFilePanel($"Export {formatName}", GetExportFolder(), GetExportFileName(isWholeTable),
                TableExport.GetExtension(format));

            // Empty if the dialog has been cancelled
            if (string.IsNullOrEmpty(path))
                return false;

            s_lastExportFolder = Path.GetDirectoryName(path);
            IReadOnlyList<TableRow> rows = isWholeTable
                ? TableExport.LoadAllRows(_table.Type, _columns.ConvertAll(column => column.Accessor))
                : _pipeline.VisibleRows;

            List<List<string>> texts = TableExport.GetRows(_columns, GetExportedColumns(isWholeTable), rows);
            try
            {
                TableExport.WriteFile(path, TableExport.Write(texts, format));
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
            {
                ShowMessage($"The file {path} can't be written: {exception.Message} If a spreadsheet app has it open, close it, then " +
                    "export again.");
                return false;
            }

            string rowsText = rows.Count == 1 ? RowText : RowsText;
            Debug.Log($"{DatabaseConstants.LogPrefix}{rows.Count} {rowsText} of the table \"{_table.Name}\" exported to {path}");
            ShowNotification($"{rows.Count} {rowsText} exported to {Path.GetFileName(path)}");
            return true;
        }

        #endregion


        #region Private API

        /// <summary>
        /// Gets the columns to export: all the columns for the whole table, in their default order, or the displayed columns for the
        /// view, in their display order. The <i>Controls</i> column is skipped by <see cref="TableExport.GetRows"/>.
        /// </summary>
        /// <param name="isWholeTable">If enabled, the whole table is exported.</param>
        /// <returns>Returns the indices of the columns in the pipeline, in their order in the file.</returns>
        private List<int> GetExportedColumns(bool isWholeTable)
        {
            if (isWholeTable)
            {
                List<int> allColumns = new List<int>(_columns.Count);
                for (int i = 0; i < _columns.Count; i++)
                    allColumns.Add(i);

                return allColumns;
            }

            // The user may have dragged a header since the order was last read
            SaveColumnOrder();
            return GetColumnOrder().FindAll(columnIndex => IsColumnDisplayed(columnIndex));
        }

        /// <summary>
        /// Gets the name suggested by the save dialog of an export: the name of the table, followed by the name of the open view if the
        /// view is exported. The characters that can't be in a file name are replaced.
        /// </summary>
        /// <param name="isWholeTable">If enabled, the whole table is exported.</param>
        /// <returns>Returns the suggested name, without extension.</returns>
        private string GetExportFileName(bool isWholeTable)
        {
            string name = _table.Name;
            SavedView view = ViewCatalog.Current.GetOpenView(_table.Id);
            if (!isWholeTable && view != null)
                name = $"{name}{ExportFileNameSeparator}{view.Name}";

            foreach (char character in Path.GetInvalidFileNameChars())
                name = name.Replace(character, InvalidFileNameReplacement);

            return name;
        }

        /// <summary>
        /// Gets the folder in which the save dialog of an export starts: the folder of the last exported file if it still exists, or the
        /// folder of the project.
        /// </summary>
        /// <returns>Returns the absolute path of the folder.</returns>
        private static string GetExportFolder()
        {
            if (!string.IsNullOrEmpty(s_lastExportFolder) && Directory.Exists(s_lastExportFolder))
                return s_lastExportFolder;

            // The parent of the Assets folder
            return Path.GetDirectoryName(Application.dataPath);
        }

        #endregion

    }

}
