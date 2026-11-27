using System;
using System.Collections.Generic;
using System.IO;

using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;

namespace SideXP.Database.EditorOnly
{

    /// <summary>
    /// The preview of an import: a modal window that shows the changes that the import of a file makes to a table, lets the user map
    /// the unknown columns and choose whether to create the missing assets, then applies the import or cancels it.
    /// </summary>
    /// <remarks>
    /// The window is modal, so the assets can't change under the changes it shows. The changes are computed again each time a choice
    /// changes (see <see cref="ImportPlan"/>). It's only opened by <see cref="Open"/>: a window shown without an import closes itself.
    /// </remarks>
    internal class ImportWindow : EditorWindow
    {

        #region Subclasses

        /// <summary>
        /// The kinds of lines of the list of the changes.
        /// </summary>
        private enum LineKind
        {

            /// <summary>
            /// The title of a row of the file: its number and its asset.
            /// </summary>
            Row,

            /// <summary>
            /// A change of a row: a value, a rename.
            /// </summary>
            Change,

            /// <summary>
            /// A row that the import skips, and why.
            /// </summary>
            SkippedRow,

            /// <summary>
            /// A value of a row that can't be converted, so the import skips it.
            /// </summary>
            SkippedValue,

        }

        /// <summary>
        /// A line of the list of the changes.
        /// </summary>
        private readonly struct PreviewLine
        {

            /// <summary>
            /// The text of the line.
            /// </summary>
            public readonly string Text;

            /// <summary>
            /// The kind of the line, which sets its style.
            /// </summary>
            public readonly LineKind Kind;

            /// <inheritdoc cref="PreviewLine"/>
            /// <param name="text"><inheritdoc cref="Text" path="/summary"/></param>
            /// <param name="kind"><inheritdoc cref="Kind" path="/summary"/></param>
            public PreviewLine(string text, LineKind kind)
            {
                Text = text;
                Kind = kind;
            }

        }

        #endregion


        #region Fields

        private const string WindowClass = "database-import";
        private const string HeaderClass = "database-import__header";
        private const string TitleClass = "database-import__title";
        private const string SummaryClass = "database-import__summary";
        private const string SectionClass = "database-import__section";
        private const string SectionTitleClass = "database-import__section-title";
        private const string ColumnsClass = "database-import__columns";
        private const string ColumnClass = "database-import__column";
        private const string ColumnHeaderClass = "database-import__column-header";
        private const string ColumnArrowClass = "database-import__column-arrow";
        private const string ColumnTargetClass = "database-import__column-target";
        private const string ColumnTargetFadedClass = "database-import__column-target--faded";
        private const string FolderClass = "database-import__folder";
        private const string FolderFieldClass = "database-import__folder-field";
        private const string HintClass = "database-import__hint";
        private const string ChangesClass = "database-import__changes";
        private const string LineClass = "database-import__line";
        private const string RowLineClass = "database-import__line--row";
        private const string ChangeLineClass = "database-import__line--change";
        private const string SkippedRowLineClass = "database-import__line--skipped-row";
        private const string SkippedValueLineClass = "database-import__line--skipped-value";
        private const string FooterClass = "database-import__footer";
        private const string FooterSpacerClass = "database-import__footer-spacer";
        private const string MessageClass = "database-message";

