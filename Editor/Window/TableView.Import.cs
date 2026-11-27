using System.Collections.Generic;
using System.IO;

using UnityEngine;
using UnityEditor;

namespace SideXP.Database.EditorOnly
{

    /// <summary>
    /// The import of CSV and TSV files into the table view: an open file dialog, then the preview of the changes in a modal window (see
    /// <see cref="ImportWindow"/>), which applies the import or cancels it.
    /// </summary>
    /// <remarks>
    /// The rows of the file are matched among all the assets of the table, the ones outside of the folder scope and the ones hidden by
    /// the filters included (see <see cref="ImportPlan"/>).
    /// </remarks>
    internal partial class TableView
    {

        #region Fields

        private const string ImportDialogTitle = "Import CSV or TSV";

        /// <summary>
        /// The filters of the open file dialog of the imports: pairs of a name and the extensions it shows.
        /// </summary>
        private static readonly string[] ImportFileFilters = { "CSV and TSV files", "csv,tsv" };

        #endregion


        #region Public API

        /// <summary>
        /// Starts the import of a file into the table: asks for the file in an open dialog, reads it, then opens the preview of the
        /// changes. Shows a message if the file can't be read.
        /// </summary>
        /// <remarks>
        /// The preview is modal: once it's closed, the table reloads if the import has been applied, and a message says what it has
        /// done.
        /// </remarks>
        public void StartImport()
        {
            if (_pipeline == null)
                return;

            // Like a click elsewhere: an edit in progress is confirmed before the dialog opens
            EndEdit(true, false);

            string path = EditorUtility.OpenFilePanelWithFilters(ImportDialogTitle, GetFileFolder(), ImportFileFilters);
            // Empty if the dialog has been cancelled
            if (string.IsNullOrEmpty(path))
                return;

            s_lastFileFolder = Path.GetDirectoryName(path);
            if (!ImportFile.TryRead(path, out ImportFile file, out string error))
            {
                ShowMessage(error);
                return;
            }

            // All the assets of the table, with their current values, so the rows are matched in the whole project
            IReadOnlyList<TableRow> tableRows = TableExport.LoadAllRows(_table.Type, _columns.ConvertAll(column => column.Accessor));
            ImportWindow.Open(_table, _columns, tableRows, file, _creatableTypes, GetDefaultFolder(), OnImportApplied);
        }

        #endregion


        #region Private API

        /// <summary>
        /// Called once an import has been applied. Reloads the table, and says what the import has done, in the console and in the
        /// window. The changes that failed are listed in the console.
        /// </summary>
        /// <param name="result">What the import has done.</param>
        private void OnImportApplied(ImportResult result)
        {
            // The changes are also tracked, but an import can create and rename many files: the table reloads once
            Reload();

            string message = GetImportResultText(result);
            Debug.Log($"{DatabaseConstants.LogPrefix}{message}");
            if (result.Errors.Count > 0)
            {
                Debug.LogWarning($"{DatabaseConstants.LogPrefix}{result.Errors.Count} change(s) of the import into the table " +
                    $"\"{_table.Name}\" can't be applied:\n- {string.Join("\n- ", result.Errors)}");
            }

            // Deferred, so the Database window has the focus again once the preview is closed
            schedule.Execute(() => ShowNotification(message));
        }

        /// <summary>
        /// Gets the text that says what an import has done.
        /// </summary>
        /// <param name="result">What the import has done.</param>
        /// <returns>Returns the text, which mentions the console if some changes failed.</returns>
        private static string GetImportResultText(ImportResult result)
        {
            List<string> parts = new List<string>();
            if (result.ChangedCellCount > 0)
                parts.Add(result.ChangedCellCount == 1 ? "1 value changed" : $"{result.ChangedCellCount} values changed");

            if (result.RenamedCount > 0)
                parts.Add(result.RenamedCount == 1 ? "1 asset renamed" : $"{result.RenamedCount} assets renamed");

            int createdCount = result.CreatedAssets.Count;
            if (createdCount > 0)
                parts.Add(createdCount == 1 ? "1 asset created" : $"{createdCount} assets created");

            string text = parts.Count > 0 ? $"Import: {string.Join(", ", parts)}." : "Import: nothing changed.";
            return result.Errors.Count > 0
                ? $"{text} Some changes can't be applied: see the console."
                : text;
        }

        #endregion

    }

}
