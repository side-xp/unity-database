using UnityEngine;

// The fields of the test types are only read by reflection
#pragma warning disable CS0414

namespace SideXP.Database.Tests
{

    /// <summary>
    /// A table with its own declaration, which stops the inheritance of <see cref="ItemSO"/>'s table.
    /// </summary>
    [Table("Tests/Items/Equipment")]
    public class EquipmentSO : ItemSO
    {

        [SerializeField]
        [Column(Width = 88)]
        private int _basePrice = 0;

    }

}
