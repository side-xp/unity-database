using UnityEngine;

// The fields of the test types are only read by reflection
#pragma warning disable CS0414

namespace SideXP.Database.Tests
{

    /// <summary>
    /// Declares columns with invalid paths, to check that they are skipped with a warning. Not a table, so the window doesn't report
    /// its columns in the console.
    /// </summary>
    public class InvalidPathColumnsSO : ScriptableObject
    {

        [SerializeField]
        [Column(Path = "unknown")]
        [Column(Path = "_hidden")]
        [Column(Path = "bonus..amount")]
        [Column(Path = "attack")]
        [Column("Same Path", Path = "attack")]
        private NestedStats _stats;

        [SerializeField]
        [Column(Path = "name")]
        private ScriptableObject _reference = null;

        [SerializeReference]
        [Column(Path = "label")]
        private NestedData _managed = null;

        [SerializeField]
        [Column(Path = "amount")]
        private NestedBonus[] _array = null;

    }

}
