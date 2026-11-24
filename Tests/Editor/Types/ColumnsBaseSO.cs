using UnityEngine;

// The fields of the test types are only read by reflection
#pragma warning disable CS0414

namespace SideXP.Database.Tests
{

    /// <summary>
    /// Base type of <see cref="ColumnsSO"/>, which declares public and protected columns to check that they are collected once.
    /// </summary>
    public abstract class ColumnsBaseSO : ScriptableObject
    {

        [Column]
        public int baseFirst = 0;

        [SerializeField]
        [Column]
        protected int _baseSecond = 0;

    }

}
