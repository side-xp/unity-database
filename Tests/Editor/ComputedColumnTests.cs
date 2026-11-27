using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

using NUnit.Framework;

using UnityEngine;
using UnityEngine.TestTools;

using SideXP.Database.EditorOnly;

using Object = UnityEngine.Object;

namespace SideXP.Database.Tests
{

    /// <summary>
    /// Tests for the computed columns (<see cref="ComputedColumnAttribute"/>): discovery and signatures, the context of the table, and
    /// the compute stage of the pipeline.
    /// </summary>
    public class ComputedColumnTests
    {

        #region Subclasses

        /// <summary>
        /// Provides the assets of a list.
        /// </summary>
        private class ListAssetSource : IAssetSource
        {

            /// <summary>
            /// The assets to provide.
            /// </summary>
            public List<Object> Assets = new List<Object>();

            /// <inheritdoc/>
            public IReadOnlyList<Object> LoadAssets()
            {
                return new List<Object>(Assets);
            }

        }

        #endregion


        #region Fields

        private const BindingFlags MethodFlags = BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private const int NumberColumn = 0;
        private const int DoubleColumn = 1;
        private const int RankColumn = 2;
        private const int TotalColumn = 3;
        private const int ThrowsColumn = 4;

        /// <summary>
        /// The warnings reported by the tested operation.
        /// </summary>
        private List<string> _warnings = null;

        /// <summary>
        /// The valid computed columns of <see cref="ComputedColumnTestMethods"/>.
        /// </summary>
        private ComputedColumns _computedColumns = null;

        /// <summary>
        /// The assets of the tests, named A to D, with the numbers 3, 1, 3 and 2.
        /// </summary>
        private List<PipelineTestSO> _assets = null;

        /// <summary>
        /// The tested pipeline: the number, then the computed columns <i>Double</i>, <i>Get Rank</i>, <i>Get Total</i> and
        /// <i>Throws</i>.
        /// </summary>
        private TablePipeline _pipeline = null;

        #endregion


        #region Lifecycle

        /// <summary>
        /// Creates the assets, the computed columns and the pipeline, and resets the counters of the test methods.
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            _warnings = new List<string>();
            ComputedColumnTestMethods.DoubleCount = 0;
            ComputedColumnTestMethods.RanksBuildCount = 0;
            _computedColumns = new ComputedColumns(typeof(ComputedColumnTestMethods).GetMethods(MethodFlags));

            _assets = new List<PipelineTestSO>
            {
                CreateAsset("A", 3),
                CreateAsset("B", 1),
                CreateAsset("C", 3),
                CreateAsset("D", 2),
            };

            ListAssetSource source = new ListAssetSource();
            source.Assets.AddRange(_assets);

            FieldInfo numberField = typeof(PipelineTestSO).GetField(nameof(PipelineTestSO.number));
            List<IValueAccessor> accessors = new List<IValueAccessor> { new ReflectionAccessor(new[] { numberField }) };
            foreach (ColumnDefinition column in _computedColumns.GetColumns(typeof(PipelineTestSO), _warnings))
                accessors.Add(ValueAccessors.Create(column));

            _pipeline = new TablePipeline(source, accessors);
        }

