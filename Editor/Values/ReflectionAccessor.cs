using System;
using System.Collections.Generic;
using System.Reflection;

namespace SideXP.Database.EditorOnly
{

    /// <summary>
    /// Reads values with reflection, by following a chain of C# fields from the asset to the value.
    /// </summary>
    /// <remarks>
    /// This is fast and creates no serialized object. It reads the C# value, which can differ from the serialized data for types that
    /// customize their serialization (<see cref="UnityEngine.ISerializationCallbackReceiver"/>), between a change made in code and the
    /// next save.
    /// </remarks>
    internal class ReflectionAccessor : IValueAccessor
    {

        #region Fields

        /// <summary>
        /// The chain of fields from the asset to the value.
        /// </summary>
        private FieldInfo[] _fields = null;

        #endregion


        #region Lifecycle

        /// <inheritdoc cref="ReflectionAccessor"/>
        /// <param name="fields"><inheritdoc cref="_fields" path="/summary"/></param>
        /// <exception cref="ArgumentException">Thrown if the chain of fields is null or empty.</exception>
        public ReflectionAccessor(IReadOnlyList<FieldInfo> fields)
        {
            if (fields == null || fields.Count == 0)
                throw new ArgumentException("The chain of fields of a reflection accessor can't be null or empty.", nameof(fields));

            _fields = new FieldInfo[fields.Count];
            for (int i = 0; i < fields.Count; i++)
                _fields[i] = fields[i];
        }

        #endregion


        #region Public API

        /// <inheritdoc/>
        public Type ValueType => _fields[_fields.Length - 1].FieldType;

        /// <inheritdoc/>
        public object Read(ValueReadContext context)
        {
            object value = context.Asset;
            foreach (FieldInfo field in _fields)
            {
                // A reference along the path can be null (for example a field marked [SerializeReference])
                if (value == null)
                    return null;

                value = field.GetValue(value);
            }
            return value;
        }

        #endregion

    }

}
