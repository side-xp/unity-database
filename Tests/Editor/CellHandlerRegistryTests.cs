using System;
using System.Collections.Generic;
using System.Reflection;

using NUnit.Framework;

using UnityEngine;

using SideXP.Database.EditorOnly;

namespace SideXP.Database.Tests
{

    /// <summary>
    /// Tests for the selection of the cell handlers: by attribute, by value type, and the default handler.
    /// </summary>
    public class CellHandlerRegistryTests
    {

        #region Subclasses

        /// <summary>
        /// Holds the fields used to test the selection by attribute.
        /// </summary>
        private class TestFields
        {

            [TestCell]
            public bool markedToggle = false;

            public bool toggle = false;

        }

        #endregion


        #region By attribute

        /// <summary>
        /// The handler of an attribute of the field is selected before the handler of the value type.
        /// </summary>
        [Test]
        public void GetHandler_FieldWithAttribute_UsesAttributeHandler()
        {
            CellHandler handler = CellHandlerRegistry.Default.GetHandler(GetField(nameof(TestFields.markedToggle)), typeof(bool));

            Assert.IsInstanceOf<TestCellHandler>(handler);
        }

        /// <summary>
        /// A field without attribute uses the handler of its value type.
        /// </summary>
        [Test]
        public void GetHandler_FieldWithoutAttribute_UsesTypeHandler()
        {
            CellHandler handler = CellHandlerRegistry.Default.GetHandler(GetField(nameof(TestFields.toggle)), typeof(bool));

            Assert.IsInstanceOf<BoolCellHandler>(handler);
        }

        #endregion


        #region By value type

        /// <summary>
        /// The built-in handlers are selected by the type of the values.
        /// </summary>
        /// <param name="valueType">The type of the values.</param>
        /// <param name="handlerType">The expected type of handler.</param>
        [TestCase(typeof(bool), typeof(BoolCellHandler))]
        [TestCase(typeof(Color), typeof(ColorCellHandler))]
        [TestCase(typeof(Color32), typeof(ColorCellHandler))]
        [TestCase(typeof(UnityEngine.Object), typeof(ObjectReferenceCellHandler))]
        [TestCase(typeof(Sprite), typeof(ObjectReferenceCellHandler))]
        [TestCase(typeof(ScriptableObject), typeof(ObjectReferenceCellHandler))]
        [TestCase(typeof(DayOfWeek), typeof(EnumCellHandler))]
        [TestCase(typeof(AttributeTargets), typeof(EnumCellHandler))]
        public void GetHandler_BuiltInType_UsesBuiltInHandler(Type valueType, Type handlerType)
        {
            Assert.IsInstanceOf(handlerType, CellHandlerRegistry.Default.GetHandler(null, valueType));
        }

        /// <summary>
        /// The types without handler use the default handler.
        /// </summary>
        /// <param name="valueType">The type of the values.</param>
        [TestCase(typeof(int))]
        [TestCase(typeof(string))]
        [TestCase(typeof(Vector3))]
        public void GetHandler_TypeWithoutHandler_UsesDefaultHandler(Type valueType)
        {
            Assert.AreSame(CellHandlerRegistry.Default.DefaultHandler, CellHandlerRegistry.Default.GetHandler(null, valueType));
        }

        /// <summary>
        /// A handler registered for the derived types is used for a derived type without its own handler.
        /// </summary>
        [Test]
        public void GetHandler_DerivedTypeOfInheritedHandler_UsesBaseHandler()
        {
            Assert.IsInstanceOf<TestBaseValueHandler>(CellHandlerRegistry.Default.GetHandler(null, typeof(TestDerivedValue)));
        }

        /// <summary>
        /// A handler registered for a type only is not used for its derived types.
        /// </summary>
        [Test]
        public void GetHandler_DerivedTypeOfExactHandler_UsesDefaultHandler()
        {
            CellHandlerRegistry registry = CellHandlerRegistry.Default;

            Assert.IsInstanceOf<TestExactValueHandler>(registry.GetHandler(null, typeof(TestExactValue)));
            Assert.AreSame(registry.DefaultHandler, registry.GetHandler(null, typeof(TestExactDerivedValue)));
        }

        /// <summary>
        /// A handler is shared: the same instance is returned for all the columns it handles.
        /// </summary>
        [Test]
        public void GetHandler_SameType_ReturnsSharedInstance()
        {
            CellHandlerRegistry registry = CellHandlerRegistry.Default;

            Assert.AreSame(registry.GetHandler(null, typeof(Color)), registry.GetHandler(null, typeof(Color32)));
        }

        #endregion


        #region Invalid handlers

        /// <summary>
        /// Types that can't be handlers are skipped with a warning.
        /// </summary>
        [Test]
        public void Constructor_InvalidHandlerTypes_SkippedWithWarnings()
        {
            List<string> warnings = new List<string>();

            CellHandlerRegistry registry = new CellHandlerRegistry(new[] { typeof(string), typeof(CellHandler) }, warnings);

            Assert.AreEqual(2, warnings.Count);
            Assert.AreSame(registry.DefaultHandler, registry.GetHandler(null, typeof(bool)));
        }

        #endregion


        #region Private API

        /// <summary>
        /// Gets a field of <see cref="TestFields"/>.
        /// </summary>
        /// <param name="name">The name of the field.</param>
        /// <returns>Returns the found field.</returns>
        private static FieldInfo GetField(string name)
        {
            return typeof(TestFields).GetField(name);
        }

        #endregion

    }

}
