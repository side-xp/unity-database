using System;
using System.Collections.Generic;
using System.Linq;

using NUnit.Framework;

using UnityEngine;

using SideXP.Database.EditorOnly;

using Object = UnityEngine.Object;

namespace SideXP.Database.Tests
{

    /// <summary>
    /// Tests for the filters of the tables (<see cref="FilterNode"/>): each kind of condition, groups, conditions that don't apply to
    /// the table, and serialization.
    /// </summary>
    public class FilterTests
    {

        #region Subclasses

        /// <summary>
        /// An enum with flags, to test the conditions on flags.
        /// </summary>
        [Flags]
        private enum TestFlags
        {
            None = 0,
            Fire = 1,
            Ice = 2,
            Poison = 4,
        }

        /// <summary>
        /// An enum based on <see cref="ulong"/>, with a value that doesn't fit in a <see cref="long"/>.
        /// </summary>
        private enum LargeEnum : ulong
        {
            Small = 1,
            Large = ulong.MaxValue,
        }

        /// <summary>
        /// Reads a fixed value, to build rows with known values.
        /// </summary>
        private class ConstantAccessor : IValueAccessor
        {

            /// <summary>
            /// The value to read.
            /// </summary>
            private object _value = null;

            /// <inheritdoc cref="ConstantAccessor"/>
            /// <param name="value"><inheritdoc cref="_value" path="/summary"/></param>
            public ConstantAccessor(object value)
            {
                _value = value;
            }

            /// <inheritdoc/>
            public Type ValueType => typeof(object);

            /// <inheritdoc/>
            public object Read(ValueReadContext context)
            {
                return _value;
            }

        }

        #endregion


        #region Fields

        private const string NumberId = "Number";
        private const string CheckedId = "Checked";
        private const string LabelId = "Label";
        private const string MissingId = "Missing";

        /// <summary>
        /// The columns of the rows built by <see cref="CreateRow"/>: a number, a checkbox and a text.
        /// </summary>
        private static readonly Dictionary<string, FilterColumn> Columns = new Dictionary<string, FilterColumn>
        {
            { NumberId, new FilterColumn(0, typeof(int), ToText) },
            { CheckedId, new FilterColumn(1, typeof(bool), ToText) },
            { LabelId, new FilterColumn(2, typeof(string), ToText) },
        };

        /// <summary>
        /// The objects created by a test, destroyed after it.
        /// </summary>
        private List<Object> _objects = new List<Object>();

        #endregion


        #region Lifecycle

        /// <summary>
        /// Destroys the objects created by the test.
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            foreach (Object obj in _objects)
            {
                if (obj != null)
                    Object.DestroyImmediate(obj);
            }
            _objects.Clear();
        }

        #endregion


        #region Number range

        /// <summary>
        /// Number ranges apply to integers and floating-point numbers only.
        /// </summary>
        [Test]
        public void NumberRange_CanApplyTo_NumbersOnly()
        {
            NumberRangeCondition condition = new NumberRangeCondition();
            foreach (Type type in new[] { typeof(int), typeof(byte), typeof(long), typeof(ulong), typeof(float), typeof(double) })
                Assert.IsTrue(condition.CanApplyTo(type), type.Name);

            foreach (Type type in new[] { typeof(DayOfWeek), typeof(bool), typeof(char), typeof(string), typeof(Vector2) })
                Assert.IsFalse(condition.CanApplyTo(type), type.Name);
        }

        /// <summary>
        /// Both limits are included.
        /// </summary>
        [Test]
        public void NumberRange_BothLimits_Included()
        {
            NumberRangeCondition condition = new NumberRangeCondition(NumberId, 10, 20);

            Assert.IsTrue(Matches(condition, typeof(int), 10));
            Assert.IsTrue(Matches(condition, typeof(int), 15));
            Assert.IsTrue(Matches(condition, typeof(int), 20));
            Assert.IsFalse(Matches(condition, typeof(int), 9));
            Assert.IsFalse(Matches(condition, typeof(int), 21));
            Assert.IsTrue(Matches(condition, typeof(float), 19.5f));
        }

