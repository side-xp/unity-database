using UnityEngine;

// The fields of the test types are only read by reflection
#pragma warning disable CS0414

namespace SideXP.Database.Tests
{

    /// <summary>
    /// Open generic type that passes its table on to its closed derived types, but is not a table itself.
    /// </summary>
    /// <typeparam name="T">The type of the value displayed in the column.</typeparam>
    [Table("Tests/Generic/Base", Inherited = true)]
    public abstract class GenericBaseTableSO<T> : ScriptableObject
    {

        [SerializeField]
        [Column]
        private T _value = default;

    }

}
