using System;

using UnityEngine;

// The fields of the test types are only read by reflection, and some are not serialized on purpose
#pragma warning disable CS0169, CS0414

namespace SideXP.Database.Tests
{

    /// <summary>
    /// Table that declares columns on fields that are not serialized, to check that they are skipped with a warning.
    /// </summary>
    [Table("Tests/Invalid Columns")]
    public class InvalidColumnsSO : ScriptableObject
    {

        [Column]
        public static int StaticField = 0;

        [Column]
        public const int ConstField = 0;

        [Column]
        public readonly int readonlyField = 0;

        [Column]
        [NonSerialized]
        public int nonSerializedField = 0;

        [Column]
        private int _privateField = 0;

        [SerializeField]
        [Column]
        [Column("Second Column")]
        private int _severalColumns = 0;

        [SerializeField]
        [Column]
        private int _validField = 0;

    }

}
