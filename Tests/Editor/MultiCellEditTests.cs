using System.Collections.Generic;

using NUnit.Framework;

using SideXP.Database.EditorOnly;

namespace SideXP.Database.Tests
{

    /// <summary>
    /// Tests for <see cref="MultiCellEdit"/>: the cells that receive the value of an edit, the mixed values and the changed values.
    /// </summary>
    public class MultiCellEditTests
    {

        #region Subclasses

        /// <summary>
        /// An enum type of the tests.
        /// </summary>
        private enum Rarity
        {
            Common,
            Rare,
        }

        /// <summary>
        /// Another enum type of the tests, with the same underlying values as <see cref="Rarity"/>.
        /// </summary>
        private enum Tier
        {
            Low,
            High,
        }

        #endregion


        #region Fields

        private const string PriceColumn = "_price";
        private const string WeightColumn = "_weight";
        private const string LevelColumn = "_level";

        #endregion


        #region Tests

        /// <summary>
        /// Without the modifier, an edit stays in its column, even if other columns have the same type.
        /// </summary>
        [Test]
        public void IsTarget_WithoutModifier_SameColumnOnly()
        {
            Assert.IsTrue(MultiCellEdit.IsTarget(PriceColumn, typeof(float), PriceColumn, typeof(float), false));
            Assert.IsFalse(MultiCellEdit.IsTarget(PriceColumn, typeof(float), WeightColumn, typeof(float), false));
        }

        /// <summary>
        /// With the modifier, an edit applies to the columns of the same type only: an <see cref="int"/> column and a
        /// <see cref="float"/> column are different types, as are two enum types.
        /// </summary>
        [Test]
        public void IsTarget_WithModifier_SameTypeOnly()
        {
            Assert.IsTrue(MultiCellEdit.IsTarget(PriceColumn, typeof(float), WeightColumn, typeof(float), true));
            Assert.IsFalse(MultiCellEdit.IsTarget(PriceColumn, typeof(float), LevelColumn, typeof(int), true));
            Assert.IsFalse(MultiCellEdit.IsTarget(PriceColumn, typeof(Rarity), WeightColumn, typeof(Tier), true));
        }

        /// <summary>
        /// The values are mixed if at least one differs from the value of the edited cell, and not mixed without other values.
        /// </summary>
        [Test]
        public void AreMixed_Values_ComparedToEditedValue()
        {
            Assert.IsFalse(MultiCellEdit.AreMixed(3, new List<object> { 3, 3 }));
            Assert.IsTrue(MultiCellEdit.AreMixed(3, new List<object> { 3, 4 }));
            Assert.IsTrue(MultiCellEdit.AreMixed(null, new List<object> { "text" }));
            Assert.IsFalse(MultiCellEdit.AreMixed(3, new List<object>()));
        }

        /// <summary>
        /// An edit that doesn't change the value doesn't apply to the other cells, unless the user changed a field that showed the mixed
        /// value indicator.
        /// </summary>
        [Test]
        public void HasChanged_Values_DependsOnValueAndMixedState()
        {
            Assert.IsTrue(MultiCellEdit.HasChanged(3, 4, false, false));
            Assert.IsFalse(MultiCellEdit.HasChanged(3, 3, false, false));

            // Confirmed without touching a mixed field
            Assert.IsFalse(MultiCellEdit.HasChanged(3, 3, true, true));
            // The user typed the original value in a mixed field
            Assert.IsTrue(MultiCellEdit.HasChanged(3, 3, true, false));
        }

        #endregion

    }

}