        private const string WindowTitlePrefix = "Import";
        private const string ColumnsTitle = "Columns";
        private const string MissingAssetsTitle = "Missing Assets";
        private const string ChangesTitle = "Changes";
        private const string IdentityText = "Identifies the assets";
        private const string IgnoreText = "Ignore";
        private const string ReadOnlyText = "read-only, ignored";
        private const string NoHeaderText = "(no header)";
        private const string EmptyValueText = "(empty)";
        private const string UnknownColumnTooltip = "No column of the table has this name. Choose the column that receives its values, " +
            "or ignore it.";
        private const string TypeLabel = "Type";
        private const string FolderLabel = "Folder";
        private const string ChooseFolderText = "Choose...";
        private const string ChooseFolderTitle = "Folder of the Created Assets";
        private const string NotProjectFolderMessage = "The folder must be a folder of the project: in Assets, or in a package " +
            "embedded in the Packages folder.";
        private const string DialogOkText = "OK";
        private const string NotCreatableText = "No asset of this table can be created: its type is abstract, and has no concrete " +
            "derived type.";
        private const string CreationHintText = "Creating assets can't be undone.";
        private const string NoChangeText = "Nothing to change: the assets already have the values of the file.";
        private const string UndoHintText = "Edit > Undo reverts the values and the renames of sub-assets.";
        private const string MainRenameHintText = "Renaming a main asset renames its file, which can't be undone.";
        private const string CancelText = "Cancel";
        private const string ApplyText = "Apply";

        /// <summary>
        /// The arrow between an old value and a new value, and between a column of the file and its column in the table.
        /// </summary>
        private const string ArrowText = "→";

        /// <summary>
        /// Shown instead of the line breaks of a value, so the lines of the list keep one line of text each.
        /// </summary>
        private const string LineBreakText = " ↵ ";

        /// <summary>
        /// The height of the lines of the list of the changes, in pixels.
        /// </summary>
        private const float LineHeight = 18;

        /// <summary>
        /// The size of the window when it opens, in pixels.
        /// </summary>
        private static readonly Vector2 InitialSize = new Vector2(640, 520);

        /// <summary>
        /// The minimum size of the window, in pixels.
        /// </summary>
        private static readonly Vector2 MinSize = new Vector2(480, 360);

        /// <summary>
        /// The table that receives the import.
        /// </summary>
        private TableDefinition _table = null;

        /// <summary>
        /// All the columns of the table. The index of a column is its index in the pipeline of the rows.
        /// </summary>
        private IReadOnlyList<TableViewColumn> _columns = null;

        /// <summary>
        /// All the rows of the table, with their current values.
        /// </summary>
        private IReadOnlyList<TableRow> _tableRows = null;

        /// <summary>
        /// The file to import.
        /// </summary>
        private ImportFile _file = null;

        /// <summary>
        /// The columns of the table that receive the columns of the file, changed by the user for the unknown columns.
        /// </summary>
        private ImportColumnMap _columnMap = null;

        /// <summary>
        /// The types of the assets that can be created in the table, the table type first.
        /// </summary>
        private IReadOnlyList<Type> _creatableTypes = null;

        /// <summary>
        /// If enabled, the rows that match no asset create one.
        /// </summary>
        private bool _shouldCreateMissingAssets = false;

        /// <summary>
        /// The index of the type of the created assets in <see cref="_creatableTypes"/>.
        /// </summary>
        private int _createdTypeIndex = 0;

        /// <summary>
        /// The folder of the created assets, from the project folder.
        /// </summary>
        private string _folder = null;

        /// <summary>
        /// Called once the import is applied, with what it has done.
        /// </summary>
        private Action<ImportResult> _applied = null;

        /// <summary>
        /// The changes of the import, with the current choices.
        /// </summary>
        private ImportPlan _plan = null;

        /// <summary>
        /// The lines of the list of the changes, built from <see cref="_plan"/>.
        /// </summary>
        private List<PreviewLine> _lines = new List<PreviewLine>();

        private Label _summaryLabel = null;
        private VisualElement _columnsList = null;
        private VisualElement _missingSection = null;
        private Toggle _createToggle = null;
        private DropdownField _typeField = null;
        private VisualElement _folderRow = null;
        private TextField _folderField = null;
        private ListView _changesList = null;
        private Label _undoHintLabel = null;
        private Button _applyButton = null;

        #endregion


        #region Lifecycle

