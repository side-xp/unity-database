using System;

using UnityEditor;

using Object = UnityEngine.Object;

namespace SideXP.Database.EditorOnly
{

    /// <summary>
    /// The write path of the edit of a cell: the serialized object of its asset, the property of its column, and the Undo group of the
    /// edit.
    /// </summary>
    /// <remarks>
    /// Only the edited cell has a serialized object, created when the edit starts and disposed when it ends. All the writes of the
    /// edit are grouped in a single Undo step when it's confirmed, and reverted when it's cancelled. Writes go through
    /// <see cref="SerializedProperty.boxedValue"/> and <see cref="SerializedObject.ApplyModifiedProperties"/>, which handle Undo, the
    /// dirty flag and <c>OnValidate()</c>.
    /// </remarks>
    internal class CellEditSession : IDisposable
    {

        #region Fields

        /// <summary>
        /// The serialized object of the edited asset.
        /// </summary>
        private SerializedObject _serializedObject = null;

        /// <summary>
        /// The serialized property of the edited cell.
        /// </summary>
        private SerializedProperty _property = null;

        /// <summary>
        /// The Undo group created when the edit started.
        /// </summary>
        private int _undoGroup = 0;

        /// <summary>
        /// Checks if the edit has been confirmed or cancelled.
        /// </summary>
        private bool _isEnded = false;

        #endregion


        #region Lifecycle

        /// <summary>
        /// Starts the edit of a cell: creates the serialized object of its asset, and an Undo group for the edit.
        /// </summary>
        /// <param name="asset">The asset of the edited cell.</param>
        /// <param name="propertyPath">The path of the serialized property of the edited cell, from the asset.</param>
        /// <param name="undoName">The name of the Undo step, shown in the Edit menu.</param>
        /// <exception cref="ArgumentException">Thrown if the asset has no property at the given path.</exception>
        public CellEditSession(Object asset, string propertyPath, string undoName)
        {
            _serializedObject = new SerializedObject(asset);
            _property = _serializedObject.FindProperty(propertyPath);
            if (_property == null)
            {
                _serializedObject.Dispose();
                throw new ArgumentException($"The asset {asset.name} has no serialized property at \"{propertyPath}\".",
                    nameof(propertyPath));
            }

            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName(undoName);
            _undoGroup = Undo.GetCurrentGroup();
        }

        /// <summary>
        /// Cancels the edit if it's still in progress, and disposes the serialized object.
        /// </summary>
        public void Dispose()
        {
            if (!_isEnded)
                Cancel();

            _serializedObject.Dispose();
        }

        #endregion


        #region Public API

        /// <inheritdoc cref="_property"/>
        /// <remarks>
        /// Fields bound to this property write it themselves: confirming the edit then only groups their writes.
        /// </remarks>
        public SerializedProperty Property => _property;

        /// <summary>
        /// Writes a value to the property of the edited cell.
        /// </summary>
        /// <param name="value">The value to write. Must be of the type of the property.</param>
        /// <returns>Returns true if the value has changed the asset.</returns>
        public bool Write(object value)
        {
            _serializedObject.Update();
            _property.boxedValue = value;
            return _serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        /// Confirms the edit: groups its writes into a single Undo step.
        /// </summary>
        public void Confirm()
        {
            if (_isEnded)
                return;

            _isEnded = true;
            Undo.CollapseUndoOperations(_undoGroup);
        }

        /// <summary>
        /// Cancels the edit: reverts its writes, without a redo step.
        /// </summary>
        public void Cancel()
        {
            if (_isEnded)
                return;

            _isEnded = true;
            // Always reverted, since fields bound to the property write it without going through this session
            Undo.RevertAllDownToGroup(_undoGroup);
        }

        #endregion

    }

}
