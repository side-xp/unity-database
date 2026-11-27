using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

using NUnit.Framework;

using UnityEngine;
using UnityEditor;

using SideXP.Database.EditorOnly;

using Object = UnityEngine.Object;

namespace SideXP.Database.Tests
{

    /// <summary>
    /// Tests for the export of the tables (<see cref="TableExport"/> and <see cref="AssetIds"/>), against real assets.
    /// </summary>
    /// <remarks>
    /// A temporary folder is created under <c>Assets/</c> in <see cref="SetUp"/> and removed in <see cref="TearDown"/>, with two
    /// <see cref="PipelineTestSO"/> assets and a sub-asset. <see cref="PipelineTestSO"/> is not a table, and has no other asset in the
    /// project, so these are all the assets of its type.
    /// </remarks>
    public class TableExportTests
    {

        #region Fields

        /// <summary>
        /// The name of the temporary folder.
        /// </summary>
        private const string TempFolderName = "TableExportTests_Temp";

        /// <summary>
        /// The path of the temporary folder.
        /// </summary>
        private const string TempFolder = "Assets/" + TempFolderName;

        /// <summary>
        /// The index of the <i>Asset Name</i> column in <see cref="_columns"/>.
        /// </summary>
        private const int AssetNameColumn = 0;

        /// <summary>
        /// The index of the column of <see cref="PipelineTestSO.number"/> in <see cref="_columns"/>.
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
        /// The value of the computed column <i>Get Total</i> (see <see cref="ComputedColumnTestMethods"/>) when its context has the
        /// three assets of the tests, all of them visible.
        /// </summary>
        private const int TotalOfAllAssets = 303;

        /// <summary>
        /// A main asset, named <c>Alpha</c>, whose label contains a comma.
        /// </summary>
        private PipelineTestSO _alpha = null;

        /// <summary>
        /// A main asset, named <c>Beta</c>, which contains <see cref="_gamma"/>.
        /// </summary>
        private PipelineTestSO _beta = null;

        /// <summary>
        /// A sub-asset of <see cref="_beta"/>, named <c>Gamma</c>.
        /// </summary>
        private PipelineTestSO _gamma = null;

        /// <summary>
        /// The columns of the tests: <i>Asset Name</i>, the number, the label, the computed column <i>Get Total</i>, then
        /// <i>Controls</i>.
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
                CreateFieldColumn(nameof(PipelineTestSO.number), "Number"),
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


        #region Asset identifiers

        /// <summary>
        /// A main asset is identified by the GUID of its file.
        /// </summary>
        [Test]
        public void GetId_MainAsset_GuidOfFile()
        {
            Assert.AreEqual(AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(_alpha)), AssetIds.GetId(_alpha));
        }

        /// <summary>
        /// A sub-asset is identified by the GUID of its file and its local file identifier, so it doesn't get the identifier of its
        /// main asset.
        /// </summary>
        [Test]
        public void GetId_SubAsset_GuidAndLocalId()
        {
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(_gamma, out string guid, out long localId);

            Assert.AreEqual($"{guid}{AssetIds.LocalIdSeparator}{localId}", AssetIds.GetId(_gamma));
            Assert.AreEqual(guid, AssetIds.GetId(_beta));
        }

        /// <summary>
        /// An object that is not saved in an asset file, and a null object, have no identifier.
        /// </summary>
        [Test]
        public void GetId_NotAnAsset_Empty()
        {
            PipelineTestSO inMemory = ScriptableObject.CreateInstance<PipelineTestSO>();
            try
            {
                Assert.AreEqual(string.Empty, AssetIds.GetId(inMemory));
                Assert.AreEqual(string.Empty, AssetIds.GetId(null));
            }
            finally
            {
                Object.DestroyImmediate(inMemory);
            }
        }

        #endregion


        #region Rows

        /// <summary>
        /// All the assets of the table type in the project are loaded, by path with the sub-assets after their main asset, and the
        /// values of the computed columns are computed for all of them, with all the assets visible.
        /// </summary>
        [Test]
        public void LoadAllRows_TableType_AllAssetsWithComputedValues()
        {
            IReadOnlyList<TableRow> rows = LoadAllRows();

            CollectionAssert.AreEqual(new Object[] { _alpha, _beta, _gamma }, rows.Select(row => row.Asset));
            Assert.AreEqual(_alpha.number, rows[0].GetValue(NumberColumn));
            CollectionAssert.AreEqual(new[] { TotalOfAllAssets, TotalOfAllAssets, TotalOfAllAssets },
                rows.Select(row => row.GetValue(TotalColumn)));
        }

        /// <summary>
        /// The header row is the GUID column, then the names of the columns. Each next row is the identifier of an asset, then the
        /// text of its values. The <i>Controls</i> column is skipped.
        /// </summary>
        [Test]
        public void GetRows_AllColumns_GuidThenColumnsWithoutControls()
        {
            List<int> allColumns = Enumerable.Range(0, _columns.Count).ToList();
            List<List<string>> texts = TableExport.GetRows(_columns, allColumns, LoadAllRows());
            string totalText = TotalOfAllAssets.ToString();

            Assert.AreEqual(4, texts.Count, "The header row, then a row per asset.");
            CollectionAssert.AreEqual(new[] { TableExport.GuidColumnName, "Asset Name", "Number", "Label", "Get Total" }, texts[0]);
            CollectionAssert.AreEqual(new[] { AssetIds.GetId(_alpha), "Alpha", "3", "Hello, world", totalText }, texts[1]);
            CollectionAssert.AreEqual(new[] { AssetIds.GetId(_gamma), "Gamma", "2", "two\nlines", totalText }, texts[3]);
        }

