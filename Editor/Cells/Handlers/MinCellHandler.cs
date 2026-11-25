using UnityEngine;
using UnityEngine.UIElements;

namespace SideXP.Database.EditorOnly
{

    /// <summary>
    /// Handles the fields marked with <see cref="MinAttribute"/>: clamps the edited, parsed and accepted numbers to the minimum, like
    /// the Inspector. The display and the edit field are the default ones.
    /// </summary>
    [CustomCellHandler(typeof(MinAttribute))]
    internal class MinCellHandler : CellHandler
    {

        #region Edit

        /// <inheritdoc/>
        public override bool TryGetEditedValue(VisualElement field, CellContext context, out object value)
        {
            if (!base.TryGetEditedValue(field, context, out value))
                return false;

            value = ClampToFieldLimits(value, context);
            return true;
        }

        /// <summary>
        /// Accepts the numbers given by an edit of another cell, clamped to the minimum of the field.
        /// </summary>
        /// <inheritdoc/>
        public override bool TryAcceptValue(object value, CellContext context, out object acceptedValue)
        {
            acceptedValue = ClampToFieldLimits(value, context);
            return true;
        }

        #endregion


        #region Text conversion

        /// <summary>
        /// Converts a text to a number clamped to the minimum of the field.
        /// </summary>
        /// <inheritdoc/>
        public override bool TryParse(string text, CellContext context, out object value)
        {
            if (!base.TryParse(text, context, out value))
                return false;

            value = ClampToFieldLimits(value, context);
            return true;
        }

        #endregion

    }

}
