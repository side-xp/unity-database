using System;
using System.Collections.Generic;
using System.Reflection;

using NUnit.Framework;

using UnityEngine;
using UnityEditor;

using SideXP.Database.EditorOnly;

using Object = UnityEngine.Object;

namespace SideXP.Database.Tests
{

    /// <summary>
    /// Tests for the value accessors and the read context they share.
    /// </summary>
    public class ValueAccessorTests
    {

        #region Fields

        /// <summary>
        /// The in-memory asset read by the tests.
        /// </summary>
        private PipelineTestSO _asset = null;

        #endregion


        #region Lifecycle

        /// <summary>
        /// Creates the in-memory asset read by the tests.
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            _asset = ScriptableObject.CreateInstance<PipelineTestSO>();
            _asset.name = "Test Asset";
            _asset.number = 42;
        }

        /// <summary>
        /// Destroys the in-memory asset.
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_asset);
        }

        #endregion


        #region Reflection accessor

        /// <summary>
        /// The reflection accessor reads the value of a public field, and has the type of that field.
        /// </summary>
        [Test]
        public void ReflectionAccessor_PublicField_ReadsValue()
        {
            ReflectionAccessor accessor = CreateReflectionAccessor(nameof(PipelineTestSO.number));

            Assert.AreEqual(typeof(int), accessor.ValueType);
            Assert.AreEqual(42, Read(accessor));
        }

        /// <summary>
        /// The reflection accessor reads the value of a private serialized field.
        /// </summary>
        [Test]
        public void ReflectionAccessor_PrivateField_ReadsValue()
        {
            ReflectionAccessor accessor = CreateReflectionAccessor("_secret");

            Assert.AreEqual("secret", Read(accessor));
        }

        /// <summary>
        /// A reflection accessor can't be created without fields.
        /// </summary>
        [Test]
        public void ReflectionAccessor_EmptyPath_Throws()
        {
            Assert.Throws<ArgumentException>(() => new ReflectionAccessor(new FieldInfo[0]));
            Assert.Throws<ArgumentException>(() => new ReflectionAccessor(null));
        }

        /// <summary>
        /// The columns declared in code are read with reflection.
        /// </summary>
        [Test]
        public void Create_CodeColumn_ReturnsReflectionAccessor()
        {
            List<ColumnDefinition> columns = CodeTables.CollectColumns(typeof(ColumnsSO), new List<string>());

            foreach (ColumnDefinition column in columns)
                Assert.IsInstanceOf<ReflectionAccessor>(ValueAccessors.Create(column));
        }

        #endregion


        #region Asset name accessor

        /// <summary>
        /// The asset name accessor reads the name of the asset.
        /// </summary>
        [Test]
        public void AssetNameAccessor_Read_ReturnsAssetName()
        {
            Assert.AreEqual(typeof(string), AssetNameAccessor.Instance.ValueType);
            Assert.AreEqual("Test Asset", Read(AssetNameAccessor.Instance));
        }

        #endregion


        #region Read context

        /// <summary>
        /// The read context creates the serialized object of the asset once, and releases it when disposed.
        /// </summary>
        [Test]
        public void ReadContext_SerializedObject_CreatedOnce()
        {
            ValueReadContext context = new ValueReadContext(_asset);
            SerializedObject serializedObject = context.SerializedObject;

            Assert.AreSame(serializedObject, context.SerializedObject);
            Assert.AreSame(_asset, serializedObject.targetObject);

            context.Dispose();
            Assert.AreNotSame(serializedObject, context.SerializedObject);
            context.Dispose();
        }

        #endregion


        #region Private API

        /// <summary>
        /// Creates a reflection accessor for a field of <see cref="PipelineTestSO"/>.
        /// </summary>
        /// <param name="fieldName">The name of the field.</param>
        /// <returns>Returns the created accessor.</returns>
        private static ReflectionAccessor CreateReflectionAccessor(string fieldName)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            FieldInfo field = typeof(PipelineTestSO).GetField(fieldName, flags);
            return new ReflectionAccessor(new[] { field });
        }

        /// <summary>
        /// Reads the value of the test asset with a given accessor.
        /// </summary>
        /// <param name="accessor">The accessor to use.</param>
        /// <returns>Returns the read value.</returns>
        private object Read(IValueAccessor accessor)
        {
            using (ValueReadContext context = new ValueReadContext(_asset))
                return accessor.Read(context);
        }

        #endregion

    }

}