        /// <summary>
        /// Opens the preview of the import of a file into a table, as a modal window: the call returns once the window is closed.
        /// </summary>
        /// <param name="table">The table that receives the import.</param>
        /// <param name="columns">All the columns of the table.</param>
        /// <param name="tableRows">All the rows of the table, with their current values (see <see cref="TableExport.LoadAllRows"/>).
        /// </param>
        /// <param name="file">The file to import.</param>
        /// <param name="creatableTypes">The types of the assets that can be created in the table, the table type first.</param>
        /// <param name="folder">The folder suggested for the created assets, from the project folder.</param>
        /// <param name="applied">Called once the import is applied, with what it has done. Not called if the import is cancelled.
        /// </param>
        public static void Open(TableDefinition table, IReadOnlyList<TableViewColumn> columns, IReadOnlyList<TableRow> tableRows,
            ImportFile file, IReadOnlyList<Type> creatableTypes, string folder, Action<ImportResult> applied)
        {
            ImportWindow window = CreateInstance<ImportWindow>();
            window._table = table;
            window._columns = columns;
            window._tableRows = tableRows;
            window._file = file;
            window._columnMap = new ImportColumnMap(file.Header, columns);
            window._creatableTypes = creatableTypes;
            window._folder = folder;
            window._applied = applied;

            window.titleContent = new GUIContent($"{WindowTitlePrefix} {file.Name}");
            window.minSize = MinSize;
            Rect mainWindow = EditorGUIUtility.GetMainWindowPosition();
            window.position = new Rect(mainWindow.center - InitialSize / 2, InitialSize);
            window.ShowModalUtility();
        }

        /// <summary>
        /// Builds the UI of the window, and computes the changes of the import.
        /// </summary>
        private void CreateGUI()
        {
            // Not opened by Open(): there is nothing to preview
            if (_file == null)
            {
                EditorApplication.delayCall += Close;
                return;
            }

            StyleSheet styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(DatabaseWindow.StyleSheetPath);
            if (styleSheet != null)
                rootVisualElement.styleSheets.Add(styleSheet);

            rootVisualElement.AddToClassList(WindowClass);
            rootVisualElement.Add(CreateHeader());
            rootVisualElement.Add(CreateColumnsSection());
            _missingSection = CreateMissingSection();
            rootVisualElement.Add(_missingSection);
            rootVisualElement.Add(CreateChangesSection());
            rootVisualElement.Add(CreateFooter());

            RebuildColumns();
            UpdatePlan();
        }

        #endregion


        #region UI

        /// <summary>
        /// Creates the header of the window: what is imported into which table, and the summary of the changes.
        /// </summary>
        /// <returns>Returns the created header.</returns>
        private VisualElement CreateHeader()
        {
            VisualElement header = new VisualElement();
            header.AddToClassList(HeaderClass);

            Label title = new Label($"{WindowTitlePrefix} {_file.Name} into the table {_table.Name}");
            title.AddToClassList(TitleClass);
            header.Add(title);

            _summaryLabel = new Label();
            _summaryLabel.AddToClassList(SummaryClass);
            header.Add(_summaryLabel);
            return header;
        }

        /// <summary>
        /// Creates the section of the columns of the file, filled by <see cref="RebuildColumns"/>.
        /// </summary>
        /// <returns>Returns the created section.</returns>
        private VisualElement CreateColumnsSection()
        {
            VisualElement section = CreateSection(ColumnsTitle);
            ScrollView scrollView = new ScrollView(ScrollViewMode.Vertical);
            scrollView.AddToClassList(ColumnsClass);
            _columnsList = scrollView;
            section.Add(scrollView);
            return section;
        }

