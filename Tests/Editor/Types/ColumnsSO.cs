using UnityEngine;

// The fields of the test types are only read by reflection
#pragma warning disable CS0414

namespace SideXP.Database.Tests
{

    /// <summary>
    /// Table used to check the order, names, widths and tooltips of the columns.
    /// </summary>
    [Table("Tests/Columns")]
    public class ColumnsSO : ColumnsBaseSO
    {

        [SerializeField]
        [Column]
        private int _first = 0;

        [SerializeField]
        private int _notAColumn = 0;

        [Tooltip("The tooltip of the second column.")]
        [Column("Custom Name", Width = 120)]
        public int second = 0;

        [Column("   ")]
        public int blankName = 0;

        [field: SerializeField]
        [field: Column]
        public int AutoProperty { get; private set; } = 0;

    }

}
