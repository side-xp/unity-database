using UnityEngine;

// The fields of the test types are only read by reflection
#pragma warning disable CS0414

namespace SideXP.Database.Tests
{

    /// <summary>
    /// Root of the item hierarchy of the tests: a table passed on to its derived types.
    /// </summary>
    [Table("Tests/Items/All", Inherited = true, AllowRenameAsset = true)]
    public class ItemSO : ScriptableObject
    {

        [SerializeField]
        [Tooltip("The name of the item shown to the players.")]
        [Column]
        private string _displayName = string.Empty;

    }

}