        /// <summary>
        /// Creates the section of the missing assets: the option that creates them, their type and their folder. Shown only when rows
        /// match no asset (see <see cref="UpdatePlan"/>).
        /// </summary>
        /// <returns>Returns the created section.</returns>
        private VisualElement CreateMissingSection()
        {
            VisualElement section = CreateSection(MissingAssetsTitle);
            if (_creatableTypes.Count == 0)
            {
                section.Add(CreateHint(NotCreatableText));
                return section;
            }

            _createToggle = new Toggle();
            _createToggle.RegisterValueChangedCallback(evt =>
            {
                _shouldCreateMissingAssets = evt.newValue;
                UpdatePlan();
            });
            section.Add(_createToggle);

            if (_creatableTypes.Count > 1)
            {
                List<string> typeNames = new List<string>(_creatableTypes.Count);
                foreach (Type type in _creatableTypes)
                    typeNames.Add(AssetOperations.GetDisplayName(type));

                _typeField = new DropdownField(TypeLabel, typeNames, _createdTypeIndex);
                _typeField.RegisterValueChangedCallback(evt =>
                {
                    _createdTypeIndex = Math.Max(0, _typeField.index);
                    UpdatePlan();
                });
                section.Add(_typeField);
            }

            _folderRow = new VisualElement();
            _folderRow.AddToClassList(FolderClass);
            _folderField = new TextField(FolderLabel) { isReadOnly = true, value = _folder };
            _folderField.AddToClassList(FolderFieldClass);
            _folderRow.Add(_folderField);
            _folderRow.Add(new Button(ChooseFolder) { text = ChooseFolderText });
            section.Add(_folderRow);

            section.Add(CreateHint(CreationHintText));
            return section;
        }

        /// <summary>
        /// Creates the section of the changes: a virtualized list, since a file can have thousands of rows.
        /// </summary>
        /// <returns>Returns the created section.</returns>
        private VisualElement CreateChangesSection()
        {
            VisualElement section = CreateSection(ChangesTitle);
            section.style.flexGrow = 1;

            _changesList = new ListView(_lines, LineHeight, MakeLine, BindLine)
            {
                selectionType = SelectionType.None,
                makeNoneElement = () =>
                {
                    Label label = new Label(NoChangeText);
                    label.AddToClassList(MessageClass);
                    return label;
                },
            };
            _changesList.AddToClassList(ChangesClass);
            section.Add(_changesList);
            return section;
        }

        /// <summary>
        /// Creates the footer of the window: what Undo reverts, then the buttons.
        /// </summary>
        /// <returns>Returns the created footer.</returns>
        private VisualElement CreateFooter()
        {
            VisualElement footer = new VisualElement();
            footer.AddToClassList(FooterClass);

            _undoHintLabel = CreateHint(string.Empty);
            footer.Add(_undoHintLabel);

            VisualElement spacer = new VisualElement();
            spacer.AddToClassList(FooterSpacerClass);
            footer.Add(spacer);

            footer.Add(new Button(Close) { text = CancelText });
            _applyButton = new Button(Apply) { text = ApplyText };
            footer.Add(_applyButton);
            return footer;
        }

        /// <summary>
        /// Creates a section of the window, with its title.
        /// </summary>
        /// <param name="title">The title of the section.</param>
        /// <returns>Returns the created section.</returns>
        private static VisualElement CreateSection(string title)
        {
            VisualElement section = new VisualElement();
            section.AddToClassList(SectionClass);
            Label titleLabel = new Label(title);
            titleLabel.AddToClassList(SectionTitleClass);
            section.Add(titleLabel);
            return section;
        }

        /// <summary>
        /// Creates a faded label that explains something.
        /// </summary>
        /// <param name="text">The text of the label.</param>
        /// <returns>Returns the created label.</returns>
        private static Label CreateHint(string text)
        {
            Label hint = new Label(text);
            hint.AddToClassList(HintClass);
            return hint;
        }

        /// <summary>
        /// Creates the rows of the columns of the file again, since mapping an unknown column changes the columns available to the
        /// others.
        /// </summary>
        private void RebuildColumns()
        {
            _columnsList.Clear();
            for (int fileColumn = 0; fileColumn < _columnMap.FileColumnCount; fileColumn++)
                _columnsList.Add(CreateColumnRow(fileColumn));
        }

