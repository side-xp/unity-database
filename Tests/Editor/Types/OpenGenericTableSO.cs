using UnityEngine;

namespace SideXP.Database.Tests
{

    /// <summary>
    /// Open generic type declared as a table without passing it on, which is ignored with a warning.
    /// </summary>
    /// <typeparam name="T">Unused type parameter.</typeparam>
    [Table("Tests/Open Generic")]
    public class OpenGenericTableSO<T> : ScriptableObject { }

}
