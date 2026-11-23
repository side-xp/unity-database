using System.Reflection;

using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.UIElements;

namespace SideXP.Database.EditorOnly
{

    /// <summary>
    /// Handles the color fields marked with <see cref="ColorUsageAttribute"/>: displays them as swatches like the other colors, and
    /// edits them with a color field that follows the HDR and alpha options of the attribute.
    /// </summary>
    [CustomCellHandler(typeof(ColorUsageAttribute))]
    internal class ColorUsageCellHandler : ColorCellHandler
    {

        #region Edit

        /// <inheritdoc/>
        public override VisualElement CreateEditField(SerializedProperty property, object value, CellContext context)
        {
            VisualElement field = base.CreateEditField(property, value, context);
            ColorUsageAttribute usage = context.Field?.GetCustomAttribute<ColorUsageAttribute>(true);
            if (field is ColorField colorField && usage != null)
            {
                colorField.hdr = usage.hdr;
                colorField.showAlpha = usage.showAlpha;
            }

            return field;
        }

        #endregion

    }

}
