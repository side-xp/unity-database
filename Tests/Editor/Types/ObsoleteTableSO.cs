using System;

using UnityEngine;

namespace SideXP.Database.Tests
{

    /// <summary>
    /// Obsolete table type, which is still a table.
    /// </summary>
    [Obsolete("Only used to test obsolete tables.")]
    [Table("Tests/Obsolete")]
    public class ObsoleteTableSO : ScriptableObject { }

}