        /// <summary>
        /// Only the given columns are exported, in the given order, after the GUID column.
        /// </summary>
        [Test]
        public void GetRows_SomeColumns_OrderOfTheIndices()
        {
            List<List<string>> texts = TableExport.GetRows(_columns, new[] { LabelColumn, AssetNameColumn }, LoadAllRows());

            CollectionAssert.AreEqual(new[] { TableExport.GuidColumnName, "Label", "Asset Name" }, texts[0]);
            CollectionAssert.AreEqual(new[] { AssetIds.GetId(_beta), "say \"hi\"", "Beta" }, texts[2]);
        }

        #endregion


        #region Writing

        /// <summary>
        /// A CSV file separates the values with commas, quotes the values that contain a comma, a quote or a line break, and ends each
        /// row with <c>\r\n</c>, the last one included.
        /// </summary>
        [Test]
        public void Write_Csv_QuotedValuesAndLineBreakAfterEachRow()
        {
            List<List<string>> rows = new List<List<string>>
            {
                new List<string> { "GUID", "Label" },
                new List<string> { "a", "Hello, world" },
                new List<string> { "b", "say \"hi\"" },
            };

            Assert.AreEqual("GUID,Label\r\na,\"Hello, world\"\r\nb,\"say \"\"hi\"\"\"\r\n", TableExport.Write(rows, DelimitedFormat.Csv));
        }

        /// <summary>
        /// A TSV file separates the values with tabs, so a comma doesn't quote a value, and ends each row with <c>\r\n</c>.
        /// </summary>
        [Test]
        public void Write_Tsv_TabsAndLineBreakAfterEachRow()
        {
            List<List<string>> rows = new List<List<string>>
            {
                new List<string> { "GUID", "Label" },
                new List<string> { "a", "Hello, world" },
                new List<string> { "b", "two\nlines" },
            };

            Assert.AreEqual("GUID\tLabel\r\na\tHello, world\r\nb\t\"two\nlines\"\r\n", TableExport.Write(rows, DelimitedFormat.Tsv));
        }

        /// <summary>
        /// The written text of the exported rows parses back into the same rows.
        /// </summary>
        [Test]
        public void Write_ExportedRows_ParsedBackToSameRows()
        {
            List<List<string>> texts = TableExport.GetRows(_columns, Enumerable.Range(0, _columns.Count).ToList(), LoadAllRows());

            foreach (DelimitedFormat format in new[] { DelimitedFormat.Csv, DelimitedFormat.Tsv })
            {
                List<List<string>> parsed = DelimitedText.Parse(TableExport.Write(texts, format), TableExport.GetDelimiter(format));

                Assert.AreEqual(texts.Count, parsed.Count, format.ToString());
                for (int i = 0; i < texts.Count; i++)
                    CollectionAssert.AreEqual(texts[i], parsed[i], $"{format}, row {i}");
            }
        }

        /// <summary>
        /// The file is written in UTF-8 with a byte order mark, so spreadsheet apps read its accents.
        /// </summary>
        [Test]
        public void WriteFile_Text_Utf8WithByteOrderMark()
        {
            // Outside of the project, so Unity doesn't import it
            string path = Path.Combine(Path.GetTempPath(), $"{TempFolderName}.csv");
            const string Text = "GUID,Label\r\na,Épée\r\n";
            try
            {
                TableExport.WriteFile(path, Text);
                byte[] bytes = File.ReadAllBytes(path);
                byte[] byteOrderMark = Encoding.UTF8.GetPreamble();

                CollectionAssert.AreEqual(byteOrderMark, bytes.Take(byteOrderMark.Length));
                Assert.AreEqual(Text, Encoding.UTF8.GetString(bytes, byteOrderMark.Length, bytes.Length - byteOrderMark.Length));
            }
            finally
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
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
        /// <param name="fieldName">The name of the field.</param>
        /// <param name="columnName">The name of the column.</param>
        /// <returns>Returns the created column.</returns>
        private static TableViewColumn CreateFieldColumn(string fieldName, string columnName)
        {
            FieldInfo field = typeof(PipelineTestSO).GetField(fieldName);
            return new TableViewColumn(fieldName, columnName, null, 0, new ReflectionAccessor(new[] { field }), field, fieldName);
        }

        /// <summary>
        /// Loads all the rows of <see cref="PipelineTestSO"/>, with the accessors of <see cref="_columns"/>.
        /// </summary>
        /// <returns>Returns the loaded rows.</returns>
        private IReadOnlyList<TableRow> LoadAllRows()
        {
            return TableExport.LoadAllRows(typeof(PipelineTestSO), _columns.ConvertAll(column => column.Accessor));
        }

        #endregion

    }

}