        /// <summary>
        /// Creates the row of a column of the file: its header, then what receives its values. An unknown column gets a dropdown of
        /// the columns it can be mapped to.
        /// </summary>
        /// <param name="fileColumn">The index of the column in the file.</param>
        /// <returns>Returns the created row.</returns>
        private VisualElement CreateColumnRow(int fileColumn)
        {
            VisualElement row = new VisualElement();
            row.AddToClassList(ColumnClass);

            string headerText = _columnMap.GetHeader(fileColumn);
            Label header = new Label(headerText.Length > 0 ? headerText : NoHeaderText) { tooltip = headerText };
            header.AddToClassList(ColumnHeaderClass);
            row.Add(header);

            Label arrow = new Label(ArrowText);
            arrow.AddToClassList(ColumnArrowClass);
            row.Add(arrow);

            int target = _columnMap.GetTarget(fileColumn);
            if (fileColumn == _columnMap.IdentityColumn)
            {
                row.Add(CreateTargetLabel(IdentityText, true));
            }
            else if (_columnMap.IsUnknown(fileColumn))
            {
                row.Add(CreateTargetDropdown(fileColumn));
            }
            else if (_columnMap.IsReadOnly(fileColumn))
            {
                row.Add(CreateTargetLabel($"{_columns[target].Name} ({ReadOnlyText})", true));
            }
            else
            {
                row.Add(CreateTargetLabel(_columns[target].Name, false));
            }

            return row;
        }

        /// <summary>
        /// Creates the label of the column that receives the values of a column of the file.
        /// </summary>
        /// <param name="text">The text of the label.</param>
        /// <param name="isFaded">If enabled, the label is faded, for the columns whose values are not written.</param>
        /// <returns>Returns the created label.</returns>
        private static Label CreateTargetLabel(string text, bool isFaded)
        {
            Label label = new Label(text);
            label.AddToClassList(ColumnTargetClass);
            label.EnableInClassList(ColumnTargetFadedClass, isFaded);
            return label;
        }

        /// <summary>
        /// Creates the dropdown of an unknown column of the file: <i>Ignore</i>, then the columns of the table it can be mapped to.
        /// </summary>
        /// <param name="fileColumn">The index of the column in the file.</param>
        /// <returns>Returns the created dropdown.</returns>
        private DropdownField CreateTargetDropdown(int fileColumn)
        {
            List<int> targets = _columnMap.GetAvailableTargets(fileColumn);
            List<string> choices = new List<string>(targets.Count + 1) { IgnoreText };
            foreach (int target in targets)
                choices.Add(_columns[target].Name);

            // The first choice is Ignore, so the index of a target is its index in the list plus one
            int index = targets.IndexOf(_columnMap.GetTarget(fileColumn)) + 1;
            DropdownField dropdown = new DropdownField(choices, index) { tooltip = UnknownColumnTooltip };
            dropdown.AddToClassList(ColumnTargetClass);
            dropdown.RegisterValueChangedCallback(evt =>
            {
                int chosenIndex = dropdown.index;
                _columnMap.SetTarget(fileColumn, chosenIndex > 0 ? targets[chosenIndex - 1] : ImportColumnMap.Ignored);
                // Deferred, since the dropdown that sent the event is removed by the rebuild
                rootVisualElement.schedule.Execute(RebuildColumns);
                UpdatePlan();
            });
            return dropdown;
        }

        /// <summary>
        /// Creates a line of the list of the changes.
        /// </summary>
        /// <returns>Returns the created line.</returns>
        private static VisualElement MakeLine()
        {
            Label label = new Label();
            label.AddToClassList(LineClass);
            return label;
        }

        /// <summary>
        /// Shows a line of the changes in an element of the list.
        /// </summary>
        /// <param name="element">The element created by <see cref="MakeLine"/>.</param>
        /// <param name="index">The index of the line in <see cref="_lines"/>.</param>
        private void BindLine(VisualElement element, int index)
        {
            Label label = (Label)element;
            PreviewLine line = _lines[index];
            label.text = line.Text;
            label.tooltip = line.Text;
            label.EnableInClassList(RowLineClass, line.Kind == LineKind.Row);
            label.EnableInClassList(ChangeLineClass, line.Kind == LineKind.Change);
            label.EnableInClassList(SkippedRowLineClass, line.Kind == LineKind.SkippedRow);
            label.EnableInClassList(SkippedValueLineClass, line.Kind == LineKind.SkippedValue);
        }