        /// <summary>
        /// Destroys the assets.
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            foreach (PipelineTestSO asset in _assets)
                Object.DestroyImmediate(asset);
        }

        #endregion


        #region Discovery

        /// <summary>
        /// The valid methods are columns, in declaration order, with their declared name or the nicified method name, their tooltip,
        /// their width and the type of their values.
        /// </summary>
        [Test]
        public void GetColumns_ValidMethods_ColumnsInDeclarationOrder()
        {
            List<ColumnDefinition> columns = _computedColumns.GetColumns(typeof(PipelineTestSO), _warnings);

            string prefix = $"{ComputedColumns.IdPrefix}{typeof(ComputedColumnTestMethods).FullName}.";
            CollectionAssert.AreEqual(
                new[] { $"{prefix}GetDouble", $"{prefix}GetRank", $"{prefix}GetTotal", $"{prefix}Throw" },
                columns.Select(i => i.Id));
            CollectionAssert.AreEqual(new[] { "Double", "Get Rank", "Get Total", "Throws" }, columns.Select(i => i.Name));
            Assert.IsTrue(columns.All(i => i.IsComputed && i.PropertyPath == null && i.Fields == null));
            Assert.IsTrue(columns.All(i => i.Computed.ValueType == typeof(int)));
            Assert.AreEqual("The rank of the number.", columns[1].Tooltip);
            Assert.AreEqual(50f, columns[1].Width);
            Assert.IsNull(columns[0].Tooltip);
            Assert.IsTrue(columns[0].IsFlexible);
            Assert.IsEmpty(_warnings);
        }

        /// <summary>
        /// The methods with an invalid signature, and the overloads of a computed column, are skipped with a warning each, reported
        /// only the first time they are checked.
        /// </summary>
        [Test]
        public void GetColumns_InvalidMethods_SkippedWithWarningsOnce()
        {
            ComputedColumns computedColumns = new ComputedColumns(typeof(InvalidComputedColumnTestMethods).GetMethods(MethodFlags));

            List<ColumnDefinition> columns = computedColumns.GetColumns(typeof(PipelineTestSO), _warnings);
            computedColumns.GetColumns(typeof(DerivedPipelineTestSO), _warnings);

            Assert.AreEqual(1, columns.Count);
            // The methods of the test type are private, so nameof() can't name them
            Assert.AreEqual("Overload", columns[0].Computed.Method.Name);
            Assert.AreEqual(1, columns[0].Computed.Method.GetParameters().Length, "The first overload is kept.");
            // Not static, void, no parameter, wrong asset type, wrong context type, too many parameters, generic, by reference, and
            // the second overload
            Assert.AreEqual(9, _warnings.Count);
        }

        /// <summary>
        /// A computed column applies to the tables of its target type and of its derived types, not to the tables of its base types.
        /// </summary>
        [Test]
        public void GetColumns_TargetType_AppliesToDerivedTypesOnly()
        {
            Assert.AreEqual(4, _computedColumns.GetColumns(typeof(DerivedPipelineTestSO), _warnings).Count);
            Assert.IsEmpty(_computedColumns.GetColumns(typeof(ScriptableObject), _warnings));
            Assert.IsEmpty(_computedColumns.GetColumns(typeof(ColumnsSO), _warnings));
        }

        /// <summary>
        /// The validation against an asset keeps the computed columns, which have no serialized property.
        /// </summary>
        [Test]
        public void GetValidColumns_ComputedColumns_Kept()
        {
            List<ColumnDefinition> columns = _computedColumns.GetColumns(typeof(PipelineTestSO), _warnings);

            CollectionAssert.AreEqual(columns, ColumnValidation.GetValidColumns(columns, _assets[0], _warnings));
            Assert.IsEmpty(_warnings);
        }

        /// <summary>
        /// The accessor of a computed column can't read an asset alone.
        /// </summary>
        [Test]
        public void ComputedAccessor_Read_Throws()
        {
            ColumnDefinition column = _computedColumns.GetColumns(typeof(PipelineTestSO), _warnings)[0];
            IValueAccessor accessor = ValueAccessors.Create(column);

            Assert.IsInstanceOf<ComputedAccessor>(accessor);
            using (ValueReadContext context = new ValueReadContext(_assets[0]))
                Assert.Throws<NotSupportedException>(() => accessor.Read(context));
        }

        #endregion


        #region Context

        /// <summary>
        /// The shared data is built once per build method, and the context is emptied when the computation ends.
        /// </summary>
        [Test]
        public void TableContext_GetShared_BuiltOnceAndClearedAtEnd()
        {
            TableContext context = new TableContext(new List<Object>(_assets), new List<Object> { _assets[0] });
            int buildCount = 0;
            Func<ITableContext, int> build = c =>
            {
                buildCount++;
                return c.VisibleAssets.Count;
            };

            Assert.AreEqual(1, context.GetShared(build));
            Assert.AreEqual(1, context.GetShared(build));
            Assert.AreEqual(1, buildCount);
            Assert.Throws<ArgumentNullException>(() => context.GetShared<int>(null));

            context.End();
            Assert.IsEmpty(context.AllAssets);
            Assert.IsEmpty(context.VisibleAssets);
            Assert.AreEqual(0, context.GetShared(build));
            Assert.AreEqual(2, buildCount);
        }

        #endregion


        #region Pipeline

        /// <summary>
        /// The computed values are computed by the update, with the values of the rows.
        /// </summary>
        [Test]
        public void Update_ComputedColumn_ValuesComputed()
        {
            _pipeline.Update();

            CollectionAssert.AreEqual(new object[] { 6, 2, 6, 4 }, GetValues(DoubleColumn));
            Assert.AreEqual(4, ComputedColumnTestMethods.DoubleCount);
        }

        /// <summary>
        /// The shared data is built once for all the rows, and the ranks follow the visible rows.
        /// </summary>
        [Test]
        public void Update_SharedData_BuiltOncePerComputation()
        {
            _pipeline.Update();

            CollectionAssert.AreEqual(new object[] { 1, 4, 2, 3 }, GetValues(RankColumn));
            Assert.AreEqual(1, ComputedColumnTestMethods.RanksBuildCount);
        }

        /// <summary>
        /// The context has all the assets and the visible ones, and the rows hidden by the filter have no computed value.
        /// </summary>
        [Test]
        public void Update_Filter_ContextHasVisibleAssetsAndHiddenRowsHaveNoValue()
        {
            _pipeline.Filter = row => (int)row.GetValue(NumberColumn) >= 2;
            _pipeline.Update();

            CollectionAssert.AreEqual(new object[] { 304, null, 304, 304 }, GetValues(TotalColumn));
            CollectionAssert.AreEqual(new object[] { 1, null, 2, 3 }, GetValues(RankColumn));
        }

        /// <summary>
        /// The values are computed again when a row changes and when the filter changes, but not when the sort or the page changes.
        /// </summary>
        [Test]
        public void Update_Changes_ComputedAgainOnlyForDataAndFilter()
        {
            _pipeline.Update();
            ComputedColumnTestMethods.DoubleCount = 0;

            _pipeline.SetSort(NumberColumn, true, (a, b) => ((int)a).CompareTo((int)b));
            _pipeline.PageSize = 2;
            _pipeline.Update();
            Assert.AreEqual(0, ComputedColumnTestMethods.DoubleCount, "The sort and the page don't change the values.");

            _assets[1].number = 5;
            _pipeline.InvalidateRow(_assets[1]);
            _pipeline.Update();
            Assert.AreEqual(4, ComputedColumnTestMethods.DoubleCount, "A change of a row computes the values of all the rows.");
            Assert.AreEqual(10, _pipeline.Rows[1].GetValue(DoubleColumn));
            Assert.AreEqual(1, _pipeline.Rows[1].GetValue(RankColumn), "The ranks of the other rows follow.");

            _pipeline.Filter = row => (int)row.GetValue(NumberColumn) > 2;
            _pipeline.Update();
            Assert.AreEqual(4 + 3, ComputedColumnTestMethods.DoubleCount, "A change of the filter computes the visible rows again.");
        }

        /// <summary>
        /// The rows can be sorted by a computed column.
        /// </summary>
        [Test]
        public void Update_SortByComputedColumn_SortedByComputedValues()
        {
            _pipeline.SetSort(RankColumn, false, (a, b) => ((int)a).CompareTo((int)b));
            _pipeline.Update();

            CollectionAssert.AreEqual(new[] { "A", "C", "D", "B" }, _pipeline.VisibleRows.Select(i => i.Asset.name));
        }

        /// <summary>
        /// After an exception, the rows of the column have no value, the exception is logged, and the other columns are computed.
        /// </summary>
        [Test]
        public void Update_Exception_ColumnHasNoValueAndIsLoggedOnce()
        {
            LogAssert.Expect(LogType.Warning, new Regex(Regex.Escape(nameof(ComputedColumnTestMethods)) + ".*Throw\\(\\)"));
            _pipeline.Update();

            // The first row is computed before the row that throws
            CollectionAssert.AreEqual(new object[] { 3, null, null, null }, GetValues(ThrowsColumn));
            CollectionAssert.AreEqual(new object[] { 6, 2, 6, 4 }, GetValues(DoubleColumn));

            // Computed again without the throwing row: the column has values again
            _pipeline.Filter = row => (int)row.GetValue(NumberColumn) != ComputedColumnTestMethods.ThrowingNumber;
            _pipeline.Update();
            CollectionAssert.AreEqual(new object[] { 3, null, 3, 2 }, GetValues(ThrowsColumn));
        }

        #endregion


        #region Private API

        /// <summary>
        /// Creates an in-memory asset, destroyed after the test.
        /// </summary>
        /// <param name="name">The name of the asset.</param>
        /// <param name="number">The value of <see cref="PipelineTestSO.number"/>.</param>
        /// <returns>Returns the created asset.</returns>
        private static PipelineTestSO CreateAsset(string name, int number)
        {
            PipelineTestSO asset = ScriptableObject.CreateInstance<PipelineTestSO>();
            asset.name = name;
            asset.number = number;
            return asset;
        }

        /// <summary>
        /// Gets the cached values of a column, in the order of the source.
        /// </summary>
        /// <param name="columnIndex">The index of the column.</param>
        /// <returns>Returns the values of the rows.</returns>
        private List<object> GetValues(int columnIndex)
        {
            return _pipeline.Rows.Select(i => i.GetValue(columnIndex)).ToList();
        }

        #endregion

    }

}
