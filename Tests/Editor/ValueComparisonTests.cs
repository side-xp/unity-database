using System;
using System.Collections.Generic;

using NUnit.Framework;

using UnityEngine;

using SideXP.Database.EditorOnly;

using Object = UnityEngine.Object;

namespace SideXP.Database.Tests
{

    /// <summary>
    /// Tests for the default comparison of values, and the comparison of the built-in handlers.
    /// </summary>
    public class ValueComparisonTests
    {

        #region Fields

        /// <summary>
        /// The in-memory objects created by a test, destroyed after it.
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


        #region Default comparison

        /// <summary>
        /// The types that implement <see cref="IComparable"/> can be sorted, the others can't.
        /// </summary>
        [Test]
        public void CanCompare_Types_ChecksComparable()
        {
            Assert.IsTrue(ValueComparison.CanCompare(typeof(string)));
            Assert.IsTrue(ValueComparison.CanCompare(typeof(int)));
            Assert.IsTrue(ValueComparison.CanCompare(typeof(float)));
            Assert.IsTrue(ValueComparison.CanCompare(typeof(bool)));
            Assert.IsTrue(ValueComparison.CanCompare(typeof(DayOfWeek)));
            Assert.IsFalse(ValueComparison.CanCompare(typeof(Vector3)));
            Assert.IsFalse(ValueComparison.CanCompare(typeof(int[])));
            Assert.IsFalse(ValueComparison.CanCompare(null));
        }

        /// <summary>
        /// Null values come before the others.
        /// </summary>
        [Test]
        public void Compare_NullValues_ComeFirst()
        {
            Assert.AreEqual(0, ValueComparison.Compare(null, null));
            Assert.Less(ValueComparison.Compare(null, "a"), 0);
            Assert.Greater(ValueComparison.Compare(3, null), 0);
        }

        /// <summary>
        /// Numbers, booleans and enums are compared by value.
        /// </summary>
        [Test]
        public void Compare_ComparableValues_UseValue()
        {
            Assert.Less(ValueComparison.Compare(2, 10), 0);
            Assert.Greater(ValueComparison.Compare(1.5f, -3f), 0);
            Assert.AreEqual(0, ValueComparison.Compare(7, 7));
            Assert.Less(ValueComparison.Compare(false, true), 0);
            Assert.Less(ValueComparison.Compare(DayOfWeek.Monday, DayOfWeek.Friday), 0);
        }

        /// <summary>
        /// Strings use the natural order, so the numbers in names are compared by value.
        /// </summary>
        [Test]
        public void Compare_Strings_UseNaturalOrder()
        {
            Assert.Less(ValueComparison.Compare("Sword 2", "Sword 10"), 0);
            Assert.Less(ValueComparison.Compare("Axe", "Sword"), 0);
            Assert.Less(ValueComparison.Compare(string.Empty, "Axe"), 0);
        }

        /// <summary>
        /// Values of different types don't throw: they are compared by their text.
        /// </summary>
        [Test]
        public void Compare_DifferentTypes_UsesText()
        {
            Assert.DoesNotThrow(() => ValueComparison.Compare(3, 2.5f));
            Assert.Greater(ValueComparison.Compare(3, 2.5f), 0);
        }

        #endregion


        #region Built-in handlers

        /// <summary>
        /// References are sorted with empty references first, then missing references, then by name in the natural order.
        /// </summary>
        [Test]
        public void ObjectReferenceHandler_Compare_SortsEmptyMissingThenNames()
        {
            ObjectReferenceCellHandler handler = new ObjectReferenceCellHandler();
            PipelineTestSO sword2 = CreateObject("Sword 2");
            PipelineTestSO sword10 = CreateObject("Sword 10");
            PipelineTestSO missing = CreateObject("Missing");
            Object.DestroyImmediate(missing);

            List<object> values = new List<object> { sword10, missing, sword2, null };
            values.Sort(handler.Compare);

            Assert.IsNull(values[0]);
            Assert.AreSame(missing, values[1]);
            Assert.AreSame(sword2, values[2]);
            Assert.AreSame(sword10, values[3]);
            Assert.IsTrue(handler.CanCompare(typeof(Sprite)));
        }

        /// <summary>
        /// Colors can't be sorted, since they have no natural order.
        /// </summary>
        [Test]
        public void ColorHandler_CanCompare_ReturnsFalse()
        {
            Assert.IsFalse(new ColorCellHandler().CanCompare(typeof(Color)));
        }

        #endregion


        #region Private API

        /// <summary>
        /// Creates an in-memory object, destroyed after the test.
        /// </summary>
        /// <param name="name">The name of the object.</param>
        /// <returns>Returns the created object.</returns>
        private PipelineTestSO CreateObject(string name)
        {
            PipelineTestSO obj = ScriptableObject.CreateInstance<PipelineTestSO>();
            obj.name = name;
            _objects.Add(obj);
            return obj;
        }

        #endregion

    }

}
