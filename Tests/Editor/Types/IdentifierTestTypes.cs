using UnityEngine;

namespace SideXP.Database.Tests
{

    /// <summary>
    /// Holds a nested table type, which has no script file of its own.
    /// </summary>
    public static class IdentifierTestTypes
    {

        /// <summary>
        /// Nested table type, without a name in its declaration.
        /// </summary>
        [Table]
        public class NestedTableSO : ScriptableObject { }

    }

    /// <summary>
    /// Table type declared in a file named after another type, so it has no script file of its own.
    /// </summary>
    [Table("Tests/Misnamed")]
    public class MisnamedTableSO : ScriptableObject { }

}