        /// <summary>
        /// A range with a single limit is "at least" or "at most" a value.
        /// </summary>
        [Test]
        public void NumberRange_SingleLimit_OpenRange()
        {
            NumberRangeCondition atLeast = new NumberRangeCondition(NumberId, 10, null);
            Assert.IsTrue(Matches(atLeast, typeof(int), int.MaxValue));
            Assert.IsFalse(Matches(atLeast, typeof(int), 9));
            Assert.IsNull(atLeast.Max);

            NumberRangeCondition atMost = new NumberRangeCondition(NumberId, null, 10);
            Assert.IsTrue(Matches(atMost, typeof(int), int.MinValue));
            Assert.IsFalse(Matches(atMost, typeof(int), 11));
            Assert.IsNull(atMost.Min);
        }

        /// <summary>
        /// A value that is not a number, and NaN with a limit, are not in the range.
        /// </summary>
        [Test]
        public void NumberRange_NotANumber_NotInRange()
        {
            NumberRangeCondition condition = new NumberRangeCondition(NumberId, 0, 10);

            Assert.IsFalse(Matches(condition, typeof(int), null));
            Assert.IsFalse(Matches(condition, typeof(float), float.NaN));
        }

        #endregion


        #region Checkbox

        /// <summary>
        /// The condition keeps the checked or the unchecked values, and applies to booleans only.
        /// </summary>
        [Test]
        public void Bool_CheckedOrUnchecked_Matches()
        {
            BoolCondition isChecked = new BoolCondition(CheckedId, true);
            Assert.IsTrue(Matches(isChecked, typeof(bool), true));
            Assert.IsFalse(Matches(isChecked, typeof(bool), false));

            BoolCondition isUnchecked = new BoolCondition(CheckedId, false);
            Assert.IsTrue(Matches(isUnchecked, typeof(bool), false));
            Assert.IsFalse(Matches(isUnchecked, typeof(bool), true));

            Assert.IsTrue(isChecked.CanApplyTo(typeof(bool)));
            Assert.IsFalse(isChecked.CanApplyTo(typeof(int)));
        }

        #endregion


        #region Enum

        /// <summary>
        /// The condition keeps the chosen values, and applies to enums only.
        /// </summary>
        [Test]
        public void Enum_ChosenValues_Matches()
        {
            EnumCondition condition = new EnumCondition(NumberId, DayOfWeek.Monday, DayOfWeek.Friday);

            Assert.IsTrue(Matches(condition, typeof(DayOfWeek), DayOfWeek.Monday));
            Assert.IsTrue(Matches(condition, typeof(DayOfWeek), DayOfWeek.Friday));
            Assert.IsFalse(Matches(condition, typeof(DayOfWeek), DayOfWeek.Sunday));
            Assert.IsFalse(Matches(condition, typeof(DayOfWeek), null));

            Assert.IsTrue(condition.CanApplyTo(typeof(DayOfWeek)));
            Assert.IsFalse(condition.CanApplyTo(typeof(int)));
        }

        /// <summary>
        /// Without any chosen value, the condition keeps no row.
        /// </summary>
        [Test]
        public void Enum_NoValue_MatchesNothing()
        {
            EnumCondition condition = new EnumCondition(NumberId);

            Assert.IsFalse(Matches(condition, typeof(DayOfWeek), DayOfWeek.Monday));
        }

        /// <summary>
        /// With a flags enum, the condition keeps the values that have at least one of the chosen flags.
        /// </summary>
        [Test]
        public void Enum_Flags_AnyChosenFlag()
        {
            EnumCondition condition = new EnumCondition(NumberId, TestFlags.Fire, TestFlags.Ice);

            Assert.IsTrue(Matches(condition, typeof(TestFlags), TestFlags.Fire));
            Assert.IsTrue(Matches(condition, typeof(TestFlags), TestFlags.Ice | TestFlags.Poison));
            Assert.IsFalse(Matches(condition, typeof(TestFlags), TestFlags.Poison));
            Assert.IsFalse(Matches(condition, typeof(TestFlags), TestFlags.None));
        }