        #endregion


        #region Plan

        /// <summary>
        /// Computes the changes of the import again with the current choices, and shows them.
        /// </summary>
        private void UpdatePlan()
        {
            Type createdType = _creatableTypes.Count > 0
                ? _creatableTypes[Math.Min(_createdTypeIndex, _creatableTypes.Count - 1)]
                : null;
            ImportOptions options = new ImportOptions(_shouldCreateMissingAssets, createdType, _folder);
            _plan = ImportPlan.Build(_file, _columnMap, _columns, _tableRows, options);

            BuildLines();
            _changesList.Rebuild();
            _summaryLabel.text = GetSummaryText();
            UpdateMissingSection();
            _undoHintLabel.text = GetUndoHintText();
            _applyButton.SetEnabled(_plan.HasChanges);
        }

        /// <summary>
        /// Shows the section of the missing assets if rows match no asset, with their number, and enables its fields while the option
        /// is enabled.
        /// </summary>
        private void UpdateMissingSection()
        {
            int missingCount = _plan.CountRows(ImportRowStatus.Missing) + _plan.CountRows(ImportRowStatus.Created);
            _missingSection.SetDisplayed(missingCount > 0);
            if (_createToggle == null)
                return;

            _createToggle.text = missingCount == 1
                ? "Create the missing asset"
                : $"Create the {missingCount} missing assets";
            _createToggle.SetValueWithoutNotify(_shouldCreateMissingAssets);
            _typeField?.SetEnabled(_shouldCreateMissingAssets);
            _folderRow.SetEnabled(_shouldCreateMissingAssets);
        }

        /// <summary>
        /// Builds the lines of the list of the changes: the rows that change or create an asset with their changes, the values that
        /// can't be converted, and the skipped rows. The unchanged rows are only counted in the summary.
        /// </summary>
        private void BuildLines()
        {
            _lines.Clear();
            foreach (ImportRow row in _plan.Rows)
            {
                switch (row.Status)
                {
                    case ImportRowStatus.Changed:
                        AddLine($"Row {row.RowNumber}: {row.Asset.name} ({GetMatchText(row.MatchKind)})", LineKind.Row);
                        if (row.IsRenamed)
                            AddLine(GetRenameText(row), LineKind.Change);

                        foreach (ImportRow.CellChange change in row.Changes)
                        {
                            string columnName = _columns[change.ColumnIndex].Name;
                            AddLine($"{columnName}: {GetValueText(change.OldText)} {ArrowText} {GetValueText(change.NewText)}",
                                LineKind.Change);
                        }

                        AddRejectedLines(row);
                        break;

                    case ImportRowStatus.Created:
                        string typeName = AssetOperations.GetDisplayName(_plan.Options.CreatedType);
                        AddLine($"Row {row.RowNumber}: new {typeName} \"{row.NewName}\"", LineKind.Row);
                        foreach (ImportRow.CellChange change in row.Changes)
                            AddLine($"{_columns[change.ColumnIndex].Name}: {GetValueText(change.NewText)}", LineKind.Change);

                        AddRejectedLines(row);
                        break;

                    case ImportRowStatus.Unchanged:
                        if (row.RejectedCells.Count == 0)
                            break;

                        AddLine($"Row {row.RowNumber}: {row.Asset.name} ({GetMatchText(row.MatchKind)})", LineKind.Row);
                        AddRejectedLines(row);
                        break;

                    case ImportRowStatus.Missing:
                        AddLine($"Row {row.RowNumber}{GetRowHint(row)}: no asset matches the row, skipped", LineKind.SkippedRow);
                        break;

                    case ImportRowStatus.Ambiguous:
                        AddLine($"Row {row.RowNumber}{GetRowHint(row)}: several assets match the row, skipped", LineKind.SkippedRow);
                        break;

                    case ImportRowStatus.Duplicate:
                        AddLine($"Row {row.RowNumber}: {row.Asset.name} is already matched by an earlier row, skipped",
                            LineKind.SkippedRow);
                        break;
                }
            }
        }

