using UnityEngine;

// The fields of the test types are only read by reflection
#pragma warning disable CS0414

namespace SideXP.Database.Tests
{

    /// <summary>
    /// A table inherited from <see cref="ItemSO"/>.
    /// </summary>
    public class ConsumableSO : ItemSO
    {

        [SerializeField]
        [Column]
        private int _amount = 0;

    }

}