        /// <summary>
        /// With a flags enum, a chosen value of 0 keeps the values without any flag.
        /// </summary>
        [Test]
        public void Enum_FlagsNone_ValuesWithoutFlag()
        {
            EnumCondition condition = new EnumCondition(NumberId, TestFlags.None);

            Assert.IsTrue(Matches(condition, typeof(TestFlags), TestFlags.None));
            Assert.IsFalse(Matches(condition, typeof(TestFlags), TestFlags.Fire));
        }

        /// <summary>
        /// The values of an enum based on <see cref="ulong"/> that don't fit in a <see cref="long"/> stay distinct.
        /// </summary>
        [Test]
        public void Enum_LargeUnsignedValue_Distinct()
        {
            Assert.AreNotEqual(EnumCondition.ToNumber(LargeEnum.Small), EnumCondition.ToNumber(LargeEnum.Large));

            EnumCondition condition = new EnumCondition(NumberId, LargeEnum.Large);
            Assert.IsTrue(Matches(condition, typeof(LargeEnum), LargeEnum.Large));
            Assert.IsFalse(Matches(condition, typeof(LargeEnum), LargeEnum.Small));
        }

        #endregion


        #region Reference

        /// <summary>
        /// The condition keeps the references to the chosen asset, and applies to references only.
        /// </summary>
        [Test]
        public void Reference_ChosenAsset_Matches()
        {
            ScriptableObject asset = CreateObject();
            ScriptableObject otherAsset = CreateObject();
            ReferenceCondition condition = new ReferenceCondition(NumberId, ReferenceCondition.ReferenceMatch.IsAsset, asset);

            Assert.IsTrue(Matches(condition, typeof(ScriptableObject), asset));
            Assert.IsFalse(Matches(condition, typeof(ScriptableObject), otherAsset));
            Assert.IsFalse(Matches(condition, typeof(ScriptableObject), null));

            Assert.IsTrue(condition.CanApplyTo(typeof(ScriptableObject)));
            Assert.IsFalse(condition.CanApplyTo(typeof(string)));
        }

        /// <summary>
        /// Without a chosen asset, the condition keeps the empty references (<i>None</i>), not the missing ones.
        /// </summary>
        [Test]
        public void Reference_NoAsset_KeepsNone()
        {
            ReferenceCondition condition = new ReferenceCondition(NumberId, ReferenceCondition.ReferenceMatch.IsAsset);

            Assert.IsTrue(Matches(condition, typeof(ScriptableObject), null));
            Assert.IsFalse(Matches(condition, typeof(ScriptableObject), CreateObject()));
            Assert.IsFalse(Matches(condition, typeof(ScriptableObject), CreateDestroyedObject()));
        }

        /// <summary>
        /// The condition on missing references keeps the references to destroyed objects only.
        /// </summary>
        [Test]
        public void Reference_IsMissing_KeepsMissingOnly()
        {
            ReferenceCondition condition = new ReferenceCondition(NumberId, ReferenceCondition.ReferenceMatch.IsMissing);

            Assert.IsTrue(Matches(condition, typeof(ScriptableObject), CreateDestroyedObject()));
            Assert.IsFalse(Matches(condition, typeof(ScriptableObject), null));
            Assert.IsFalse(Matches(condition, typeof(ScriptableObject), CreateObject()));
        }

        /// <summary>
        /// A condition whose chosen asset has been destroyed keeps no row: not the empty references, nor the missing ones.
        /// </summary>
        [Test]
        public void Reference_DestroyedChosenAsset_MatchesNothing()
        {
            ScriptableObject destroyedAsset = CreateDestroyedObject();
            ReferenceCondition condition = new ReferenceCondition(NumberId, ReferenceCondition.ReferenceMatch.IsAsset, destroyedAsset);

            Assert.IsFalse(Matches(condition, typeof(ScriptableObject), null));
            Assert.IsFalse(Matches(condition, typeof(ScriptableObject), destroyedAsset));
            Assert.IsFalse(Matches(condition, typeof(ScriptableObject), CreateObject()));
        }