        /// <summary>
        /// Adds a line to the list of the changes.
        /// </summary>
        /// <param name="text">The text of the line.</param>
        /// <param name="kind">The kind of the line.</param>
        private void AddLine(string text, LineKind kind)
        {
            _lines.Add(new PreviewLine(text, kind));
        }

        /// <summary>
        /// Adds the lines of the values of a row that can't be converted.
        /// </summary>
        /// <param name="row">The row of the file.</param>
        private void AddRejectedLines(ImportRow row)
        {
            foreach (ImportRow.RejectedCell cell in row.RejectedCells)
            {
                AddLine($"{_columns[cell.ColumnIndex].Name}: \"{GetValueText(cell.Text)}\" can't be converted, skipped",
                    LineKind.SkippedValue);
            }
        }

        /// <summary>
        /// Gets the text of the rename of a row, which says that renaming a main asset can't be undone.
        /// </summary>
        /// <param name="row">The row of the file, which renames its asset.</param>
        /// <returns>Returns the text of the rename.</returns>
        private static string GetRenameText(ImportRow row)
        {
            string text = $"Asset Name: {row.Asset.name} {ArrowText} {row.NewName}";
            return AssetDatabase.IsMainAsset(row.Asset)
                ? $"{text} (renames the file, can't be undone)"
                : text;
        }

        /// <summary>
        /// Gets the text that says how a row has been matched with its asset.
        /// </summary>
        /// <param name="matchKind">How the row has been matched.</param>
        /// <returns>Returns the text of the match.</returns>
        private static string GetMatchText(ImportMatchKind matchKind)
        {
            switch (matchKind)
            {
                case ImportMatchKind.Id:
                    return "by GUID";

                case ImportMatchKind.Path:
                    return "by path";

                case ImportMatchKind.Name:
                    return "by name";

                default:
                    return "no match";
            }
        }

        /// <summary>
        /// Gets what identifies a row that matches no asset in the file, so the user can find it: its <i>Asset Name</i>, or the text of
        /// its identity column.
        /// </summary>
        /// <param name="row">The row of the file.</param>
        /// <returns>Returns the text in parentheses, after a space, or an empty text if the row has neither.</returns>
        private string GetRowHint(ImportRow row)
        {
            int nameColumn = FindAssetNameFileColumn();
            string hint = nameColumn >= 0 ? _file.GetText(row.RowIndex, nameColumn).Trim() : string.Empty;
            if (hint.Length == 0 && _columnMap.IdentityColumn >= 0)
                hint = _file.GetText(row.RowIndex, _columnMap.IdentityColumn).Trim();

            return hint.Length > 0 ? $" ({GetValueText(hint)})" : string.Empty;
        }

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
        /// Gets a value as a line of the list shows it: on one line, and a mark for an empty value.
        /// </summary>
        /// <param name="text">The text of the value.</param>
        /// <returns>Returns the text to show.</returns>
        private static string GetValueText(string text)
        {
            if (string.IsNullOrEmpty(text))
                return EmptyValueText;

            return text.Replace("\r\n", LineBreakText).Replace("\n", LineBreakText).Replace("\r", LineBreakText);
        }

