using System;

using UnityEditor;

using Object = UnityEngine.Object;

namespace SideXP.Database.EditorOnly
{

    /// <summary>
    /// The asset being read by the accessors of a row, with its serialized object created only if an accessor needs it.
    /// </summary>
    /// <remarks>
    /// The accessors of a row share the same context, so a row creates at most one serialized object, whatever the number of columns
    /// that read serialized properties. Reflection reads never create it. Dispose the context once the row has been read.
    /// </remarks>
    internal sealed class ValueReadContext : IDisposable
    {

        #region Fields

        /// <summary>
        /// The asset being read.
        /// </summary>
        private Object _asset = null;

        /// <summary>
        /// The serialized object of the asset, or null if no accessor has needed it yet.
        /// </summary>
        private SerializedObject _serializedObject = null;

        #endregion


        #region Lifecycle

        /// <inheritdoc cref="ValueReadContext"/>
        /// <param name="asset"><inheritdoc cref="_asset" path="/summary"/></param>
        public ValueReadContext(Object asset)
        {
            _asset = asset;
        }

        /// <summary>
        /// Disposes the serialized object of the asset, if it has been created.
        /// </summary>
        public void Dispose()
        {
            if (_serializedObject == null)
                return;

            _serializedObject.Dispose();
            _serializedObject = null;
        }

        #endregion


        #region Public API

        /// <inheritdoc cref="_asset"/>
        public Object Asset => _asset;

        /// <summary>
        /// Gets the serialized object of the asset, created the first time it's needed.
        /// </summary>
        public SerializedObject SerializedObject
        {
            get
            {
                if (_serializedObject == null)
                    _serializedObject = new SerializedObject(_asset);

                return _serializedObject;
            }
        }

        #endregion

    }

}
