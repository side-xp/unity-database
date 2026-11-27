using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using NUnit.Framework;

using UnityEngine;
using UnityEditor;

using SideXP.Database.EditorOnly;

using Object = UnityEngine.Object;

namespace SideXP.Database.Tests
{

    /// <summary>
    /// Tests for the import of the tables: reading the file (<see cref="ImportFile"/>), mapping the columns
    /// (<see cref="ImportColumnMap"/>), matching the rows (<see cref="ImportRowMatcher"/>), the diff and its apply
    /// (<see cref="ImportPlan"/>), against real assets.
    /// </summary>
    /// <remarks>
    /// A temporary folder is created under <c>Assets/</c> in <see cref="SetUp"/> and removed in <see cref="TearDown"/>, with two
    /// <see cref="PipelineTestSO"/> assets and a sub-asset. <see cref="PipelineTestSO"/> is not a table, and has no other asset in the
    /// project, so these are all the assets of its type.
    /// </remarks>
    public class TableImportTests
    {

        #region Fields

        /// <summary>
        /// The name of the temporary folder.
        /// </summary>
        private const string TempFolderName = "TableImportTests_Temp";

        /// <summary>
        /// The path of the temporary folder.
        /// </summary>
        private const string TempFolder = "Assets/" + TempFolderName;

        /// <summary>
        /// The index of the <i>Asset Name</i> column in <see cref="_columns"/>.
        /// </summary>
        private const int AssetNameColumn = 0;

        /// <summary>
        /// The index of the column of <see cref="PipelineTestSO.number"/> in <see cref="_columns"/>, named <see cref="AmountName"/>, so
        /// its name differs from its id.
        /// </summary>
        private const int NumberColumn = 1;

        /// <summary>
        /// The index of the column of <see cref="PipelineTestSO.label"/> in <see cref="_columns"/>.
        /// </summary>
        private const int LabelColumn = 2;

        /// <summary>
        /// The index of the computed column <i>Get Total</i> in <see cref="_columns"/>.
        /// </summary>
        private const int TotalColumn = 3;

        /// <summary>
        /// The name of the column of <see cref="PipelineTestSO.number"/>.
        /// </summary>
        private const string AmountName = "Amount";

        /// <summary>
        /// The name of the type of the tests, as it names the created assets.
        /// </summary>
        private const string TypeDisplayName = "Pipeline Test SO";

        /// <summary>
        /// A main asset, named <c>Alpha</c>, with the number 3 and a label with a comma.
        /// </summary>
        private PipelineTestSO _alpha = null;

        /// <summary>
        /// A main asset, named <c>Beta</c>, with the number 1, which contains <see cref="_gamma"/>.
        /// </summary>
        private PipelineTestSO _beta = null;

        /// <summary>
        /// A sub-asset of <see cref="_beta"/>, named <c>Gamma</c>, with the number 2.
        /// </summary>
        private PipelineTestSO _gamma = null;

        /// <summary>
        /// The columns of the tests: <i>Asset Name</i>, <see cref="AmountName"/>, the label, the computed column <i>Get Total</i>,
        /// then <i>Controls</i>.
        /// </summary>
        private List<TableViewColumn> _columns = null;

        #endregion


        #region Lifecycle

        /// <summary>
        /// Creates the temporary folder, the assets and the columns of the tests.
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            if (AssetDatabase.IsValidFolder(TempFolder))
                AssetDatabase.DeleteAsset(TempFolder);

            AssetDatabase.CreateFolder("Assets", TempFolderName);

            _alpha = CreateAsset("Alpha", 3, "Hello, world");
            _beta = CreateAsset("Beta", 1, "say \"hi\"");

            _gamma = ScriptableObject.CreateInstance<PipelineTestSO>();
            _gamma.name = "Gamma";
            _gamma.number = 2;
            _gamma.label = "two\nlines";
            AssetDatabase.AddObjectToAsset(_gamma, _beta);
            AssetDatabase.SaveAssets();

