using System;
using System.Collections.Generic;

using UnityEngine;

namespace SideXP.Database.EditorOnly
{

    /// <summary>
    /// Keeps the rows whose enum value is one of the chosen values. For a <see cref="FlagsAttribute"/> enum, keeps the rows that have
    /// at least one of the chosen flags.
    /// </summary>
    /// <remarks>
    /// The values are stored as numbers, like Unity serializes the enum fields of the assets, so renaming a member of the enum keeps
    /// the condition. Without any chosen value, the condition keeps no row, like a column filter of a spreadsheet app with all its
    /// values unchecked.
    /// <para>
    /// With a flags enum, a chosen value of <c>0</c> (usually <i>None</i>) keeps the rows without any flag, and a chosen combination of
    /// flags keeps the rows that have at least one of its flags.
    /// </para>
    /// </remarks>
    [Serializable]
    internal class EnumCondition : FilterCondition
    {

        #region Fields

        /// <summary>
        /// The chosen values, as their numbers (see <see cref="ToNumber(object)"/>).
        /// </summary>
        [SerializeField]
        private List<long> _values = new List<long>();

        #endregion


        #region Lifecycle

        /// <inheritdoc cref="EnumCondition"/>
        public EnumCondition()
        {
        }

        /// <inheritdoc cref="EnumCondition"/>
        /// <param name="columnId"><inheritdoc cref="FilterCondition.ColumnId" path="/summary"/></param>
        /// <param name="values">The chosen enum values.</param>
        public EnumCondition(string columnId, params Enum[] values)
            : base(columnId)
        {
            foreach (Enum value in values)
                _values.Add(ToNumber(value));
        }

        #endregion


        #region Public API

        /// <inheritdoc cref="_values"/>
        public List<long> Values => _values;

        /// <summary>
        /// Gets the number of an enum value, as stored by the condition.
        /// </summary>
        /// <remarks>
        /// The values of an enum based on <see cref="ulong"/> that don't fit in a <see cref="long"/> are stored with the same bits, so
        /// they stay distinct.
        /// </remarks>
        /// <param name="enumValue">The enum value.</param>
        /// <returns>Returns the number of the value.</returns>
        /// <exception cref="ArgumentException">Thrown if the value is not an enum value.</exception>
        public static long ToNumber(object enumValue)
        {
            if (enumValue is not Enum)
                throw new ArgumentException("The value must be an enum value.", nameof(enumValue));

            return Enum.GetUnderlyingType(enumValue.GetType()) == typeof(ulong)
                ? unchecked((long)Convert.ToUInt64(enumValue))
                : Convert.ToInt64(enumValue);
        }

        /// <inheritdoc/>
        public override bool CanApplyTo(Type valueType)
        {
            return valueType != null && valueType.IsEnum;
        }

        /// <inheritdoc/>
        public override Predicate<object> CreateMatcher(FilterColumn column)
        {
            if (!column.ValueType.IsDefined(typeof(FlagsAttribute), false))
            {
                HashSet<long> values = new HashSet<long>(_values);
                return value => value is Enum && values.Contains(ToNumber(value));
            }

            long flags = 0;
            bool isNoFlagChosen = false;
            foreach (long chosenValue in _values)
            {
                flags |= chosenValue;
                isNoFlagChosen |= chosenValue == 0;
            }

            return value =>
            {
                if (value is not Enum)
                    return false;

                long number = ToNumber(value);
                return number == 0 ? isNoFlagChosen : (number & flags) != 0;
            };
        }

        #endregion

    }

}
