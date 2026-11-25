using UnityEngine;

using SideXP.Database.EditorOnly;

namespace SideXP.Database.Tests
{

    /// <summary>
    /// Type of the in-memory objects used to test the serialization of the filter trees, which the settings of the views store with
    /// <see cref="SerializeReferenceAttribute"/>. Not a table, so it doesn't appear in the window.
    /// </summary>
    public class FilterTestSO : ScriptableObject
    {

        /// <summary>
        /// The serialized filter tree.
        /// </summary>
        [SerializeReference]
        internal FilterNode filter = null;

    }

}