            ComputedColumns computedColumns = new ComputedColumns(typeof(ComputedColumnTestMethods).GetMethods(BindingFlags.Static |
                BindingFlags.NonPublic));
            ColumnDefinition totalColumn = computedColumns.GetColumns(typeof(PipelineTestSO), new List<string>())
                .First(column => column.Name == "Get Total");

            _columns = new List<TableViewColumn>
            {
                TableViewColumn.CreateAssetNameColumn(),
                CreateFieldColumn(nameof(PipelineTestSO.number), AmountName),
                CreateFieldColumn(nameof(PipelineTestSO.label), "Label"),
                TableViewColumn.FromDefinition(totalColumn),
                TableViewColumn.CreateControlsColumn(),
            };
        }

        /// <summary>
        /// Deletes the temporary folder and its assets.
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            if (AssetDatabase.IsValidFolder(TempFolder))
                AssetDatabase.DeleteAsset(TempFolder);
        }

        #endregion


        #region File

        /// <summary>
        /// The first row is the header, the blank rows are skipped, and the data rows keep their number in the file.
        /// </summary>
        [Test]
        public void Parse_Text_HeaderAndRowsWithoutBlankRows()
        {
            ImportFile file = ImportFile.Parse("GUID,Amount\r\na,1\r\n , \r\n\r\nb,2\r\n", DelimitedFormat.Csv);

            CollectionAssert.AreEqual(new[] { "GUID", "Amount" }, file.Header);
            Assert.AreEqual(2, file.RowCount);
            Assert.AreEqual("b", file.GetText(1, 0));
            Assert.AreEqual(2, file.GetRowNumber(0));
            Assert.AreEqual(5, file.GetRowNumber(1));
        }

        /// <summary>
        /// A value missing at the end of a row is an empty text.
        /// </summary>
        [Test]
        public void GetText_MissingValue_Empty()
        {
            ImportFile file = ImportFile.Parse("A,B\r\nx\r\n", DelimitedFormat.Csv);

            Assert.AreEqual(string.Empty, file.GetText(0, 1));
        }

        /// <summary>
        /// A CSV file uses semicolons when its header has more of them than commas, outside of the quoted values. A TSV file always
        /// uses tabs.
        /// </summary>
        [Test]
        public void DetectDelimiter_Texts_CommaSemicolonOrTab()
        {
            Assert.AreEqual(ImportFile.Semicolon, ImportFile.DetectDelimiter("Name;Price\r\nSword;12,5", DelimitedFormat.Csv));
            Assert.AreEqual(DelimitedText.Comma, ImportFile.DetectDelimiter("Name,Price;Rank", DelimitedFormat.Csv));
            Assert.AreEqual(DelimitedText.Comma, ImportFile.DetectDelimiter("\"a;b;c\",d", DelimitedFormat.Csv));
            Assert.AreEqual(DelimitedText.Tab, ImportFile.DetectDelimiter("a;b;c", DelimitedFormat.Tsv));
        }

        /// <summary>
        /// The format comes from the extension, ignoring case, and other extensions are refused.
        /// </summary>
        [Test]
        public void TryGetFormat_Extensions_CsvTsvOrRefused()
        {
            Assert.IsTrue(ImportFile.TryGetFormat("Items.CSV", out DelimitedFormat format));
            Assert.AreEqual(DelimitedFormat.Csv, format);
            Assert.IsTrue(ImportFile.TryGetFormat("C:/Exports/Items.tsv", out format));
            Assert.AreEqual(DelimitedFormat.Tsv, format);
            Assert.IsFalse(ImportFile.TryGetFormat("Items.txt", out _));
            Assert.IsFalse(ImportFile.TryRead("Items.txt", out ImportFile file, out string error));
            Assert.IsNull(file);
            Assert.IsNotNull(error);
        }

        #endregion


        #region Columns

        /// <summary>
        /// The header of an export maps all its columns: the GUID column is the identity column, the computed column is read-only,
        /// and no column is unknown.
        /// </summary>
        [Test]
        public void Map_ExportedHeader_AllMapped()
        {
            ImportColumnMap map = new ImportColumnMap(new[] { "GUID", "Asset Name", AmountName, "Label", "Get Total" }, _columns);

            Assert.AreEqual(0, map.IdentityColumn);
            Assert.AreEqual(ImportColumnMap.Ignored, map.GetTarget(0));
            CollectionAssert.AreEqual(new[] { AssetNameColumn, NumberColumn, LabelColumn, TotalColumn },
                Enumerable.Range(1, 4).Select(map.GetTarget));
            Assert.IsTrue(map.IsReadOnly(4));
            Assert.IsFalse(map.IsReadOnly(2));
            Assert.IsFalse(Enumerable.Range(0, 5).Any(map.IsUnknown));
        }

        /// <summary>
        /// Headers match the names ignoring case, then the ids. A column of the table receives one column of the file at most, and
        /// the headers that match nothing, empty headers included, are unknown.
        /// </summary>
        [Test]
        public void Map_HandWrittenHeader_NamesThenIdsOthersUnknown()
        {
            ImportColumnMap map = new ImportColumnMap(new[] { "label", "number", "Label", "Weight", "", "Controls" }, _columns);

            Assert.AreEqual(-1, map.IdentityColumn);
            Assert.AreEqual(LabelColumn, map.GetTarget(0));
            Assert.AreEqual(NumberColumn, map.GetTarget(1), "Matched by id.");
            CollectionAssert.AreEqual(new[] { true, true, true, true }, Enumerable.Range(2, 4).Select(map.IsUnknown));
            Assert.AreEqual(-1, map.FindFileColumn(TotalColumn));
        }

        /// <summary>
        /// An unknown column can be mapped by hand to a column that can be written and receives no other column, and stays unknown.
        /// The identity column, the read-only columns and the columns already mapped can't be targets.
        /// </summary>
        [Test]
        public void SetTarget_UnknownColumn_MappedToAvailableColumns()
        {
            ImportColumnMap map = new ImportColumnMap(new[] { "GUID", "Label", "Weight" }, _columns);

            CollectionAssert.AreEqual(new[] { AssetNameColumn, NumberColumn }, map.GetAvailableTargets(2));
            Assert.IsFalse(map.SetTarget(2, LabelColumn), "Already mapped.");
            Assert.IsFalse(map.SetTarget(2, TotalColumn), "Read-only.");
            Assert.IsFalse(map.SetTarget(0, NumberColumn), "Identity column.");
            Assert.IsTrue(map.SetTarget(2, NumberColumn));
            Assert.AreEqual(NumberColumn, map.GetTarget(2));
            Assert.IsTrue(map.IsUnknown(2));
            Assert.IsTrue(map.SetTarget(2, ImportColumnMap.Ignored));
            Assert.AreEqual(ImportColumnMap.Ignored, map.GetTarget(2));
        }

        #endregion


        #region Rows

        /// <summary>
        /// The identity column matches the identifiers and the paths (with the name of a sub-asset after a <c>#</c>, back slashes
        /// accepted), then the <i>Asset Name</i> matches the name, ignoring case, also when the identity matches nothing.
        /// </summary>
        [Test]
        public void Match_IdentitiesAndNames_Matched()
        {
            ImportRowMatcher matcher = new ImportRowMatcher(new Object[] { _alpha, _beta, _gamma });
            string betaPath = AssetDatabase.GetAssetPath(_beta);

            AssertMatch(matcher, AssetIds.GetId(_alpha), null, _alpha, ImportMatchKind.Id);
            AssertMatch(matcher, AssetIds.GetId(_gamma), null, _gamma, ImportMatchKind.Id);
            AssertMatch(matcher, $" {betaPath} ", null, _beta, ImportMatchKind.Path);
            AssertMatch(matcher, betaPath.Replace('/', '\\'), null, _beta, ImportMatchKind.Path);
            AssertMatch(matcher, $"{betaPath}#Gamma", null, _gamma, ImportMatchKind.Path);
            AssertMatch(matcher, null, "alpha", _alpha, ImportMatchKind.Name);
            AssertMatch(matcher, "0123456789abcdef0123456789abcdef", "Beta", _beta, ImportMatchKind.Name);
            AssertMatch(matcher, "unknown", "Unknown", null, ImportMatchKind.None);
        }

        /// <summary>
        /// A name shared by several assets matches none of them, and makes the row ambiguous.
        /// </summary>
        [Test]
        public void Match_SharedName_Ambiguous()
        {
            PipelineTestSO otherGamma = CreateAsset("Gamma", 0, string.Empty);
            ImportRowMatcher matcher = new ImportRowMatcher(new Object[] { _alpha, _beta, _gamma, otherGamma });

            Assert.AreEqual(ImportMatchKind.None, matcher.Match(null, "Gamma", out Object asset, out bool isAmbiguous));
            Assert.IsNull(asset);
            Assert.IsTrue(isAmbiguous);
            Assert.AreEqual(ImportMatchKind.Id, matcher.Match(AssetIds.GetId(otherGamma), "Gamma", out asset, out isAmbiguous));
            Assert.AreEqual(otherGamma, asset);
            Assert.IsFalse(isAmbiguous);
        }

        #endregion


        #region Plan

        /// <summary>
        /// An edited export changes only the edited cells, compared as the cells display them, and the rows without change are
        /// unchanged.
        /// </summary>
        [Test]
        public void Build_EditedExport_OnlyEditedCellsChange()
        {
            ImportPlan plan = BuildPlan(null,
                new[] { "GUID", "Asset Name", AmountName, "Label" },
                new[] { AssetIds.GetId(_alpha), "Alpha", "5", "Hello, world" },
                new[] { AssetIds.GetId(_beta), "Beta", " 1 ", "say \"hi\"" });

            Assert.AreEqual(ImportRowStatus.Changed, plan.Rows[0].Status);
            Assert.AreEqual(ImportMatchKind.Id, plan.Rows[0].MatchKind);
            Assert.AreEqual(1, plan.Rows[0].Changes.Count);
            Assert.AreEqual(NumberColumn, plan.Rows[0].Changes[0].ColumnIndex);
            Assert.AreEqual("3", plan.Rows[0].Changes[0].OldText);
            Assert.AreEqual("5", plan.Rows[0].Changes[0].NewText);
            Assert.AreEqual(5, plan.Rows[0].Changes[0].Value);
            Assert.AreEqual(ImportRowStatus.Unchanged, plan.Rows[1].Status);
            Assert.AreEqual(1, plan.ChangedCellCount);
            Assert.IsTrue(plan.HasChanges);
        }

        /// <summary>
        /// A different <i>Asset Name</i> renames the matched asset, and a blank one keeps its name.
        /// </summary>
        [Test]
        public void Build_DifferentName_Renamed()
        {
            ImportPlan plan = BuildPlan(null,
                new[] { "GUID", "Asset Name" },
                new[] { AssetIds.GetId(_alpha), " Omega " },
                new[] { AssetIds.GetId(_beta), "" });

            Assert.IsTrue(plan.Rows[0].IsRenamed);
            Assert.AreEqual("Omega", plan.Rows[0].NewName);
            Assert.AreEqual(ImportRowStatus.Changed, plan.Rows[0].Status);
            Assert.IsFalse(plan.Rows[1].IsRenamed);
            Assert.AreEqual(ImportRowStatus.Unchanged, plan.Rows[1].Status);
            Assert.AreEqual(1, plan.CountRenames());
        }

        /// <summary>
        /// A text that can't be converted is a rejected cell, a blank one is ignored, and the values of a read-only column are ignored.
        /// </summary>
        [Test]
        public void Build_InvalidAndReadOnlyValues_RejectedOrIgnored()
        {
            ImportPlan plan = BuildPlan(null,
                new[] { "GUID", AmountName, "Get Total" },
                new[] { AssetIds.GetId(_alpha), "abc", "999" },
                new[] { AssetIds.GetId(_beta), " ", "999" });

            Assert.AreEqual(1, plan.Rows[0].RejectedCells.Count);
            Assert.AreEqual(NumberColumn, plan.Rows[0].RejectedCells[0].ColumnIndex);
            Assert.AreEqual("abc", plan.Rows[0].RejectedCells[0].Text);
            Assert.AreEqual(ImportRowStatus.Unchanged, plan.Rows[0].Status);
            Assert.IsEmpty(plan.Rows[1].RejectedCells);
            Assert.AreEqual(ImportRowStatus.Unchanged, plan.Rows[1].Status);
            Assert.AreEqual(1, plan.RejectedCellCount);
            Assert.IsFalse(plan.HasChanges);
        }

        /// <summary>
        /// A row that matches the asset of an earlier row is a duplicate, and a name shared by several assets is ambiguous.
        /// </summary>
        [Test]
        public void Build_SameAssetTwiceAndSharedName_Skipped()
        {
            CreateAsset("Gamma", 0, string.Empty);
            ImportPlan plan = BuildPlan(null,
                new[] { "Asset Name", AmountName },
                new[] { "Alpha", "4" },
                new[] { "alpha", "5" },
                new[] { "Gamma", "6" });

            Assert.AreEqual(ImportRowStatus.Changed, plan.Rows[0].Status);
            Assert.AreEqual(ImportRowStatus.Duplicate, plan.Rows[1].Status);
            Assert.AreEqual(ImportRowStatus.Ambiguous, plan.Rows[2].Status);
        }

        /// <summary>
        /// The rows that match no asset are missing by default. With the option, they create assets named after their <i>Asset
        /// Name</i>, or after the type, with a number when the name is taken, and all their converted values are changes.
        /// </summary>
        [Test]
        public void Build_MissingRows_MissingOrCreated()
        {
            string[][] rows =
            {
                new[] { "Asset Name", AmountName, "Label" },
                new[] { "Delta", "7", "new" },
                new[] { "", "8", "" },
                new[] { "", "9", "" },
                new[] { "Delta", "10", "" },
            };

            ImportPlan missingPlan = BuildPlan(null, rows);
            Assert.AreEqual(4, missingPlan.CountRows(ImportRowStatus.Missing));
            Assert.IsFalse(missingPlan.HasChanges);

            ImportPlan createdPlan = BuildPlan(CreateOptions(), rows);
            Assert.AreEqual(4, createdPlan.CountRows(ImportRowStatus.Created));
            CollectionAssert.AreEqual(new[] { "Delta", TypeDisplayName, $"{TypeDisplayName} 2", "Delta 2" },
                createdPlan.Rows.Select(row => row.NewName));
            Assert.AreEqual(2, createdPlan.Rows[0].Changes.Count);
            Assert.IsNull(createdPlan.Rows[0].Changes[0].OldText);
            Assert.AreEqual(7, createdPlan.Rows[0].Changes[0].Value);
            Assert.AreEqual(0, createdPlan.ChangedCellCount, "The values of the created assets are not changes of existing cells.");
        }

        /// <summary>
        /// The name of a created asset gets a number when a file of the folder has it.
        /// </summary>
        [Test]
        public void Build_NameOfExistingFile_Numbered()
        {
            // Left out of the assets of the table, like an asset of another type, so the row doesn't match it
            CreateAsset("Delta", 0, string.Empty);
            ImportPlan plan = BuildPlanForAssets(CreateOptions(), new Object[] { _alpha, _beta, _gamma },
                new[] { "Asset Name" },
                new[] { "Delta" });

            Assert.AreEqual("Delta 2", plan.Rows[0].NewName);
        }

        #endregion


        #region Apply

        /// <summary>
        /// The values and the renames of sub-assets are written as a single Undo step.
        /// </summary>
        [Test]
        public void Apply_ValuesAndSubAssetRename_SingleUndoStep()
        {
            ImportPlan plan = BuildPlan(null,
                new[] { "GUID", "Asset Name", AmountName, "Label" },
                new[] { AssetIds.GetId(_alpha), "Alpha", "5", "Hello, world" },
                new[] { AssetIds.GetId(_beta), "Beta", "1", "changed" },
                new[] { AssetIds.GetId(_gamma), "Gamma Renamed", "2", "two\nlines" });

            ImportResult result = plan.Apply();

            Assert.IsEmpty(result.Errors);
            Assert.AreEqual(2, result.ChangedCellCount);
            Assert.AreEqual(1, result.RenamedCount);
            Assert.AreEqual(5, _alpha.number);
            Assert.AreEqual("changed", _beta.label);
            Assert.AreEqual("Gamma Renamed", _gamma.name);

            Undo.PerformUndo();
            Assert.AreEqual(3, _alpha.number);
            Assert.AreEqual("say \"hi\"", _beta.label);
            Assert.AreEqual("Gamma", _gamma.name);
        }

        /// <summary>
        /// The rename of a main asset renames its file.
        /// </summary>
        [Test]
        public void Apply_MainAssetRename_FileRenamed()
        {
            ImportPlan plan = BuildPlan(null,
                new[] { "GUID", "Asset Name" },
                new[] { AssetIds.GetId(_alpha), "Omega" });

            ImportResult result = plan.Apply();

            Assert.IsEmpty(result.Errors);
            Assert.AreEqual(1, result.RenamedCount);
            Assert.AreEqual($"{TempFolder}/Omega.asset", AssetDatabase.GetAssetPath(_alpha));
        }

        /// <summary>
        /// The missing assets are created in the folder of the options, with their values.
        /// </summary>
        [Test]
        public void Apply_MissingAssets_CreatedWithValues()
        {
            ImportPlan plan = BuildPlan(CreateOptions(),
                new[] { "Asset Name", AmountName, "Label" },
                new[] { "Delta", "7", "new" });

            ImportResult result = plan.Apply();

            Assert.IsEmpty(result.Errors);
            Assert.AreEqual(1, result.CreatedAssets.Count);
            PipelineTestSO created = (PipelineTestSO)result.CreatedAssets[0];
            Assert.AreEqual($"{TempFolder}/Delta.asset", AssetDatabase.GetAssetPath(created));
            Assert.AreEqual(7, created.number);
            Assert.AreEqual("new", created.label);
        }

        #endregion


        #region Private API

        /// <summary>
        /// Creates a main asset in the temporary folder.
        /// </summary>
        /// <param name="name">The name of the asset, and of its file.</param>
        /// <param name="number">The value of <see cref="PipelineTestSO.number"/>.</param>
        /// <param name="label">The value of <see cref="PipelineTestSO.label"/>.</param>
        /// <returns>Returns the created asset.</returns>
        private static PipelineTestSO CreateAsset(string name, int number, string label)
        {
            PipelineTestSO asset = ScriptableObject.CreateInstance<PipelineTestSO>();
            asset.number = number;
            asset.label = label;
            AssetDatabase.CreateAsset(asset, $"{TempFolder}/{name}.asset");
            return asset;
        }

        /// <summary>
        /// Creates the column of a public field of <see cref="PipelineTestSO"/>.
        /// </summary>
        /// <param name="fieldName">The name of the field, which is the id of the column.</param>
        /// <param name="columnName">The name of the column.</param>
        /// <returns>Returns the created column.</returns>
        private static TableViewColumn CreateFieldColumn(string fieldName, string columnName)
        {
            FieldInfo field = typeof(PipelineTestSO).GetField(fieldName);
            return new TableViewColumn(fieldName, columnName, null, 0, new ReflectionAccessor(new[] { field }), field, fieldName);
        }

        /// <summary>
        /// Creates the options that create the missing assets in the temporary folder.
        /// </summary>
        /// <returns>Returns the created options.</returns>
        private static ImportOptions CreateOptions()
        {
            return new ImportOptions(true, typeof(PipelineTestSO), TempFolder);
        }

        /// <summary>
        /// Builds the plan of the import of a CSV file into all the assets of <see cref="PipelineTestSO"/>.
        /// </summary>
        /// <param name="options">The options of the import, or null to leave the missing assets missing.</param>
        /// <param name="rows">The rows of the file, the header first.</param>
        /// <returns>Returns the built plan.</returns>
        private ImportPlan BuildPlan(ImportOptions options, params string[][] rows)
        {
            IReadOnlyList<TableRow> tableRows = TableExport.LoadAllRows(typeof(PipelineTestSO),
                _columns.ConvertAll(column => column.Accessor));
            return BuildPlanForRows(options, tableRows, rows);
        }

        /// <summary>
        /// Builds the plan of the import of a CSV file into some assets.
        /// </summary>
        /// <param name="options">The options of the import, or null to leave the missing assets missing.</param>
        /// <param name="assets">The assets of the table.</param>
        /// <param name="rows">The rows of the file, the header first.</param>
        /// <returns>Returns the built plan.</returns>
        private ImportPlan BuildPlanForAssets(ImportOptions options, IEnumerable<Object> assets, params string[][] rows)
        {
            // A single update, so the fallback source of the preloaded assets is never used
            TablePipeline pipeline = new TablePipeline(new PreloadedAssetSource(assets.ToList(), null),
                _columns.ConvertAll(column => column.Accessor));
            pipeline.Update();
            return BuildPlanForRows(options, pipeline.Rows, rows);
        }

        /// <summary>
        /// Builds the plan of the import of a CSV file into the rows of a table.
        /// </summary>
        /// <param name="options">The options of the import, or null to leave the missing assets missing.</param>
        /// <param name="tableRows">The rows of the table.</param>
        /// <param name="rows">The rows of the file, the header first.</param>
        /// <returns>Returns the built plan.</returns>
        private ImportPlan BuildPlanForRows(ImportOptions options, IReadOnlyList<TableRow> tableRows, string[][] rows)
        {
            string text = TableExport.Write(rows, DelimitedFormat.Csv);
            ImportFile file = ImportFile.Parse(text, DelimitedFormat.Csv, "Test.csv");
            ImportColumnMap map = new ImportColumnMap(file.Header, _columns);
            options = options ?? new ImportOptions(false, typeof(PipelineTestSO), TempFolder);
            return ImportPlan.Build(file, map, _columns, tableRows, options);
        }

        /// <summary>
        /// Checks the match of a row.
        /// </summary>
        /// <param name="matcher">The tested matcher.</param>
        /// <param name="identityText">The text of the identity column.</param>
        /// <param name="nameText">The text of the <i>Asset Name</i> column.</param>
        /// <param name="expectedAsset">The expected asset, or null.</param>
        /// <param name="expectedKind">The expected kind of match.</param>
        private static void AssertMatch(ImportRowMatcher matcher, string identityText, string nameText, Object expectedAsset,
            ImportMatchKind expectedKind)
        {
            ImportMatchKind kind = matcher.Match(identityText, nameText, out Object asset, out bool isAmbiguous);
            string message = $"Identity \"{identityText}\", name \"{nameText}\"";
            Assert.AreEqual(expectedKind, kind, message);
            Assert.AreEqual(expectedAsset, asset, message);
            Assert.IsFalse(isAmbiguous, message);
        }

        #endregion

    }

}
