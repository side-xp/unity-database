using System;
using System.Globalization;

using UnityEngine;

namespace SideXP.Database.EditorOnly
{

    /// <summary>
    /// Keeps the rows whose number is in a range. Both limits are included, and each one is optional, so the range can be "at least"
    /// or "at most" a value.
    /// </summary>
    /// <remarks>
    /// Applies to the columns of integers and floating-point numbers, not to enums. The values are compared as <see cref="double"/>, so
    /// the largest 64-bit integers lose some precision.
    /// </remarks>
    [Serializable]
    internal class NumberRangeCondition : FilterCondition
    {

        #region Fields

        /// <summary>
        /// Checks if the range has a minimum.
        /// </summary>
        [SerializeField]
        private bool _hasMin = false;

        /// <summary>
        /// The smallest number of the range, included. Ignored if <see cref="_hasMin"/> is disabled.
        /// </summary>
        [SerializeField]
        private double _min = 0;

        /// <summary>
        /// Checks if the range has a maximum.
        /// </summary>
        [SerializeField]
        private bool _hasMax = false;

        /// <summary>
        /// The largest number of the range, included. Ignored if <see cref="_hasMax"/> is disabled.
        /// </summary>
        [SerializeField]
        private double _max = 0;

        #endregion


        #region Lifecycle

        /// <inheritdoc cref="NumberRangeCondition"/>
        public NumberRangeCondition()
        {
        }

        /// <inheritdoc cref="NumberRangeCondition"/>
        /// <param name="columnId"><inheritdoc cref="FilterCondition.ColumnId" path="/summary"/></param>
        /// <param name="min">The smallest number of the range, included, or null if the range has no minimum.</param>
        /// <param name="max">The largest number of the range, included, or null if the range has no maximum.</param>
        public NumberRangeCondition(string columnId, double? min, double? max)
            : base(columnId)
        {
            Min = min;
            Max = max;
        }

        #endregion


        #region Public API

        /// <summary>
        /// The smallest number of the range, included, or null if the range has no minimum.
        /// </summary>
        public double? Min
        {
            get => _hasMin ? _min : (double?)null;
            set
            {
                _hasMin = value.HasValue;
                _min = value ?? 0;
            }
        }

        /// <summary>
        /// The largest number of the range, included, or null if the range has no maximum.
        /// </summary>
        public double? Max
        {
            get => _hasMax ? _max : (double?)null;
            set
            {
                _hasMax = value.HasValue;
                _max = value ?? 0;
            }
        }

        /// <summary>
        /// Checks if a type is a type of numbers: an integer or a floating-point number, not an enum nor a character.
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <returns>Returns true if the type is a type of numbers.</returns>
        public static bool IsNumberType(Type type)
        {
            // The type code of an enum is the one of its underlying type
            if (type == null || type.IsEnum)
                return false;

            TypeCode typeCode = Type.GetTypeCode(type);
            return typeCode >= TypeCode.SByte && typeCode <= TypeCode.Decimal;
        }

        /// <inheritdoc/>
        public override bool CanApplyTo(Type valueType)
        {
            return IsNumberType(valueType);
        }

        /// <inheritdoc/>
        public override Predicate<object> CreateMatcher(FilterColumn column)
        {
            bool hasMin = _hasMin;
            bool hasMax = _hasMax;
            double min = _min;
            double max = _max;
            return value =>
            {
                if (value is not IConvertible convertible)
                    return false;

                // NaN fails both comparisons, so it's only kept by a range without limits
                double number = convertible.ToDouble(CultureInfo.InvariantCulture);
                return (!hasMin || number >= min) && (!hasMax || number <= max);
            };
        }

        #endregion

    }

}