        #endregion


        #region Text

        /// <summary>
        /// The condition keeps the cells that contain the text, ignoring case, and applies to any type.
        /// </summary>
        [Test]
        public void Text_Contains_IgnoresCase()
        {
            TextCondition condition = new TextCondition(LabelId, "sword");

            Assert.IsTrue(Matches(condition, typeof(string), "Iron Sword"));
            Assert.IsFalse(Matches(condition, typeof(string), "Iron Shield"));
            Assert.IsFalse(Matches(condition, typeof(string), null));

            Assert.IsTrue(condition.CanApplyTo(typeof(string)));
            Assert.IsTrue(condition.CanApplyTo(typeof(Vector2)));
        }

        /// <summary>
        /// The text is not split into words, unlike the search.
        /// </summary>
        [Test]
        public void Text_SeveralWords_WholeText()
        {
            TextCondition condition = new TextCondition(LabelId, "iron sword");

            Assert.IsTrue(Matches(condition, typeof(string), "Iron Sword"));
            Assert.IsFalse(Matches(condition, typeof(string), "Sword of Iron"));
        }

        /// <summary>
        /// An empty text keeps all the cells.
        /// </summary>
        [Test]
        public void Text_Empty_MatchesAll()
        {
            TextCondition condition = new TextCondition(LabelId, string.Empty);

            Assert.IsTrue(Matches(condition, typeof(string), "Iron Sword"));
            Assert.IsTrue(Matches(condition, typeof(string), null));
        }

        /// <summary>
        /// A cell whose conversion to text fails doesn't contain the text.
        /// </summary>
        [Test]
        public void Text_FailedConversion_NoMatch()
        {
            TextCondition condition = new TextCondition(LabelId, "sword");
            FilterColumn column = new FilterColumn(0, typeof(string), value => null);

            Assert.IsFalse(condition.CreateMatcher(column)("Iron Sword"));
        }

        #endregion


        #region Compilation and groups

        /// <summary>
        /// A compiled condition reads the value of its column in the rows.
        /// </summary>
        [Test]
        public void Compile_Condition_ReadsItsColumn()
        {
            Predicate<TableRow> filter = new BoolCondition(CheckedId, true).Compile(Columns);

            Assert.IsTrue(filter(CreateRow(1, true, "A")));
            Assert.IsFalse(filter(CreateRow(1, false, "A")));
        }

        /// <summary>
        /// A condition on a column that doesn't exist, or whose values are of a type it doesn't support, is ignored.
        /// </summary>
        [Test]
        public void Compile_ConditionNotApplicable_Ignored()
        {
            Assert.IsNull(new BoolCondition(MissingId, true).Compile(Columns));
            Assert.IsNull(new BoolCondition(null, true).Compile(Columns));
            // The column of numbers has been changed to a checkbox, or the reverse
            Assert.IsNull(new BoolCondition(NumberId, true).Compile(Columns));
            Assert.IsNull(new NumberRangeCondition(CheckedId, 0, 1).Compile(Columns));
        }

        /// <summary>
        /// An "and" group keeps the rows that meet all its conditions.
        /// </summary>
        [Test]
        public void Compile_AndGroup_AllConditions()
        {
            FilterGroup group = new FilterGroup(FilterOperator.And,
                new NumberRangeCondition(NumberId, 10, null),
                new BoolCondition(CheckedId, true));
            Predicate<TableRow> filter = group.Compile(Columns);

            Assert.IsTrue(filter(CreateRow(10, true, "A")));
            Assert.IsFalse(filter(CreateRow(5, true, "A")));
            Assert.IsFalse(filter(CreateRow(10, false, "A")));
        }

