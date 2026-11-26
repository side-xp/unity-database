using UnityEngine;

using SideXP.Database.EditorOnly;

namespace SideXP.Database.Tests
{

    /// <summary>
    /// Type of the in-memory objects used to test the serialization of the views through Unity's serialization, like
    /// <see cref="UserViews"/> stores them, since their filter tree uses <see cref="SerializeReferenceAttribute"/>. Not a table, so it
    /// doesn't appear in the window.
    /// </summary>
    public class ViewSettingsTestSO : ScriptableObject
    {

        /// <summary>
        /// The serialized settings.
        /// </summary>
        [SerializeField]
        internal ViewSettings settings = null;

        /// <summary>
        /// The serialized personal views.
        /// </summary>
        [SerializeField]
        internal PersonalViews personalViews = null;

        /// <summary>
        /// The serialized project views.
        /// </summary>
        [SerializeField]
        internal ProjectViewList projectViews = null;

    }

}
