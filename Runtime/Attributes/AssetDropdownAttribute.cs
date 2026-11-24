using System;
using System.Diagnostics;

using UnityEngine;

namespace SideXP.Database
{

    /// <summary>
    /// Draws a reference to an asset as a dropdown, which lists a <i>None</i> option and every asset of the field's type found in the
    /// project, including the assets of its derived types and sub-assets.
    /// </summary>
    /// <remarks>
    /// Use it for fields that reference a small, known set of "definition" assets (categories, types, tiers), where picking from a
    /// list is faster than drag-and-drop or the object picker. Works in the Inspector, and in the cells of the Database window.
    /// <para>
    /// The options are found when the dropdown opens, so new assets show up right away. Sub-assets are listed in a submenu named after
    /// their main asset, and a <c>/</c> in the name of a sub-asset creates a submenu too.
    /// </para>
    /// <para>
    /// The field must reference a type of asset that is not a component: a <see cref="ScriptableObject"/> type, or a type such as
    /// <see cref="Material"/>, <see cref="Sprite"/> or <see cref="GameObject"/> (prefabs and models). This attribute is stripped from
    /// player builds.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// [SerializeField]
    /// [AssetDropdown]
    /// private ItemCategorySO _category = null;
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
    [Conditional("UNITY_EDITOR")]
    public class AssetDropdownAttribute : PropertyAttribute { }

}