        /// <summary>
        /// An "or" group keeps the rows that meet at least one of its conditions.
        /// </summary>
        [Test]
        public void Compile_OrGroup_AnyCondition()
        {
            FilterGroup group = new FilterGroup(FilterOperator.Or,
                new NumberRangeCondition(NumberId, 10, null),
                new BoolCondition(CheckedId, true));
            Predicate<TableRow> filter = group.Compile(Columns);

            Assert.IsTrue(filter(CreateRow(10, false, "A")));
            Assert.IsTrue(filter(CreateRow(5, true, "A")));
            Assert.IsFalse(filter(CreateRow(5, false, "A")));
        }

        /// <summary>
        /// Groups can contain groups: <c>Checked and (Number at least 10 or Label contains "sword")</c>.
        /// </summary>
        [Test]
        public void Compile_NestedGroups_Combined()
        {
            FilterGroup group = new FilterGroup(FilterOperator.And,
                new BoolCondition(CheckedId, true),
                new FilterGroup(FilterOperator.Or,
                    new NumberRangeCondition(NumberId, 10, null),
                    new TextCondition(LabelId, "sword")));
            Predicate<TableRow> filter = group.Compile(Columns);

            Assert.IsTrue(filter(CreateRow(10, true, "Shield")));
            Assert.IsTrue(filter(CreateRow(5, true, "Sword")));
            Assert.IsFalse(filter(CreateRow(5, true, "Shield")));
            Assert.IsFalse(filter(CreateRow(10, false, "Sword")));
        }

        /// <summary>
        /// The conditions that don't apply are removed from an "or" group, instead of making it keep all the rows.
        /// </summary>
        [Test]
        public void Compile_OrGroupWithIgnoredCondition_OtherConditionsOnly()
        {
            FilterGroup group = new FilterGroup(FilterOperator.Or,
                new BoolCondition(MissingId, true),
                new BoolCondition(CheckedId, true),
                null);
            Predicate<TableRow> filter = group.Compile(Columns);

            Assert.IsTrue(filter(CreateRow(1, true, "A")));
            Assert.IsFalse(filter(CreateRow(1, false, "A")));
        }

        /// <summary>
        /// A group without any condition that applies is ignored, so it keeps all the rows when it's the root of the filter.
        /// </summary>
        [Test]
        public void Compile_EmptyGroups_Ignored()
        {
            Assert.IsNull(new FilterGroup().Compile(Columns));
            Assert.IsNull(new FilterGroup(FilterOperator.Or, new BoolCondition(MissingId, true)).Compile(Columns));
            Assert.IsNull(new FilterGroup(FilterOperator.And, new FilterGroup(FilterOperator.Or)).Compile(Columns));
        }

        #endregion


        #region Default conditions

        /// <summary>
        /// A checkbox column gets a condition that keeps the checked rows, the other columns a text condition that keeps all the rows.
        /// Each default condition applies to its column.
        /// </summary>
        [Test]
        public void CreateDefault_ByType_ApplicableCondition()
        {
            FilterCondition boolCondition = FilterConditions.CreateDefault(CheckedId, typeof(bool));
            Assert.IsInstanceOf<BoolCondition>(boolCondition);
            Assert.IsTrue(((BoolCondition)boolCondition).IsChecked);

            foreach (Type type in new[] { typeof(string), typeof(int), typeof(DayOfWeek), typeof(ScriptableObject), typeof(Vector2) })
            {
                FilterCondition condition = FilterConditions.CreateDefault(LabelId, type);
                Assert.IsInstanceOf<TextCondition>(condition, type.Name);
                Assert.AreEqual(LabelId, condition.ColumnId);
                Assert.IsTrue(condition.CanApplyTo(type), type.Name);
                Assert.IsTrue(Matches(condition, type, "Anything"), type.Name);
            }
        }

        #endregion


        #region Serialization

