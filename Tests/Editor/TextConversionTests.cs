using System;
using System.Collections.Generic;

using NUnit.Framework;

using UnityEngine;

using SideXP.Database.EditorOnly;

namespace SideXP.Database.Tests
{

    /// <summary>
    /// Tests for the default text conversion, and the text conversion of the built-in handlers.
    /// </summary>
    public class TextConversionTests
    {

        #region To text

        /// <summary>
        /// Values are converted to text without depending on the culture of the user.
        /// </summary>
        [Test]
        public void ToText_Values_UseInvariantFormats()
        {
            Assert.AreEqual(string.Empty, TextConversion.ToText(null));
            Assert.AreEqual("text", TextConversion.ToText("text"));
            Assert.AreEqual("42", TextConversion.ToText(42));
            Assert.AreEqual("-1.5", TextConversion.ToText(-1.5f));
            Assert.AreEqual("0.1", TextConversion.ToText(0.1));
            Assert.AreEqual("True", TextConversion.ToText(true));
            Assert.AreEqual("Friday", TextConversion.ToText(DayOfWeek.Friday));
        }

        /// <summary>
        /// Collections are converted to their number of elements.
        /// </summary>
        [Test]
        public void ToText_Collections_ReturnsNumberOfElements()
        {
            Assert.AreEqual("3 elements", TextConversion.ToText(new[] { 1, 2, 3 }));
            Assert.AreEqual("1 element", TextConversion.ToText(new List<string> { "a" }));
        }

        /// <summary>
        /// Floats are converted to the shortest text that converts back to the same value.
        /// </summary>
        [Test]
        public void ToText_Float_RoundTrips()
        {
            const float value = 1f / 3f;

            Assert.IsTrue(TextConversion.TryParse(TextConversion.ToText(value), typeof(float), out object parsed));
            Assert.AreEqual(value, parsed);
        }

        #endregion


        #region Parse

        /// <summary>
        /// Texts are converted to values of the supported types, without depending on the culture of the user.
        /// </summary>
        [Test]
        public void TryParse_SupportedTypes_ReturnsValues()
        {
            AssertParsed("text", typeof(string), "text");
            AssertParsed(" 42 ", typeof(int), 42);
            AssertParsed("-1.5", typeof(float), -1.5f);
            AssertParsed("2.25", typeof(double), 2.25);
            AssertParsed("true", typeof(bool), true);
            AssertParsed("friday", typeof(DayOfWeek), DayOfWeek.Friday);
            AssertParsed("x", typeof(char), 'x');
        }

        /// <summary>
        /// Texts that don't match the type, and unsupported types, are not converted.
        /// </summary>
        [Test]
        public void TryParse_InvalidTexts_ReturnsFalse()
        {
            Assert.IsFalse(TextConversion.TryParse("abc", typeof(int), out object value));
            Assert.IsNull(value);
            Assert.IsFalse(TextConversion.TryParse("1,5", typeof(float), out _));
            Assert.IsFalse(TextConversion.TryParse("maybe", typeof(bool), out _));
            Assert.IsFalse(TextConversion.TryParse("1", typeof(Vector3), out _));
            Assert.IsFalse(TextConversion.TryParse(null, typeof(int), out _));
        }

        #endregion


        #region Built-in handlers

        /// <summary>
        /// Colors are converted to hexadecimal text and back, with or without the <c>#</c> prefix.
        /// </summary>
        [Test]
        public void ColorHandler_Text_ConvertsHexadecimal()
        {
            ColorCellHandler handler = new ColorCellHandler();

            Assert.AreEqual("#FF000080", handler.ToText(new Color32(255, 0, 0, 128)));
            Assert.IsTrue(handler.TryParse("#00FF00", typeof(Color), out object color));
            Assert.AreEqual(Color.green, color);
            Assert.IsTrue(handler.TryParse("0000FFFF", typeof(Color32), out object color32));
            Assert.AreEqual(new Color32(0, 0, 255, 255), color32);
            Assert.IsFalse(handler.TryParse("not a color", typeof(Color), out _));
        }

        /// <summary>
        /// A null reference is converted to an empty text, and an empty text to a null reference.
        /// </summary>
        [Test]
        public void ObjectReferenceHandler_NullReference_ConvertsToEmptyText()
        {
            ObjectReferenceCellHandler handler = new ObjectReferenceCellHandler();

            Assert.AreEqual(string.Empty, handler.ToText(null));
            Assert.IsTrue(handler.TryParse(string.Empty, typeof(Sprite), out object reference));
            Assert.IsNull(reference);
            Assert.IsFalse(handler.TryParse(string.Empty, typeof(int), out _));
        }

        #endregion


        #region Private API

        /// <summary>
        /// Asserts that a text is converted to an expected value.
        /// </summary>
        /// <param name="text">The text to convert.</param>
        /// <param name="valueType">The type of the value to get.</param>
        /// <param name="expected">The expected value.</param>
        private static void AssertParsed(string text, Type valueType, object expected)
        {
            Assert.IsTrue(TextConversion.TryParse(text, valueType, out object value), $"\"{text}\" should be converted to {valueType}.");
            Assert.AreEqual(expected, value);
        }

        #endregion

    }

}
