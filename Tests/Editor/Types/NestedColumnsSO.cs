using System;

using UnityEngine;

// The fields of the test types are only read by reflection, and some are not serialized on purpose
#pragma warning disable CS0169, CS0414, CS0649

namespace SideXP.Database.Tests
{

    /// <summary>
    /// A struct whose fields are displayed as nested columns.
    /// </summary>
    [Serializable]
    public struct NestedStats
    {

        public int attack;

        [Tooltip("The defense of the item.")]
        public int defense;

        [Range(0, 10)]
        public float speed;

        public NestedBonus bonus;

        [field: SerializeField]
        public int Luck { get; set; }

        private int _hidden;

    }

    /// <summary>
    /// A struct nested in <see cref="NestedStats"/>, to check paths of several fields.
    /// </summary>
    [Serializable]
    public struct NestedBonus
    {

        public int amount;

    }

    /// <summary>
    /// The base class of <see cref="NestedData"/>, to check that the fields of base classes can be in a path.
    /// </summary>
    [Serializable]
    public class NestedBaseData
    {

        [SerializeField]
        private int _baseValue = 0;

        /// <summary>
        /// The value of the private field of the base class.
        /// </summary>
        public int BaseValue
        {
            get => _baseValue;
            set => _baseValue = value;
        }

    }

    /// <summary>
    /// A serializable class whose fields are displayed as nested columns.
    /// </summary>
    [Serializable]
    public class NestedData : NestedBaseData
    {

        public string label = string.Empty;

    }

    /// <summary>
    /// Table that declares columns on fields nested in a struct and in a class.
    /// </summary>
    [Table("Tests/Nested Columns")]
    public class NestedColumnsSO : ScriptableObject
    {

        #region Fields

        [SerializeField]
        [Tooltip("The stats of the item.")]
        [Column(Path = "defense")]
        [Column(Path = "attack")]
        [Column("Bonus", Path = " bonus . amount ", Width = 60)]
        [Column(Path = "Luck")]
        [Column(Path = "speed")]
        private NestedStats _stats;

        [SerializeField]
        [Column]
        [Column(Path = "_baseValue")]
        [Column(Path = "label")]
        private NestedData _data = new NestedData();

        #endregion


        #region Public API

        /// <summary>
        /// The stats displayed by the nested columns of the struct.
        /// </summary>
        public NestedStats Stats
        {
            get => _stats;
            set => _stats = value;
        }

        /// <summary>
        /// The data displayed by the nested columns of the class.
        /// </summary>
        public NestedData Data => _data;

        #endregion

    }

}