        /// <summary>
        /// Gets the summary of the changes: the number of rows of the file, then what the import does with them.
        /// </summary>
        /// <returns>Returns the text of the summary.</returns>
        private string GetSummaryText()
        {
            List<string> parts = new List<string>();
            int changedCount = _plan.CountRows(ImportRowStatus.Changed);
            if (changedCount > 0)
            {
                List<string> details = new List<string>();
                if (_plan.ChangedCellCount > 0)
                    details.Add(GetCountText(_plan.ChangedCellCount, "value", "values"));

                int renameCount = _plan.CountRenames();
                if (renameCount > 0)
                    details.Add(GetCountText(renameCount, "rename", "renames"));

                parts.Add($"{changedCount} changed ({string.Join(", ", details)})");
            }

            AddSummaryPart(parts, _plan.CountRows(ImportRowStatus.Created), "created");
            AddSummaryPart(parts, _plan.CountRows(ImportRowStatus.Unchanged), "unchanged");
            int skippedCount = _plan.CountRows(ImportRowStatus.Missing) + _plan.CountRows(ImportRowStatus.Ambiguous)
                + _plan.CountRows(ImportRowStatus.Duplicate);
            AddSummaryPart(parts, skippedCount, "skipped");

            string rowsText = GetCountText(_file.RowCount, "row", "rows");
            string summary = parts.Count > 0 ? $"{rowsText}: {string.Join(", ", parts)}." : $"{rowsText}.";
            int rejectedCount = _plan.RejectedCellCount;
            return rejectedCount > 0
                ? $"{summary} {GetCountText(rejectedCount, "value", "values")} can't be converted, and will be skipped."
                : summary;
        }

        /// <summary>
        /// Adds a part to the summary, if its count is not 0.
        /// </summary>
        /// <param name="parts">The parts of the summary.</param>
        /// <param name="count">The number of rows.</param>
        /// <param name="text">What the import does with the rows.</param>
        private static void AddSummaryPart(List<string> parts, int count, string text)
        {
            if (count > 0)
                parts.Add($"{count} {text}");
        }

        /// <summary>
        /// Gets a number followed by the singular or the plural of a word.
        /// </summary>
        /// <param name="count">The number.</param>
        /// <param name="singular">The word for 1.</param>
        /// <param name="plural">The word for the other numbers.</param>
        /// <returns>Returns the number and the word.</returns>
        private static string GetCountText(int count, string singular, string plural)
        {
            return $"{count} {(count == 1 ? singular : plural)}";
        }

        /// <summary>
        /// Gets the text that says what Undo reverts, and what can't be undone.
        /// </summary>
        /// <returns>Returns the text of the hint.</returns>
        private string GetUndoHintText()
        {
            bool hasMainRename = false;
            foreach (ImportRow row in _plan.Rows)
                hasMainRename |= row.IsRenamed && AssetDatabase.IsMainAsset(row.Asset);

            string text = UndoHintText;
            if (hasMainRename)
                text = $"{text} {MainRenameHintText}";

            if (_plan.CountRows(ImportRowStatus.Created) > 0)
                text = $"{text} {CreationHintText}";

            return text;
        }

        #endregion


        #region Actions

        /// <summary>
        /// Opens the folder dialog of the system, and uses the picked folder for the created assets. Refused with a message if the
        /// folder is not a folder of the project.
        /// </summary>
        private void ChooseFolder()
        {
            string projectPath = Path.GetDirectoryName(Application.dataPath);
            string absolutePath = EditorUtility.OpenFolderPanel(ChooseFolderTitle, Path.Combine(projectPath, _folder ?? string.Empty),
                string.Empty);

            // Empty if the dialog has been cancelled
            if (string.IsNullOrEmpty(absolutePath))
                return;

            if (!FolderScope.TryGetProjectFolder(absolutePath, projectPath, out string folder) || !AssetDatabase.IsValidFolder(folder))
            {
                EditorUtility.DisplayDialog(ChooseFolderTitle, NotProjectFolderMessage, DialogOkText);
                return;
            }

            _folder = folder;
            _folderField.SetValueWithoutNotify(folder);
            UpdatePlan();
        }

        /// <summary>
        /// Applies the import, closes the window, then notifies what the import has done.
        /// </summary>
        private void Apply()
        {
            if (_plan == null || !_plan.HasChanges)
                return;

            ImportResult result = _plan.Apply();
            Action<ImportResult> applied = _applied;
            _applied = null;
            Close();
            applied?.Invoke(result);
        }

        #endregion

    }

}