        /// <summary>
        /// A filter tree survives Unity's serialization, with the kind and the settings of each node.
        /// </summary>
        [Test]
        public void Serialization_RoundTrip_TreeKept()
        {
            ScriptableObject asset = CreateObject();
            FilterTestSO holder = ScriptableObject.CreateInstance<FilterTestSO>();
            _objects.Add(holder);
            holder.filter = new FilterGroup(FilterOperator.Or,
                new NumberRangeCondition(NumberId, 1.5, null),
                new FilterGroup(FilterOperator.And,
                    new BoolCondition(CheckedId, false),
                    new EnumCondition("Day", DayOfWeek.Friday)),
                new ReferenceCondition("Reference", ReferenceCondition.ReferenceMatch.IsAsset, asset),
                new TextCondition(LabelId, "sword"));

            // Instantiating copies the object through its serialized data
            FilterTestSO copy = Object.Instantiate(holder);
            _objects.Add(copy);

            FilterGroup root = copy.filter as FilterGroup;
            Assert.IsNotNull(root);
            Assert.AreNotSame(holder.filter, root);
            Assert.AreEqual(FilterOperator.Or, root.Operator);
            Assert.AreEqual(4, root.Nodes.Count);

            NumberRangeCondition range = (NumberRangeCondition)root.Nodes[0];
            Assert.AreEqual(NumberId, range.ColumnId);
            Assert.AreEqual(1.5, range.Min);
            Assert.IsNull(range.Max);

            FilterGroup subgroup = (FilterGroup)root.Nodes[1];
            Assert.AreEqual(FilterOperator.And, subgroup.Operator);
            Assert.IsFalse(((BoolCondition)subgroup.Nodes[0]).IsChecked);
            CollectionAssert.AreEqual(new[] { EnumCondition.ToNumber(DayOfWeek.Friday) }, ((EnumCondition)subgroup.Nodes[1]).Values);

            ReferenceCondition reference = (ReferenceCondition)root.Nodes[2];
            Assert.AreEqual(ReferenceCondition.ReferenceMatch.IsAsset, reference.Match);
            Assert.AreSame(asset, reference.Asset);

            Assert.AreEqual("sword", ((TextCondition)root.Nodes[3]).Text);
        }

        #endregion


        #region Private API

        /// <summary>
        /// Checks a value with a condition, as a cell of a column of the given type.
        /// </summary>
        /// <param name="condition">The condition to check.</param>
        /// <param name="valueType">The type of the values of the column.</param>
        /// <param name="value">The value of the cell.</param>
        /// <returns>Returns true if the value meets the condition.</returns>
        private static bool Matches(FilterCondition condition, Type valueType, object value)
        {
            return condition.CreateMatcher(new FilterColumn(0, valueType, ToText))(value);
        }

        /// <summary>
        /// Converts a value to text for the tests.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <returns>Returns the text of the value, or null if the value is null.</returns>
        private static string ToText(object value)
        {
            return value?.ToString();
        }

        /// <summary>
        /// Creates a row with the given values, in the order of <see cref="Columns"/>.
        /// </summary>
        /// <param name="number">The value of the <see cref="NumberId"/> column.</param>
        /// <param name="isChecked">The value of the <see cref="CheckedId"/> column.</param>
        /// <param name="label">The value of the <see cref="LabelId"/> column.</param>
        /// <returns>Returns the created row.</returns>
        private static TableRow CreateRow(int number, bool isChecked, string label)
        {
            IValueAccessor[] accessors = new object[] { number, isChecked, label }
                .Select(value => (IValueAccessor)new ConstantAccessor(value))
                .ToArray();

            TableRow row = new TableRow(null, accessors.Length);
            row.Read(accessors);
            return row;
        }

        /// <summary>
        /// Creates an in-memory object, destroyed after the test.
        /// </summary>
        /// <returns>Returns the created object.</returns>
        private ScriptableObject CreateObject()
        {
            FilterTestSO obj = ScriptableObject.CreateInstance<FilterTestSO>();
            _objects.Add(obj);
            return obj;
        }

        /// <summary>
        /// Creates an object, then destroys it, like the target of a missing reference.
        /// </summary>
        /// <returns>Returns the destroyed object: not null for C#, but null for Unity.</returns>
        private static ScriptableObject CreateDestroyedObject()
        {
            FilterTestSO obj = ScriptableObject.CreateInstance<FilterTestSO>();
            Object.DestroyImmediate(obj);
            return obj;
        }

        #endregion

    }

}
