using System;
using System.Collections.Generic;
using System.Globalization;

using UnityEngine;
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
            SetValue(_property, value);
            return _serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        /// Writes a value to a cell other than the edited one, through its own serialized object, for an edit that applies to several
        /// cells. The write is recorded in the current Undo group, so it joins the Undo step of the edit when the edit is confirmed.
        /// </summary>
        /// <param name="asset">The asset of the cell.</param>
        /// <param name="propertyPath">The path of the serialized property of the cell, from the asset.</param>
        /// <param name="value">The value to write, of the C# type of the field.</param>
        /// <returns>Returns true if the value has changed the asset.</returns>
        /// <exception cref="ArgumentException">Thrown if the asset has no property at the given path.</exception>
        public static bool WriteValue(Object asset, string propertyPath, object value)
        {
            using (SerializedObject serializedObject = new SerializedObject(asset))
            {
                SerializedProperty property = serializedObject.FindProperty(propertyPath);
                if (property == null)
                {
                    throw new ArgumentException($"The asset {asset.name} has no serialized property at \"{propertyPath}\".",
                        nameof(propertyPath));
                }

                SetValue(property, value);
                return serializedObject.ApplyModifiedProperties();
            }
        }

        /// <summary>
        /// Writes several values to the cells of an asset, through a single serialized object, for example the values of an import.
        /// </summary>
        /// <remarks>
        /// An undoable write is recorded in the current Undo group. A write without Undo is meant for an asset that has just been created,
        /// whose creation can't be undone: undoing its values would leave it with the default values.
        /// </remarks>
        /// <param name="asset">The asset of the cells.</param>
        /// <param name="values">The paths of the serialized properties of the cells, from the asset, with the values to write, of the
        /// C# types of the fields.</param>
        /// <param name="isUndoable">If enabled, the writes are recorded in the current Undo group.</param>
        /// <returns>Returns true if the values have changed the asset.</returns>
        /// <exception cref="ArgumentException">Thrown if the asset has no property at one of the paths. Nothing is written then.
        /// </exception>
        public static bool WriteValues(Object asset, IReadOnlyList<KeyValuePair<string, object>> values, bool isUndoable)
        {
            using (SerializedObject serializedObject = new SerializedObject(asset))
            {
                foreach (KeyValuePair<string, object> value in values)
                {
                    SerializedProperty property = serializedObject.FindProperty(value.Key);
                    if (property == null)
                    {
                        throw new ArgumentException($"The asset {asset.name} has no serialized property at \"{value.Key}\".",
                            nameof(values));
                    }

                    SetValue(property, value.Value);
                }

                return isUndoable
                    ? serializedObject.ApplyModifiedProperties()
                    : serializedObject.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        /// <summary>
        /// Confirms the edit: groups its writes into a single Undo step, including the writes made to other cells with
        /// <see cref="WriteValue(Object, string, object)"/> since the edit started.
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


        #region Private API

        /// <summary>
        /// Sets the value of a serialized property.
        /// </summary>
        /// <remarks>
        /// Uses <see cref="SerializedProperty.boxedValue"/>, except for the property types stored as integers (enums, characters and
        /// layer masks) and for <see cref="Color32"/>, which go through their own setters. Their C# values don't match the type that
        /// <see cref="SerializedProperty.boxedValue"/> expects, and their setters convert them reliably.
        /// </remarks>
        /// <param name="property">The property to set.</param>
        /// <param name="value">The value to set, of the C# type of the field.</param>
        private static void SetValue(SerializedProperty property, object value)
        {
            switch (property.propertyType)
            {
                case SerializedPropertyType.Enum:
                case SerializedPropertyType.Character:
                    property.intValue = Convert.ToInt32(value, CultureInfo.InvariantCulture);
                    break;

                case SerializedPropertyType.LayerMask:
                    property.intValue = value is LayerMask mask
                        ? mask.value
                        : Convert.ToInt32(value, CultureInfo.InvariantCulture);
                    break;

                case SerializedPropertyType.Color when value is Color32 color32:
                    property.colorValue = color32;
                    break;

                default:
                    property.boxedValue = value;
                    break;
            }
        }

        #endregion

    }

}
