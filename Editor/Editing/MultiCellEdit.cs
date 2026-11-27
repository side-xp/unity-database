using System;
using System.Collections.Generic;

namespace SideXP.Database.EditorOnly
{

    /// <summary>
    /// The rules of the edits that apply to several selected cells: which cells receive the value, and when the value is applied.
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item>By default, an edit stays in its column: the value goes to the selected cells of the edited cell's column.</item>
    /// <item>With the modifier (Alt), the value goes to all the selected cells of the same field type, in any column. Two enums of
    /// different types, references to different asset types, or a column of <see cref="int"/> and a column of <see cref="float"/> are
    /// different types.</item>
    /// <item>An edit applies to the other cells only if the edited cell is selected, and only if the edit changed the value, so
    /// confirming an edit without typing anything never overwrites the other cells.</item>
    /// </list>
    /// </remarks>
    internal static class MultiCellEdit
    {

        #region Public API

        /// <summary>
        /// Checks if a selected cell receives the value of an edit.
        /// </summary>
        /// <param name="editedColumnId">The identifier of the column of the edited cell.</param>
        /// <param name="editedType">The type of the values of the column of the edited cell.</param>
        /// <param name="columnId">The identifier of the column of the selected cell.</param>
        /// <param name="columnType">The type of the values of the column of the selected cell.</param>
        /// <param name="isSameTypeEdit">If enabled (the modifier is held), the edit applies to all the columns of the same type.
        /// Otherwise, it stays in the column of the edited cell.</param>
        /// <returns>Returns true if the selected cell receives the value.</returns>
        public static bool IsTarget(string editedColumnId, Type editedType, string columnId, Type columnType, bool isSameTypeEdit)
        {
            return isSameTypeEdit
                ? columnType == editedType
                : columnId == editedColumnId;
        }

        /// <summary>
        /// Checks if values differ from the value of the edited cell, so its edit field shows the mixed value indicator.
        /// </summary>
        /// <param name="editedValue">The value of the edited cell.</param>
        /// <param name="otherValues">The values of the other cells that the edit would change.</param>
        /// <returns>Returns true if at least one value differs.</returns>
        public static bool AreMixed(object editedValue, IEnumerable<object> otherValues)
        {
            foreach (object value in otherValues)
            {
                if (!Equals(editedValue, value))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Checks if an edit changed the value of the edited cell, so it applies to the other selected cells.
        /// </summary>
        /// <param name="originalValue">The value of the edited cell when the edit started.</param>
        /// <param name="editedValue">The value of the edit field when the edit is confirmed.</param>
        /// <param name="wasMixed">Checks if the edit field showed the mixed value indicator when the edit started.</param>
        /// <param name="isStillMixed">Checks if the edit field still shows the mixed value indicator. Unity removes it when the user
        /// changes the value of the field.</param>
        /// <returns>Returns true if the value differs from the original one, or if the user changed the value of a field that showed the
        /// mixed value indicator (even to the original value, which is then given to the other cells).</returns>
        public static bool HasChanged(object originalValue, object editedValue, bool wasMixed, bool isStillMixed)
        {
            if (wasMixed && !isStillMixed)
                return true;

            return !Equals(originalValue, editedValue);
        }

        #endregion

    }

}
