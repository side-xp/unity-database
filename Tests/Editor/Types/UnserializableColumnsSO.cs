using System;
using System.Collections.Generic;

using UnityEngine;

// The fields of the test types are only read by reflection
#pragma warning disable CS0414
// The unsupported types are the subject of the tests that use this type
#pragma warning disable UAC1009

namespace SideXP.Database.Tests
{

    /// <summary>
    /// Type that declares columns on fields marked to be serialized, but whose types Unity can't serialize. Not a table, so it doesn't
    /// appear in the window.
    /// </summary>
    public class UnserializableColumnsSO : ScriptableObject
    {

        [SerializeField]
        [Column]
        private int _validField = 0;

        [SerializeField]
        [Column]
        private Dictionary<int, int> _dictionary = new Dictionary<int, int>();

        [SerializeField]
        [Column]
        private Action _callback = null;

    }

}
